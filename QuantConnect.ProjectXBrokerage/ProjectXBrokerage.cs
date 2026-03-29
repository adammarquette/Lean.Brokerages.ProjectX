/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Logging;
using QuantConnect.Configuration;
using System.Collections.Generic;
using System.Collections.Concurrent;
using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.DependencyInjection;
using ModifyOrderRequest = MarqSpec.Client.ProjectX.Api.Models.ModifyOrderRequest;
using PlaceOrderRequest = MarqSpec.Client.ProjectX.Api.Models.PlaceOrderRequest;
using PxOrderUpdate = MarqSpec.Client.ProjectX.Api.Models.OrderUpdate;
using MarqSpec.Client.ProjectX.Exceptions;
using MarqSpec.Client.ProjectX.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PxOrder = MarqSpec.Client.ProjectX.Api.Models.Order;
using PxOrderStatus = MarqSpec.Client.ProjectX.Api.Models.OrderStatus;
using PxOrderType = MarqSpec.Client.ProjectX.Api.Models.OrderType;
using PxOrderSide = MarqSpec.Client.ProjectX.Api.Models.OrderSide;
using PxPosition = MarqSpec.Client.ProjectX.Api.Models.Position;
using PxPositionType = MarqSpec.Client.ProjectX.Api.Models.PositionType;
using PxAccount = MarqSpec.Client.ProjectX.Api.Models.TradingAccount;
using PxPriceUpdate = MarqSpec.Client.ProjectX.Api.Models.PriceUpdate;
using PxTradeUpdate = MarqSpec.Client.ProjectX.Api.Models.TradeUpdate;
using PxAggregateBar = MarqSpec.Client.ProjectX.Api.Models.AggregateBar;
using PxAggregateBarUnit = MarqSpec.Client.ProjectX.Api.Models.AggregateBarUnit;
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages.ProjectXBrokerage
{
    [BrokerageFactory(typeof(ProjectXBrokerageFactory))]
    public partial class ProjectXBrokerage : Brokerage
    {
        private readonly IDataAggregator _aggregator;
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;
        private readonly ProjectXSymbolMapper _symbolMapper;

        // Connection state
        private volatile bool _isConnected;
        private readonly object _connectionLock = new object();
        private readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);

        // Order ID mapping - Thread-safe bidirectional mapping
        private readonly ConcurrentDictionary<int, long> _leanToProjectXOrderIds = new ConcurrentDictionary<int, long>();
        private readonly ConcurrentDictionary<long, int> _projectXToLeanOrderIds = new ConcurrentDictionary<long, int>();
        private readonly ConcurrentDictionary<long, Order> _ordersCache = new ConcurrentDictionary<long, Order>();
        private readonly object _orderMappingLock = new object();

        // Market data subscription tracking - maps ProjectX contractId to LEAN Symbol
        private readonly ConcurrentDictionary<string, Symbol> _subscribedContractIds = new ConcurrentDictionary<string, Symbol>();

        // Recently submitted orders for duplicate detection (OrderId -> submission time)
        private readonly ConcurrentDictionary<int, DateTime> _recentlySubmittedOrders = new ConcurrentDictionary<int, DateTime>();
        private readonly TimeSpan _duplicateDetectionWindow = TimeSpan.FromSeconds(5);

        // Configuration
        private string _apiKey;
        private string _apiSecret;
        private string _environment;
        private int _maxReconnectAttempts;
        private int _reconnectDelayMilliseconds;
        private int _connectionTimeoutMilliseconds;

        // Retry and reconnection
        private CancellationTokenSource _connectionCts;
        private Task _heartbeatTask;
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);

        private IProjectXApiClient _apiClient;
        private IProjectXWebSocketClient _wsClient;
        private ServiceProvider _serviceProvider;
        private int _accountId;
        private string _baseUrl;

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get
            {
                lock (_connectionLock)
                {
                    return _isConnected;
                }
            }
        }

        /// <summary>
        /// Parameterless constructor for brokerage
        /// </summary>
        /// <remarks>This parameterless constructor is required for brokerages implementing <see cref="IDataQueueHandler"/></remarks>
        public ProjectXBrokerage()
            : this(Composer.Instance.GetPart<IDataAggregator>())
        {
        }

        /// <summary>
        /// Creates a new instance of the ProjectXBrokerage with explicit parameters
        /// </summary>
        /// <param name="apiKey">The ProjectX API key</param>
        /// <param name="apiSecret">The ProjectX API secret</param>
        /// <param name="environment">The target environment (sandbox or production)</param>
        /// <param name="accountId">The ProjectX account ID</param>
        /// <param name="aggregator">The data aggregator for consolidating ticks</param>
        public ProjectXBrokerage(string apiKey, string apiSecret, string environment, int accountId, IDataAggregator aggregator) : base("ProjectXBrokerage")
        {
            Log.Trace("ProjectXBrokerage(): Initializing ProjectX brokerage instance with explicit configuration");

            _aggregator = aggregator ?? Composer.Instance.GetPart<IDataAggregator>();
            _symbolMapper = new ProjectXSymbolMapper();
            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += (s, t) => Subscribe(s);
            _subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _environment = environment;
            _accountId = accountId;
            
            LoadOptionalConfiguration();

            // Initialize connection state
            _isConnected = false;
            _connectionCts = new CancellationTokenSource();
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="aggregator">consolidate ticks</param>
        public ProjectXBrokerage(IDataAggregator aggregator) : base("ProjectXBrokerage")
        {
            Log.Trace("ProjectXBrokerage(): Initializing ProjectX brokerage instance");

            _aggregator = aggregator;
            _symbolMapper = new ProjectXSymbolMapper();
            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += (s, t) => Subscribe(s);
            _subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

            // Load configuration
            LoadConfiguration();

            // Initialize connection state
            _isConnected = false;
            _connectionCts = new CancellationTokenSource();

            // Useful for some brokerages:

            // Brokerage helper class to lock websocket message stream while executing an action, for example placing an order
            // avoid race condition with placing an order and getting filled events before finished placing
            // _messageHandler = new BrokerageConcurrentMessageHandler<>();

            // Rate gate limiter useful for API/WS calls
            // _connectionRateLimiter = new RateGate();

            Log.Trace("ProjectXBrokerage(): Initialization complete");
        }

        #region Brokerage

        /// <summary>
        /// Gets all open orders on the account.
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from ProjectX</returns>
        public override List<Order> GetOpenOrders()
        {
            Log.Trace("ProjectXBrokerage.GetOpenOrders(): Retrieving open orders");

            try
            {
                // Validate connection
                if (!IsConnected)
                {
                    Log.Error("ProjectXBrokerage.GetOpenOrders(): Not connected to ProjectX");
                    return new List<Order>();
                }

                var pxOrders = _apiClient.GetOpenOrdersAsync(_accountId, CancellationToken.None).GetAwaiter().GetResult();
                var openOrders = new List<Order>();

                foreach (var pxOrder in pxOrders)
                {
                    try
                    {
                        var order = ConvertFromProjectXOrder(pxOrder);
                        openOrders.Add(order);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"ProjectXBrokerage.GetOpenOrders(): Failed to convert ProjectX order {pxOrder.Id}");
                    }
                }

                Log.Debug($"ProjectXBrokerage.GetOpenOrders(): Retrieved {openOrders.Count} open orders");
                return openOrders;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.GetOpenOrders(): Error retrieving open orders");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "GET_OPEN_ORDERS_ERROR", 
                    $"Error retrieving open orders: {ex.Message}"));
                return new List<Order>();
            }
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            Log.Trace("ProjectXBrokerage.GetAccountHoldings(): Retrieving account holdings");

            try
            {
                // Validate connection
                if (!IsConnected)
                {
                    Log.Error("ProjectXBrokerage.GetAccountHoldings(): Not connected to ProjectX");
                    return new List<Holding>();
                }

                var pxPositions = _apiClient.GetOpenPositionsAsync(_accountId, CancellationToken.None).GetAwaiter().GetResult();
                var holdings = new List<Holding>();

                foreach (var pxPosition in pxPositions)
                {
                    try
                    {
                        holdings.Add(ConvertFromProjectXPosition(pxPosition));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"ProjectXBrokerage.GetAccountHoldings(): Failed to convert position {pxPosition.ContractId}");
                    }
                }

                Log.Debug($"ProjectXBrokerage.GetAccountHoldings(): Retrieved {holdings.Count} holding(s)");
                return holdings;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.GetAccountHoldings(): Error retrieving account holdings");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "GET_HOLDINGS_ERROR", 
                    $"Error retrieving account holdings: {ex.Message}"));
                return new List<Holding>();
            }
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            Log.Trace("ProjectXBrokerage.GetCashBalance(): Retrieving cash balance");

            try
            {
                // Validate connection
                if (!IsConnected)
                {
                    Log.Error("ProjectXBrokerage.GetCashBalance(): Not connected to ProjectX");
                    return new List<CashAmount>();
                }

                var accounts = _apiClient.GetAccountsAsync(true, CancellationToken.None).GetAwaiter().GetResult();
                var account = accounts?.FirstOrDefault(a => a.Id == _accountId);

                if (account == null)
                {
                    Log.Error($"ProjectXBrokerage.GetCashBalance(): Account {_accountId} not found in response");
                    return new List<CashAmount>();
                }

                Log.Debug($"ProjectXBrokerage.GetCashBalance(): Account {_accountId} balance={account.Balance}");
                return new List<CashAmount> { new CashAmount(account.Balance, "USD") };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.GetCashBalance(): Error retrieving cash balance");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "GET_CASH_BALANCE_ERROR", 
                    $"Error retrieving cash balance: {ex.Message}"));
                return new List<CashAmount>();
            }
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            Log.Trace($"ProjectXBrokerage.PlaceOrder(): OrderId={order.Id}, Symbol={order.Symbol}, Quantity={order.Quantity}, Type={order.Type}");

            try
            {
                // 1. Pre-submission validation
                if (!ValidateOrder(order, out var errorMessage))
                {
                    Log.Error($"ProjectXBrokerage.PlaceOrder(): Order validation failed: {errorMessage}");
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "ProjectX Order Validation Failed")
                    {
                        Status = OrderStatus.Invalid,
                        Message = errorMessage
                    });
                    return false;
                }

                // 2. Check for duplicate submission
                if (IsDuplicateSubmission(order))
                {
                    Log.Error($"ProjectXBrokerage.PlaceOrder(): Duplicate order submission detected for OrderId={order.Id}");
                    return false;
                }

                // 3. Convert LEAN order to ProjectX format
                var request = ConvertToProjectXOrder(order);

                // 4. Submit order via API
                var response = _apiClient.PlaceOrderAsync(request, CancellationToken.None).GetAwaiter().GetResult();
                if (!response.Success)
                {
                    HandleOrderRejection(order, response.ErrorCode.ToString(), response.ErrorMessage);
                    return false;
                }

                if (!response.OrderId.HasValue)
                {
                    Log.Error($"ProjectXBrokerage.PlaceOrder(): Success response missing order ID for LEAN order {order.Id}");
                    return false;
                }

                var projectXOrderId = response.OrderId.Value;

                // 5. Store order ID mapping and cache the order for WebSocket event lookup
                AddOrderIdMapping(order.Id, projectXOrderId);
                _ordersCache[projectXOrderId] = order;

                // 6. Mark order as recently submitted
                _recentlySubmittedOrders.TryAdd(order.Id, DateTime.UtcNow);

                // 7. Fire order event (Submitted status)
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "ProjectX Order Placed")
                {
                    Status = OrderStatus.Submitted
                });

                Log.Trace($"ProjectXBrokerage.PlaceOrder(): Order placed successfully. LEAN ID={order.Id}, ProjectX ID={projectXOrderId}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.PlaceOrder(): Error placing order {order.Id}");
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "ProjectX Order Error")
                {
                    Status = OrderStatus.Invalid,
                    Message = $"Error placing order: {ex.Message}"
                });
                return false;
            }
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            Log.Trace($"ProjectXBrokerage.UpdateOrder(): OrderId={order.Id}, Symbol={order.Symbol}, Quantity={order.Quantity}");

            try
            {
                // 1. Validate connection
                if (!IsConnected)
                {
                    Log.Error("ProjectXBrokerage.UpdateOrder(): Not connected to ProjectX");
                    return false;
                }

                // 2. Retrieve ProjectX order ID from mapping
                if (!_leanToProjectXOrderIds.TryGetValue(order.Id, out var projectXOrderId))
                {
                    Log.Error($"ProjectXBrokerage.UpdateOrder(): Order ID {order.Id} not found in mapping");
                    return false;
                }

                // 3. Build the modification request with updated fields
                var request = new ModifyOrderRequest
                {
                    AccountId = _accountId,
                    OrderId = projectXOrderId,
                    Size = (int)Math.Abs(order.Quantity)
                };

                if (order is LimitOrder limitOrder)
                    request.LimitPrice = limitOrder.LimitPrice;
                else if (order is StopLimitOrder stopLimitOrder)
                {
                    request.LimitPrice = stopLimitOrder.LimitPrice;
                    request.StopPrice = stopLimitOrder.StopPrice;
                }
                else if (order is StopMarketOrder stopMarketOrder)
                    request.StopPrice = stopMarketOrder.StopPrice;

                // 4. Submit modification via API
                var response = _apiClient.ModifyOrderAsync(request, CancellationToken.None).GetAwaiter().GetResult();
                if (!response.Success)
                {
                    Log.Error($"ProjectXBrokerage.UpdateOrder(): Failed to update order. Code: {response.ErrorCode}, Message: {response.ErrorMessage}");
                    return false;
                }

                // 5. Fire order event
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "ProjectX Order Updated")
                {
                    Status = OrderStatus.UpdateSubmitted
                });

                Log.Trace($"ProjectXBrokerage.UpdateOrder(): Order {order.Id} updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.UpdateOrder(): Error updating order {order.Id}");
                return false;
            }
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Log.Trace($"ProjectXBrokerage.CancelOrder(): OrderId={order.Id}, Symbol={order.Symbol}");

            try
            {
                // 1. Validate connection
                if (!IsConnected)
                {
                    Log.Error("ProjectXBrokerage.CancelOrder(): Not connected to ProjectX");
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "ProjectX Not Connected")
                    {
                        Status = OrderStatus.Invalid,
                        Message = "Cannot cancel order: not connected to ProjectX"
                    });
                    return false;
                }

                // 2. Retrieve ProjectX order ID from mapping
                if (!_leanToProjectXOrderIds.TryGetValue(order.Id, out var projectXOrderId))
                {
                    Log.Error($"ProjectXBrokerage.CancelOrder(): Order ID {order.Id} not found in mapping. May have already been filled/canceled.");
                    // Don't treat this as an error - order may have already been filled
                    return false;
                }

                // 3. Submit cancellation request
                var cancelResponse = _apiClient.CancelOrderAsync(_accountId, projectXOrderId, CancellationToken.None).GetAwaiter().GetResult();
                if (!cancelResponse.Success)
                {
                    Log.Error($"ProjectXBrokerage.CancelOrder(): Failed to cancel Order {order.Id}. Code: {cancelResponse.ErrorCode}, Message: {cancelResponse.ErrorMessage}");
                    return false;
                }

                Log.Trace($"ProjectXBrokerage.CancelOrder(): Cancellation submitted for Order {order.Id} (ProjectX ID: {projectXOrderId})");

                // 4. Fire CancelPending event; the final Canceled status will arrive via the OrderUpdateReceived WebSocket event
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "ProjectX Cancel Requested")
                {
                    Status = OrderStatus.CancelPending
                });

                Log.Debug($"ProjectXBrokerage.CancelOrder(): Order {order.Id} cancel request successful");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.CancelOrder(): Error canceling order {order.Id}");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "CANCEL_ORDER_ERROR",
                    $"Error canceling order {order.Id}: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            Log.Trace("ProjectXBrokerage.Connect(): Connecting to ProjectX API");

            if (IsConnected)
            {
                Log.Trace("ProjectXBrokerage.Connect(): Already connected");
                return;
            }

            // Validate configuration
            ValidateConfiguration();

            // Attempt connection with retry logic
            var connected = ConnectWithRetry();

            if (!connected)
            {
                var message = $"ProjectXBrokerage.Connect(): Failed to connect after {_maxReconnectAttempts} attempts";
                Log.Error(message);
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "CONNECTION_FAILED", message));
                throw new Exception(message);
            }

            Log.Trace("ProjectXBrokerage.Connect(): Successfully connected to ProjectX");
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            Log.Trace("ProjectXBrokerage.Disconnect(): Disconnecting from ProjectX API");

            if (!IsConnected)
            {
                Log.Debug("ProjectXBrokerage.Disconnect(): Already disconnected");
                return;
            }

            try
            {
                // Update connection state first to stop heartbeat loop
                lock (_connectionLock)
                {
                    _isConnected = false;
                }

                // Cancel any ongoing operations
                _connectionCts?.Cancel();

                // Stop heartbeat monitoring
                if (_heartbeatTask != null && !_heartbeatTask.IsCompleted)
                {
                    Log.Debug("ProjectXBrokerage.Disconnect(): Stopping heartbeat monitoring");
                    try
                    {
                        _heartbeatTask.Wait(TimeSpan.FromSeconds(5));
                    }
                    catch (AggregateException ex)
                    {
                        // Ignore cancellation exceptions from heartbeat task
                        if (!(ex.InnerException is OperationCanceledException))
                        {
                            Log.Error(ex, "ProjectXBrokerage.Disconnect(): Error waiting for heartbeat to stop");
                        }
                    }
                }

                CleanupClients();

                // Reset cancellation token source
                _connectionCts?.Dispose();
                _connectionCts = new CancellationTokenSource();

                Log.Trace("ProjectXBrokerage.Disconnect(): Successfully disconnected from ProjectX");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, "DISCONNECTED", "Disconnected from ProjectX"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.Disconnect(): Error during disconnection");

                // Ensure we're marked as disconnected even if there's an error
                lock (_connectionLock)
                {
                    _isConnected = false;
                }

                // Don't throw - disconnection should always succeed
            }
        }

        #endregion
               
        

        /// <summary>
        /// Gets the history for the requested symbols
        /// <see cref="IBrokerage.GetHistory(Data.HistoryRequest)"/>
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(Data.HistoryRequest request)
        {
            if (!CanSubscribe(request.Symbol))
            {
                Log.Trace($"ProjectXBrokerage.GetHistory(): Cannot subscribe to {request.Symbol}");
                return null;
            }

            Log.Trace($"ProjectXBrokerage.GetHistory(): Requesting history for {request.Symbol}, Start: {request.StartTimeUtc}, End: {request.EndTimeUtc}, Resolution: {request.Resolution}");
            if (request.Resolution == Resolution.Tick)
            {
                Log.Trace($"ProjectXBrokerage.GetHistory(): Tick resolution is not supported by the ProjectX API. Symbol: {request.Symbol}");
                return null;
            }

            try
            {
                if (!IsConnected)
                {
                    Log.Error("ProjectXBrokerage.GetHistory(): Not connected to ProjectX");
                    return null;
                }

                var contractId = _symbolMapper.GetBrokerageSymbol(request.Symbol);

                PxAggregateBarUnit unit;
                int limit;
                switch (request.Resolution)
                {
                    case Resolution.Second:
                        unit = PxAggregateBarUnit.Second;
                        limit = (int)Math.Ceiling((request.EndTimeUtc - request.StartTimeUtc).TotalSeconds) + 1;
                        break;
                    case Resolution.Minute:
                        unit = PxAggregateBarUnit.Minute;
                        limit = (int)Math.Ceiling((request.EndTimeUtc - request.StartTimeUtc).TotalMinutes) + 1;
                        break;
                    case Resolution.Hour:
                        unit = PxAggregateBarUnit.Hour;
                        limit = (int)Math.Ceiling((request.EndTimeUtc - request.StartTimeUtc).TotalHours) + 1;
                        break;
                    case Resolution.Daily:
                        unit = PxAggregateBarUnit.Day;
                        limit = (int)Math.Ceiling((request.EndTimeUtc - request.StartTimeUtc).TotalDays) + 1;
                        break;
                    default:
                        Log.Trace($"ProjectXBrokerage.GetHistory(): Resolution {request.Resolution} is not supported");
                        return null;
                }

                // Cap at a reasonable maximum to avoid excessive API calls
                limit = Math.Min(limit, 10000);

                var bars = _apiClient.GetHistoricalBarsAsync(
                    contractId,
                    request.StartTimeUtc,
                    request.EndTimeUtc,
                    unit,
                    1,
                    limit,
                    false,
                    CancellationToken.None
                ).GetAwaiter().GetResult();

                return ConvertHistoricalBars(bars, request.Symbol, request.Resolution);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.GetHistory(): Error retrieving history for {request.Symbol}");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "HISTORY_ERROR",
                    $"Error retrieving history for {request.Symbol}: {ex.Message}"));
                return null;
            }
        }

        private IEnumerable<BaseData> ConvertHistoricalBars(IEnumerable<PxAggregateBar> bars, Symbol symbol, Resolution resolution)
        {
            var period = resolution.ToTimeSpan();
            foreach (var bar in bars)
            {
                yield return new TradeBar(bar.Timestamp, symbol, bar.Open, bar.High, bar.Low, bar.Close, (decimal)bar.Volume, period);
            }
        }

        #region Connection Management Helper Methods

        /// <summary>
        /// Loads configuration from LEAN config or environment variables
        /// </summary>
        private void LoadConfiguration()
        {
            Log.Trace("ProjectXBrokerage.LoadConfiguration(): Loading configuration");

            // Load API credentials
            _apiKey = Config.Get("brokerage-project-x-api-key", string.Empty);
            _apiSecret = Config.Get("brokerage-project-x-api-secret", string.Empty);

            // Load environment setting
            _environment = Config.Get("brokerage-project-x-environment", "production");
            _accountId = Config.GetInt("brokerage-project-x-account-id", 0);

            LoadOptionalConfiguration();
        }

        /// <summary>
        /// Loads optional configuration items like retries, timeouts, and base URLs
        /// </summary>
        private void LoadOptionalConfiguration()
        {
            // Load retry/reconnect settings
            _maxReconnectAttempts = Config.GetInt("brokerage-project-x-reconnect-attempts", 5);
            _reconnectDelayMilliseconds = Config.GetInt("brokerage-project-x-reconnect-delay", 1000);
            _connectionTimeoutMilliseconds = Config.GetInt("brokerage-project-x-connection-timeout", 30000);
            _baseUrl = Config.Get("brokerage-project-x-base-url", "https://gateway.projectx.com/api");

            Log.Debug($"ProjectXBrokerage.LoadConfiguration(): Environment={_environment}, MaxRetries={_maxReconnectAttempts}, AccountId={_accountId}");
        }

        /// <summary>
        /// Validates that all required configuration is present
        /// </summary>
        private void ValidateConfiguration()
        {
            Log.Debug("ProjectXBrokerage.ValidateConfiguration(): Validating configuration");

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new ArgumentException("ProjectX API key is required. Set 'brokerage-project-x-api-key' in configuration.");
            }

            if (string.IsNullOrWhiteSpace(_apiSecret))
            {
                throw new ArgumentException("ProjectX API secret is required. Set 'brokerage-project-x-api-secret' in configuration.");
            }

            if (_environment != "sandbox" && _environment != "production")
            {
                throw new ArgumentException($"Invalid environment '{_environment}'. Must be 'sandbox' or 'production'.");
            }

            if (_maxReconnectAttempts < 1 || _maxReconnectAttempts > 20)
            {
                throw new ArgumentException($"Invalid reconnect attempts '{_maxReconnectAttempts}'. Must be between 1 and 20.");
            }

            if (_accountId <= 0)
            {
                throw new ArgumentException("ProjectX account ID is required. Set 'brokerage-project-x-account-id' in configuration.");
            }

            Log.Debug("ProjectXBrokerage.ValidateConfiguration(): Configuration is valid");
        }

        /// <summary>
        /// Attempts to connect with retry logic and exponential backoff
        /// </summary>
        /// <returns>True if connected successfully, false otherwise</returns>
        private bool ConnectWithRetry()
        {
            var attempt = 0;
            var delay = _reconnectDelayMilliseconds;
            var maxDelay = 60000; // 60 seconds max delay

            while (attempt < _maxReconnectAttempts)
            {
                attempt++;
                Log.Debug($"ProjectXBrokerage.ConnectWithRetry(): Attempt {attempt}/{_maxReconnectAttempts}");

                try
                {
                    // Attempt the actual connection
                    var success = AttemptConnection();

                    if (success)
                    {
                        Log.Trace($"ProjectXBrokerage.ConnectWithRetry(): Successfully connected on attempt {attempt}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"ProjectXBrokerage.ConnectWithRetry(): Connection attempt {attempt} failed");

                    if (attempt < _maxReconnectAttempts)
                    {
                        // Fire message event for retry
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "CONNECTION_RETRY", 
                            $"Connection failed. Retrying in {delay}ms (attempt {attempt}/{_maxReconnectAttempts})"));

                        // Wait before retry with exponential backoff
                        Thread.Sleep(delay);

                        // Exponential backoff: double the delay, but cap at maxDelay
                        delay = Math.Min(delay * 2, maxDelay);
                    }
                }
            }

            Log.Error($"ProjectXBrokerage.ConnectWithRetry(): Failed to connect after {_maxReconnectAttempts} attempts");
            return false;
        }

        /// <summary>
        /// Performs a single connection attempt
        /// </summary>
        /// <returns>True if connected successfully, false otherwise</returns>
        private bool AttemptConnection()
        {
            Log.Debug("ProjectXBrokerage.AttemptConnection(): Attempting connection");

            try
            {
                // Cleanup any leftover clients from a previous session
                CleanupClients();

                // Build the DI container with the ProjectX client and its dependencies
                var services = new ServiceCollection();
                services.AddLogging();

                var configValues = new Dictionary<string, string?>
                {
                    ["ProjectX:ApiKey"] = _apiKey,
                    ["ProjectX:ApiSecret"] = _apiSecret,
                    ["ProjectX:BaseUrl"] = _baseUrl
                };

                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(configValues)
                    .Build();

                services.AddProjectXApiClient(configuration);

                _serviceProvider = services.BuildServiceProvider();
                _apiClient = _serviceProvider.GetRequiredService<IProjectXApiClient>();
                _wsClient = _serviceProvider.GetRequiredService<IProjectXWebSocketClient>();

                // Wire up WebSocket events before connecting
                _wsClient.ConnectionStatusChanged += OnConnectionStatusChanged;
                _wsClient.OrderUpdateReceived += OnOrderUpdateReceived;
                _wsClient.PriceUpdateReceived += OnPriceUpdateReceived;
                _wsClient.TradeUpdateReceived += OnTradeUpdateReceived;

                // Connect both SignalR hubs
                _wsClient.ConnectMarketHubAsync(_connectionCts.Token).GetAwaiter().GetResult();
                _wsClient.ConnectUserHubAsync(_connectionCts.Token).GetAwaiter().GetResult();

                lock (_connectionLock)
                {
                    _isConnected = true;
                }

                // Subscribe to real-time order updates for this account
                SubscribeToAccountUpdates();

                // Reconcile any positions that existed before this session
                ReconcilePositions();

                // Start heartbeat monitoring
                StartHeartbeat();

                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, "CONNECTED", "Connected to ProjectX"));

                Log.Trace("ProjectXBrokerage.AttemptConnection(): Connection successful");
                return true;
            }
            catch (AuthenticationException ex)
            {
                Log.Error(ex, "ProjectXBrokerage.AttemptConnection(): Authentication failed - check API key and secret");

                lock (_connectionLock)
                {
                    _isConnected = false;
                }

                CleanupClients();
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.AttemptConnection(): Connection failed");

                lock (_connectionLock)
                {
                    _isConnected = false;
                }

                CleanupClients();
                throw;
            }
        }

        /// <summary>
        /// Starts the heartbeat monitoring task
        /// </summary>
        private void StartHeartbeat()
        {
            if (_heartbeatTask != null && !_heartbeatTask.IsCompleted)
            {
                Log.Debug("ProjectXBrokerage.StartHeartbeat(): Heartbeat already running");
                return;
            }

            Log.Debug("ProjectXBrokerage.StartHeartbeat(): Starting heartbeat monitoring");

            _heartbeatTask = Task.Run(async () =>
            {
                while (!_connectionCts.Token.IsCancellationRequested && IsConnected)
                {
                    try
                    {
                        await Task.Delay(_heartbeatInterval, _connectionCts.Token);

                        if (_connectionCts.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        if (_wsClient == null ||
                            _wsClient.MarketHubState != ConnectionState.Connected ||
                            _wsClient.UserHubState != ConnectionState.Connected)
                        {
                            throw new Exception($"WebSocket connection degraded. " +
                                $"Market: {_wsClient?.MarketHubState}, User: {_wsClient?.UserHubState}");
                        }

                        Log.Trace("ProjectXBrokerage.Heartbeat(): WebSocket connections healthy");
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when disconnecting
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "ProjectXBrokerage.Heartbeat(): Error during heartbeat");

                        // Attempt reconnection on heartbeat failure
                        if (IsConnected)
                        {
                            Log.Error("ProjectXBrokerage.Heartbeat(): Triggering reconnection due to heartbeat failure");
                            _ = Task.Run(() => HandleReconnection());
                        }

                        break;
                    }
                }

                Log.Debug("ProjectXBrokerage.StartHeartbeat(): Heartbeat monitoring stopped");
            }, _connectionCts.Token);
        }

        /// <summary>
        /// Handles reconnection logic
        /// </summary>
        private void HandleReconnection()
        {
            if (!_connectionSemaphore.Wait(0))
            {
                Log.Debug("ProjectXBrokerage.HandleReconnection(): Reconnection already in progress");
                return;
            }

            try
            {
                Log.Error("ProjectXBrokerage.HandleReconnection(): Connection lost, attempting to reconnect");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "RECONNECTING", "Connection lost, attempting to reconnect"));

                // Mark as disconnected
                lock (_connectionLock)
                {
                    _isConnected = false;
                }

                // Reset the cancellation token for the new connection attempt
                if (_connectionCts == null || _connectionCts.IsCancellationRequested)
                {
                    _connectionCts?.Dispose();
                    _connectionCts = new CancellationTokenSource();
                }

                // Attempt reconnection
                var reconnected = ConnectWithRetry();

                if (reconnected)
                {
                    Log.Trace("ProjectXBrokerage.HandleReconnection(): Reconnection successful");
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, "RECONNECTED", "Successfully reconnected to ProjectX"));

                    // Resubscribe to account updates
                    SubscribeToAccountUpdates();

                    // Resync account state
                    ReconcilePositions();

                    // TODO: Resubscribe to data feeds
                }
                else
                {
                    Log.Error("ProjectXBrokerage.HandleReconnection(): Reconnection failed");
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "RECONNECT_FAILED", "Failed to reconnect to ProjectX"));
                }
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        #endregion

        #region Account Synchronization Helper Methods

        /// <summary>
        /// Converts a ProjectX position to a LEAN Holding object
        /// </summary>
        /// <param name="projectXPosition">The ProjectX position object</param>
        /// <summary>
        /// Converts a ProjectX <see cref="PxPosition"/> to a LEAN <see cref="Holding"/>.
        /// </summary>
        private Holding ConvertFromProjectXPosition(PxPosition pxPosition)
        {
            var symbol = _symbolMapper.GetLeanSymbol(pxPosition.ContractId, SecurityType.Future, string.Empty);

            // Long positions have positive quantity; short positions are negative
            var quantity = pxPosition.Type == PxPositionType.Long
                ? (decimal)pxPosition.Size
                : -(decimal)pxPosition.Size;

            Log.Debug($"ProjectXBrokerage.ConvertFromProjectXPosition(): {pxPosition.ContractId} -> {symbol}, qty={quantity}, avg={pxPosition.AveragePrice}");

            return new Holding
            {
                Symbol = symbol,
                Quantity = quantity,
                AveragePrice = pxPosition.AveragePrice,
                MarketPrice = pxPosition.AveragePrice,
                CurrencySymbol = "$",
                ConversionRate = 1m
            };
        }

        /// <summary>
        /// Reconciles positions and cash balance with ProjectX on connection.
        /// ProjectX is the source of truth; discrepancies are logged and balance events are fired.
        /// </summary>
        private void ReconcilePositions()
        {
            try
            {
                Log.Trace("ProjectXBrokerage.ReconcilePositions(): Starting position reconciliation");

                var holdings = GetAccountHoldings();
                var cashBalances = GetCashBalance();

                Log.Debug($"ProjectXBrokerage.ReconcilePositions(): {holdings.Count} position(s), {cashBalances.Count} cash balance(s)");

                foreach (var holding in holdings)
                {
                    Log.Debug($"ProjectXBrokerage.ReconcilePositions(): Open position — {holding.Symbol}: qty={holding.Quantity}, avgPrice={holding.AveragePrice}");
                }

                foreach (var cash in cashBalances)
                {
                    Log.Debug($"ProjectXBrokerage.ReconcilePositions(): Cash balance — {cash.Amount} {cash.Currency}");
                    OnAccountChanged(new AccountEvent(cash.Currency, cash.Amount));
                }

                Log.Trace("ProjectXBrokerage.ReconcilePositions(): Reconciliation complete");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.ReconcilePositions(): Error during position reconciliation");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "RECONCILIATION_ERROR",
                    $"Error during position reconciliation: {ex.Message}"));
            }
        }

        /// <summary>
        /// Handles account update events from ProjectX WebSocket
        /// </summary>
        /// <param name="accountUpdate">The account update object from ProjectX</param>
        private void HandleAccountUpdate(object accountUpdate)
        {
            try
            {
                // TODO: Replace with actual MarqSpec.Client.ProjectX types when available
                // Example implementation:
                //
                // var update = (ProjectXAccountUpdate)accountUpdate;
                //
                // Log.Debug($"ProjectXBrokerage.HandleAccountUpdate(): Received account update - Type: {update.UpdateType}");
                //
                // switch (update.UpdateType)
                // {
                //     case AccountUpdateType.Balance:
                //         // Balance changed
                //         if (update.AvailableCash.HasValue)
                //         {
                //             var currency = string.IsNullOrEmpty(update.Currency) ? "USD" : update.Currency;
                //             OnAccountChanged(new AccountEvent(currency, update.AvailableCash.Value));
                //             Log.Debug($"ProjectXBrokerage.HandleAccountUpdate(): Balance updated - {update.AvailableCash.Value} {currency}");
                //         }
                //         break;
                //
                //     case AccountUpdateType.Position:
                //         // Position changed
                //         if (update.Position != null)
                //         {
                //             var holding = ConvertFromProjectXPosition(update.Position);
                //             // Cache updated position
                //             Log.Debug($"ProjectXBrokerage.HandleAccountUpdate(): Position updated - {holding.Symbol}: {holding.Quantity}");
                //         }
                //         break;
                //
                //     case AccountUpdateType.RealizedPnL:
                //         // Realized P&L from closed position
                //         Log.Debug($"ProjectXBrokerage.HandleAccountUpdate(): Realized P&L - {update.RealizedPnL}");
                //         break;
                //
                //     default:
                //         Log.Debug($"ProjectXBrokerage.HandleAccountUpdate(): Unknown update type: {update.UpdateType}");
                //         break;
                // }

                Log.Trace("ProjectXBrokerage.HandleAccountUpdate(): MarqSpec.Client.ProjectX integration pending");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.HandleAccountUpdate(): Error handling account update");
            }
        }

        /// <summary>
        /// Subscribes to account update events via WebSocket
        /// </summary>
        private void SubscribeToAccountUpdates()
        {
            try
            {
                Log.Debug("ProjectXBrokerage.SubscribeToAccountUpdates(): Subscribing to account order updates");

                _wsClient.SubscribeToOrderUpdatesAsync(_accountId, _connectionCts.Token).GetAwaiter().GetResult();

                Log.Trace("ProjectXBrokerage.SubscribeToAccountUpdates(): Successfully subscribed to account order updates");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.SubscribeToAccountUpdates(): Error subscribing to account updates");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ACCOUNT_SUBSCRIPTION_ERROR",
                    $"Error subscribing to account updates: {ex.Message}"));
            }
        }

        #endregion

        #region Order Management Helper Methods

        /// <summary>
        /// Validates an order before submission
        /// </summary>
        /// <param name="order">The order to validate</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if order is valid, false otherwise</returns>
        private bool ValidateOrder(Order order, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                // Check connection
                if (!IsConnected)
                {
                    errorMessage = "Not connected to ProjectX";
                    Log.Error($"ProjectXBrokerage.ValidateOrder(): {errorMessage}");
                    return false;
                }

                // Validate order object
                if (order == null)
                {
                    errorMessage = "Order cannot be null";
                    Log.Error($"ProjectXBrokerage.ValidateOrder(): {errorMessage}");
                    return false;
                }

                // Validate symbol
                if (order.Symbol == null || string.IsNullOrWhiteSpace(order.Symbol.Value))
                {
                    errorMessage = "Order symbol is required";
                    Log.Error($"ProjectXBrokerage.ValidateOrder(): {errorMessage}");
                    return false;
                }

                // Check if symbol can be subscribed (basic validation)
                if (!CanSubscribe(order.Symbol))
                {
                    errorMessage = $"Symbol {order.Symbol} is not supported for trading";
                    Log.Error($"ProjectXBrokerage.ValidateOrder(): {errorMessage}");
                    return false;
                }

                // Validate quantity
                if (order.Quantity == 0)
                {
                    errorMessage = "Order quantity cannot be zero";
                    Log.Error($"ProjectXBrokerage.ValidateOrder(): {errorMessage}");
                    return false;
                }

                // Validate order type support
                switch (order.Type)
                {
                    case OrderType.Market:
                    case OrderType.Limit:
                    case OrderType.StopMarket:
                    case OrderType.StopLimit:
                        // Supported order types
                        break;

                    case OrderType.MarketOnOpen:
                    case OrderType.MarketOnClose:
                    case OrderType.OptionExercise:
                    case OrderType.LimitIfTouched:
                    case OrderType.TrailingStop:
                    case OrderType.ComboMarket:
                    case OrderType.ComboLimit:
                    case OrderType.ComboLegLimit:
                        errorMessage = $"Order type {order.Type} is not supported by ProjectX";
                        Log.Error($"ProjectXBrokerage.ValidateOrder(): {errorMessage}");
                        return false;

                    default:
                        errorMessage = $"Unknown order type {order.Type}";
                        Log.Error($"ProjectXBrokerage.ValidateOrder(): {errorMessage}");
                        return false;
                }

                // Validate Limit order has limit price
                if (order.Type == OrderType.Limit || order.Type == OrderType.StopLimit)
                {
                    var limitOrder = order as Orders.LimitOrder;
                    if (limitOrder != null && limitOrder.LimitPrice <= 0)
                    {
                        errorMessage = "Limit price must be greater than zero";
                        Log.Error($"ProjectXBrokerage.ValidateOrder(): {errorMessage}");
                        return false;
                    }
                }

                // Validate Stop order has stop price
                if (order.Type == OrderType.StopMarket || order.Type == OrderType.StopLimit)
                {
                    var stopOrder = order as Orders.StopMarketOrder;
                    if (stopOrder != null && stopOrder.StopPrice <= 0)
                    {
                        errorMessage = "Stop price must be greater than zero";
                        Log.Error($"ProjectXBrokerage.ValidateOrder(): {errorMessage}");
                        return false;
                    }
                }

                Log.Debug($"ProjectXBrokerage.ValidateOrder(): Order validation successful for OrderId={order.Id}");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Validation error: {ex.Message}";
                Log.Error(ex, $"ProjectXBrokerage.ValidateOrder(): Exception during validation for OrderId={order.Id}");
                return false;
            }
        }

        /// <summary>
        /// Checks if an order is a duplicate submission
        /// </summary>
        /// <param name="order">The order to check</param>
        /// <returns>True if this is a duplicate submission, false otherwise</returns>
        private bool IsDuplicateSubmission(Order order)
        {
            try
            {
                // Check if order was recently submitted
                if (_recentlySubmittedOrders.TryGetValue(order.Id, out var submissionTime))
                {
                    var timeSinceSubmission = DateTime.UtcNow - submissionTime;

                    if (timeSinceSubmission < _duplicateDetectionWindow)
                    {
                        Log.Error($"ProjectXBrokerage.IsDuplicateSubmission(): Duplicate submission detected for OrderId={order.Id}. " +
                                  $"Last submitted {timeSinceSubmission.TotalSeconds:F2} seconds ago.");
                        return true;
                    }
                    else
                    {
                        // Outside detection window, remove old entry
                        _recentlySubmittedOrders.TryRemove(order.Id, out _);
                    }
                }

                // Cleanup old entries periodically (every 100 checks)
                if (_recentlySubmittedOrders.Count > 100)
                {
                    var cutoffTime = DateTime.UtcNow - _duplicateDetectionWindow;
                    var expiredKeys = _recentlySubmittedOrders
                        .Where(kvp => kvp.Value < cutoffTime)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in expiredKeys)
                    {
                        _recentlySubmittedOrders.TryRemove(key, out _);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.IsDuplicateSubmission(): Error checking for duplicate OrderId={order.Id}");
                // On error, allow submission (fail open)
                return false;
            }
        }

        /// <summary>
        /// Adds a bidirectional order ID mapping
        /// </summary>
        /// <param name="leanId">LEAN order ID</param>
        /// <param name="projectXId">ProjectX order ID</param>
        private void AddOrderIdMapping(int leanId, long projectXId)
        {
            try
            {
                lock (_orderMappingLock)
                {
                    // Add to both dictionaries for bidirectional lookup
                    _leanToProjectXOrderIds[leanId] = projectXId;
                    _projectXToLeanOrderIds[projectXId] = leanId;

                    Log.Debug($"ProjectXBrokerage.AddOrderIdMapping(): Added mapping LEAN ID={leanId} <-> ProjectX ID={projectXId}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.AddOrderIdMapping(): Error adding mapping for LEAN ID={leanId}, ProjectX ID={projectXId}");
            }
        }

        /// <summary>
        /// Removes an order ID mapping (both directions)
        /// </summary>
        /// <param name="leanId">LEAN order ID to remove</param>
        private void RemoveOrderIdMapping(int leanId)
        {
            try
            {
                lock (_orderMappingLock)
                {
                    // Remove from both dictionaries
                    if (_leanToProjectXOrderIds.TryRemove(leanId, out var projectXId))
                    {
                        _projectXToLeanOrderIds.TryRemove(projectXId, out _);
                        Log.Debug($"ProjectXBrokerage.RemoveOrderIdMapping(): Removed mapping for LEAN ID={leanId}, ProjectX ID={projectXId}");
                    }
                    else
                    {
                        Log.Debug($"ProjectXBrokerage.RemoveOrderIdMapping(): No mapping found for LEAN ID={leanId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.RemoveOrderIdMapping(): Error removing mapping for LEAN ID={leanId}");
            }
        }

        /// <summary>
        /// Converts a LEAN Order to ProjectX order format
        /// </summary>
        /// <param name="order">LEAN order to convert</param>
        /// <returns>ProjectX order request object</returns>
        private PlaceOrderRequest ConvertToProjectXOrder(Order order)
        {
            var side = order.Direction == OrderDirection.Buy ? PxOrderSide.Bid : PxOrderSide.Ask;
            var pxType = ConvertToProjectXOrderType(order.Type);

            // Phase 3: replace order.Symbol.Value with _symbolMapper.GetBrokerageSymbol(order.Symbol)
            var request = new PlaceOrderRequest
            {
                AccountId = _accountId,
                ContractId = _symbolMapper.GetBrokerageSymbol(order.Symbol),
                Type = pxType,
                Side = side,
                Size = (int)Math.Abs(order.Quantity),
                CustomTag = order.Id.ToString()
            };

            if (order is LimitOrder limitOrder)
                request.LimitPrice = limitOrder.LimitPrice;
            else if (order is StopLimitOrder stopLimitOrder)
            {
                request.LimitPrice = stopLimitOrder.LimitPrice;
                request.StopPrice = stopLimitOrder.StopPrice;
            }
            else if (order is StopMarketOrder stopMarketOrder)
                request.StopPrice = stopMarketOrder.StopPrice;

            Log.Debug($"ProjectXBrokerage.ConvertToProjectXOrder(): LEAN {order.Id} -> ContractId={request.ContractId}, Type={pxType}, Side={side}, Size={request.Size}");
            return request;
        }

        /// <summary>
        /// Converts a ProjectX order to LEAN Order format
        /// </summary>
        /// <param name="projectXOrder">ProjectX order object</param>
        /// <returns>LEAN Order object</returns>
        private Order ConvertFromProjectXOrder(PxOrder pxOrder)
        {
            // Phase 3: replace with _symbolMapper.GetLeanSymbol(pxOrder.ContractId, SecurityType.Future, Market.USA)
            var symbol = _symbolMapper.GetLeanSymbol(pxOrder.ContractId, SecurityType.Future, string.Empty);

            var qty = (decimal)pxOrder.Size * (pxOrder.Side == PxOrderSide.Bid ? 1m : -1m);
            var time = pxOrder.CreationTimestamp;

            Order leanOrder;
            switch (pxOrder.Type)
            {
                case PxOrderType.Market:
                    leanOrder = new MarketOrder(symbol, qty, time);
                    break;
                case PxOrderType.Limit:
                    leanOrder = new LimitOrder(symbol, qty, pxOrder.LimitPrice ?? 0m, time);
                    break;
                case PxOrderType.Stop:
                    leanOrder = new StopMarketOrder(symbol, qty, pxOrder.StopPrice ?? 0m, time);
                    break;
                case PxOrderType.StopLimit:
                    leanOrder = new StopLimitOrder(symbol, qty, pxOrder.StopPrice ?? 0m, pxOrder.LimitPrice ?? 0m, time);
                    break;
                default:
                    throw new NotSupportedException($"ProjectX order type '{pxOrder.Type}' is not supported");
            }

            leanOrder.BrokerId.Add(pxOrder.Id.ToString());

            // If placed through this brokerage, recover the LEAN order ID from CustomTag and update caches
            if (!string.IsNullOrEmpty(pxOrder.CustomTag) && int.TryParse(pxOrder.CustomTag, out var leanId))
            {
                AddOrderIdMapping(leanId, pxOrder.Id);
                _ordersCache[pxOrder.Id] = leanOrder;
            }

            Log.Debug($"ProjectXBrokerage.ConvertFromProjectXOrder(): ProjectX {pxOrder.Id} -> {leanOrder.Type}, Qty={qty}, Symbol={symbol}");
            return leanOrder;
        }

        /// <summary>
        /// Handles order rejection by firing appropriate events
        /// </summary>
        /// <param name="order">The rejected order</param>
        /// <param name="errorCode">ProjectX error code</param>
        /// <param name="errorMessage">Error message from ProjectX</param>
        private void HandleOrderRejection(Order order, string errorCode, string errorMessage)
        {
            try
            {
                var fullMessage = $"Order rejected by ProjectX. Code: {errorCode}, Message: {errorMessage}";
                Log.Error($"ProjectXBrokerage.HandleOrderRejection(): OrderId={order.Id}, {fullMessage}");

                // Fire order event with Invalid status
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "ProjectX Order Rejected")
                {
                    Status = OrderStatus.Invalid,
                    Message = fullMessage
                });

                // Fire brokerage message for user notification
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, errorCode, fullMessage));

                // Remove from recently submitted (if present)
                _recentlySubmittedOrders.TryRemove(order.Id, out _);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.HandleOrderRejection(): Error handling rejection for OrderId={order.Id}");
            }
        }

        #endregion

        #region WebSocket Event Handlers

        /// <summary>
        /// Handles WebSocket connection status changes; triggers reconnection on failure.
        /// </summary>
        private void OnConnectionStatusChanged(object sender, ConnectionStatusChange e)
        {
            Log.Debug($"ProjectXBrokerage.OnConnectionStatusChanged(): {e.PreviousState} -> {e.CurrentState}");

            if ((e.CurrentState == ConnectionState.Failed || e.CurrentState == ConnectionState.Disconnected) && IsConnected)
            {
                Log.Error($"ProjectXBrokerage.OnConnectionStatusChanged(): Connection lost. Error: {e.ErrorMessage}");
                _ = Task.Run(() => HandleReconnection());
            }
        }

        /// <summary>
        /// Handles real-time order update events from the ProjectX WebSocket.
        /// </summary>
        private void OnOrderUpdateReceived(object sender, PxOrderUpdate e)
        {
            try
            {
                Log.Debug($"ProjectXBrokerage.OnOrderUpdateReceived(): OrderId={e.OrderId}, Status={e.Status}");

                if (!_ordersCache.TryGetValue(e.OrderId, out var order))
                {
                    Log.Debug($"ProjectXBrokerage.OnOrderUpdateReceived(): No cached LEAN order for ProjectX ID={e.OrderId}");
                    return;
                }

                var leanStatus = ConvertOrderStatus(e.Status);
                var fillPrice = e.AverageFillPrice ?? 0m;
                var filledQty = (decimal)e.FilledQuantity;
                var signedFillQty = filledQty * (order.Direction == Orders.OrderDirection.Buy ? 1m : -1m);

                var orderEvent = new OrderEvent(order, e.Timestamp, OrderFee.Zero, $"ProjectX: {e.Status}")
                {
                    Status = leanStatus,
                    FillPrice = fillPrice,
                    FillQuantity = signedFillQty
                };

                OnOrderEvent(orderEvent);

                // Clean up mappings for terminal states
                if (leanStatus == OrderStatus.Filled || leanStatus == OrderStatus.Canceled || leanStatus == OrderStatus.Invalid)
                {
                    _ordersCache.TryRemove(e.OrderId, out _);
                    RemoveOrderIdMapping(order.Id);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.OnOrderUpdateReceived(): Error processing update for ProjectX order {e.OrderId}");
            }
        }

        /// <summary>
        /// Maps a LEAN <see cref="OrderType"/> to the equivalent ProjectX <see cref="PxOrderType"/>.
        /// </summary>
        private static PxOrderType ConvertToProjectXOrderType(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Market:    return PxOrderType.Market;
                case OrderType.Limit:     return PxOrderType.Limit;
                case OrderType.StopMarket: return PxOrderType.Stop;
                case OrderType.StopLimit: return PxOrderType.StopLimit;
                default:
                    throw new NotSupportedException($"LEAN OrderType.{orderType} is not supported by ProjectX");
            }
        }

        /// <summary>
        /// Maps a ProjectX <see cref="PxOrderStatus"/> to the equivalent LEAN <see cref="OrderStatus"/>.
        /// </summary>
        private static OrderStatus ConvertOrderStatus(PxOrderStatus status)
        {
            switch (status)
            {
                case PxOrderStatus.Accepted:
                case PxOrderStatus.Pending:
                    return OrderStatus.Submitted;
                case PxOrderStatus.Triggered:
                case PxOrderStatus.PartiallyFilled:
                    return OrderStatus.PartiallyFilled;
                case PxOrderStatus.Filled:
                    return OrderStatus.Filled;
                case PxOrderStatus.Cancelled:
                    return OrderStatus.Canceled;
                case PxOrderStatus.Rejected:
                    return OrderStatus.Invalid;
                case PxOrderStatus.Expired:
                    return OrderStatus.Canceled;
                default:
                    Log.Debug($"ProjectXBrokerage.ConvertOrderStatus(): Unknown status '{status}', returning None");
                    return OrderStatus.None;
            }
        }

        #endregion

        #region Client Lifecycle

        /// <summary>
        /// Unwires events and disposes WebSocket and DI service provider.
        /// </summary>
        private void CleanupClients()
        {
            try
            {
                if (_wsClient != null)
                {
                    _wsClient.ConnectionStatusChanged -= OnConnectionStatusChanged;
                    _wsClient.OrderUpdateReceived -= OnOrderUpdateReceived;
                }

                _serviceProvider?.Dispose();
                _serviceProvider = null;
                _apiClient = null;
                _wsClient = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.CleanupClients(): Error during client cleanup");
            }
        }

        #endregion
    }
}
