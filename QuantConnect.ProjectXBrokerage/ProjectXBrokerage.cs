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

namespace QuantConnect.Brokerages.ProjectXBrokerage
{
    [BrokerageFactory(typeof(ProjectXBrokerageFactory))]
    public class ProjectXBrokerage : Brokerage, IDataQueueHandler, IDataQueueUniverseProvider
    {
        private readonly IDataAggregator _aggregator;
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        // Connection state
        private volatile bool _isConnected;
        private readonly object _connectionLock = new object();
        private readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);

        // Order ID mapping - Thread-safe bidirectional mapping
        private readonly ConcurrentDictionary<int, string> _leanToProjectXOrderIds = new ConcurrentDictionary<int, string>();
        private readonly ConcurrentDictionary<string, int> _projectXToLeanOrderIds = new ConcurrentDictionary<string, int>();
        private readonly object _orderMappingLock = new object();

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

        // TODO: Replace with actual MarqSpec.Client.ProjectX instance when available
        // private readonly IProjectXClient _apiClient;

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
        /// Creates a new instance
        /// </summary>
        /// <param name="aggregator">consolidate ticks</param>
        public ProjectXBrokerage(IDataAggregator aggregator) : base("ProjectXBrokerage")
        {
            Log.Trace("ProjectXBrokerage(): Initializing ProjectX brokerage instance");

            _aggregator = aggregator;
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

        #region IDataQueueHandler

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                Log.Trace($"ProjectXBrokerage.Subscribe(): Subscription not allowed for symbol: {dataConfig.Symbol}");
                return null;
            }

            Log.Trace($"ProjectXBrokerage.Subscribe(): Subscribing to {dataConfig.Symbol}, Resolution: {dataConfig.Resolution}");

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            Log.Trace($"ProjectXBrokerage.Unsubscribe(): Unsubscribing from {dataConfig.Symbol}");

            _subscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
            Log.Trace($"ProjectXBrokerage.SetJob(): Job UserId: {job?.UserId}, AlgorithmId: {job?.AlgorithmId}");
            throw new NotImplementedException("ProjectXBrokerage.SetJob(): Implementation pending Phase 2");
        }

        #endregion

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

                // TODO: Replace with actual API call when MarqSpec.Client.ProjectX is integrated
                // Example:
                // var projectXOrders = await _apiClient.GetOpenOrdersAsync();

                // For now, return empty list (Phase 2.2 stub - will be implemented with real API)
                // When implementing:
                // 1. Query open orders from ProjectX API
                // 2. Convert each ProjectX order to LEAN Order using ConvertFromProjectXOrder()
                // 3. Store new order ID mappings
                // 4. Filter out filled/canceled orders

                var openOrders = new List<Order>();

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
            throw new NotImplementedException("ProjectXBrokerage.GetAccountHoldings(): Implementation pending Phase 2");
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            Log.Trace("ProjectXBrokerage.GetCashBalance(): Retrieving cash balance");
            throw new NotImplementedException("ProjectXBrokerage.GetCashBalance(): Implementation pending Phase 2");
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
                // TODO: Implement symbol mapper and order conversion when MarqSpec.Client.ProjectX is integrated
                // var projectXOrderRequest = ConvertToProjectXOrder(order);

                // 4. Submit order via API
                // TODO: Replace with actual API call
                // Example:
                // var result = await _apiClient.PlaceOrderAsync(projectXOrderRequest);
                // if (!result.Success)
                // {
                //     HandleOrderRejection(order, result.ErrorCode, result.ErrorMessage);
                //     return false;
                // }

                // Simulate successful order placement (will be replaced with real API call)
                var projectXOrderId = $"PX{order.Id}"; // Simulated ProjectX order ID

                // 5. Store order ID mapping
                AddOrderIdMapping(order.Id, projectXOrderId);

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
                // 1. Check if ProjectX supports order modification
                // Note: Many brokerages don't support modifications, only cancel/replace
                Log.Error("ProjectXBrokerage.UpdateOrder(): Order modifications not currently supported. " +
                           "Cancel the order and place a new one instead.");
                return false;

                // TODO: If ProjectX supports modifications in the future, implement:
                // 1. Validate connection
                // 2. Retrieve ProjectX order ID from mapping
                // 3. Validate updated order parameters
                // 4. Check if order is still open
                // 5. Submit modification via API
                // 6. Fire OrderStatusChanged event
                // 7. Return true on success

                // Example implementation:
                // if (!IsConnected)
                // {
                //     Log.Error("ProjectXBrokerage.UpdateOrder(): Not connected");
                //     return false;
                // }
                //
                // if (!_leanToProjectXOrderIds.TryGetValue(order.Id, out var projectXOrderId))
                // {
                //     Log.Error($"ProjectXBrokerage.UpdateOrder(): Order ID {order.Id} not found in mapping");
                //     return false;
                // }
                //
                // var updateRequest = ConvertToProjectXOrderUpdate(order);
                // var result = await _apiClient.UpdateOrderAsync(projectXOrderId, updateRequest);
                //
                // if (result.Success)
                // {
                //     OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "ProjectX Order Updated")
                //     {
                //         Status = OrderStatus.UpdateSubmitted
                //     });
                //     return true;
                // }
                //
                // return false;
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
                // TODO: Replace with actual API call when MarqSpec.Client.ProjectX is integrated
                // Example:
                // var result = await _apiClient.CancelOrderAsync(projectXOrderId);
                // if (!result.Success)
                // {
                //     if (result.ErrorCode == "ORDER_ALREADY_FILLED")
                //     {
                //         Log.Warning($"ProjectXBrokerage.CancelOrder(): Order {order.Id} already filled");
                //         return false;
                //     }
                //     if (result.ErrorCode == "ORDER_NOT_FOUND")
                //     {
                //         Log.Warning($"ProjectXBrokerage.CancelOrder(): Order {order.Id} not found");
                //         RemoveOrderIdMapping(order.Id);
                //         return false;
                //     }
                //     throw new Exception($"Cancel failed: {result.ErrorMessage}");
                // }

                // Simulate successful cancellation (will be replaced with real API call)
                Log.Trace($"ProjectXBrokerage.CancelOrder(): Cancellation request submitted for Order {order.Id} (ProjectX ID: {projectXOrderId})");

                // 4. Fire order event (CancelPending status)
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

                // TODO: Close WebSocket connections
                // TODO: Dispose MarqSpec.Client.ProjectX resources
                // Example:
                // _apiClient?.Dispose();

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

        #region IDataQueueUniverseProvider

        /// <summary>
        /// Method returns a collection of Symbols that are available at the data source.
        /// </summary>
        /// <param name="symbol">Symbol to lookup</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="securityCurrency">Expected security currency(if any)</param>
        /// <returns>Enumerable of Symbols, that are associated with the provided Symbol</returns>
        public IEnumerable<Symbol> LookupSymbols(Symbol symbol, bool includeExpired, string securityCurrency = null)
        {
            Log.Trace($"ProjectXBrokerage.LookupSymbols(): Looking up symbols for {symbol}, IncludeExpired: {includeExpired}");
            throw new NotImplementedException("ProjectXBrokerage.LookupSymbols(): Implementation pending Phase 2");
        }

        /// <summary>
        /// Returns whether selection can take place or not.
        /// </summary>
        /// <remarks>This is useful to avoid a selection taking place during invalid times, for example IB reset times or when not connected,
        /// because if allowed selection would fail since IB isn't running and would kill the algorithm</remarks>
        /// <returns>True if selection can take place</returns>
        public bool CanPerformSelection()
        {
            Log.Trace("ProjectXBrokerage.CanPerformSelection(): Checking if selection can be performed");
            throw new NotImplementedException("ProjectXBrokerage.CanPerformSelection(): Implementation pending Phase 2");
        }

        #endregion

        private bool CanSubscribe(Symbol symbol)
        {
            if (symbol.Value.IndexOfInvariant("universe", true) != -1 || symbol.IsCanonical())
            {
                return false;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        private bool Subscribe(IEnumerable<Symbol> symbols)
        {
            var symbolList = symbols?.ToList() ?? new List<Symbol>();
            Log.Trace($"ProjectXBrokerage.Subscribe(): Subscribing to {symbolList.Count} symbols");
            throw new NotImplementedException("ProjectXBrokerage.Subscribe(): Implementation pending Phase 5");
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        private bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            var symbolList = symbols?.ToList() ?? new List<Symbol>();
            Log.Trace($"ProjectXBrokerage.Unsubscribe(): Unsubscribing from {symbolList.Count} symbols");
            throw new NotImplementedException("ProjectXBrokerage.Unsubscribe(): Implementation pending Phase 5");
        }

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
                return null; // Should consistently return null instead of an empty enumerable
            }

            Log.Trace($"ProjectXBrokerage.GetHistory(): Requesting history for {request.Symbol}, Start: {request.StartTimeUtc}, End: {request.EndTimeUtc}, Resolution: {request.Resolution}");
            throw new NotImplementedException("ProjectXBrokerage.GetHistory(): Implementation pending Phase 6");
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

            // Load retry/reconnect settings
            _maxReconnectAttempts = Config.GetInt("brokerage-project-x-reconnect-attempts", 5);
            _reconnectDelayMilliseconds = Config.GetInt("brokerage-project-x-reconnect-delay", 1000);
            _connectionTimeoutMilliseconds = Config.GetInt("brokerage-project-x-connection-timeout", 30000);

            Log.Debug($"ProjectXBrokerage.LoadConfiguration(): Environment={_environment}, MaxRetries={_maxReconnectAttempts}");
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
                // TODO: Replace with actual MarqSpec.Client.ProjectX connection code
                // Example:
                // _apiClient = new ProjectXClient(_apiKey, _apiSecret, _environment);
                // await _apiClient.ConnectAsync();
                // await _apiClient.AuthenticateAsync();

                // TODO: Initialize WebSocket connections
                // Example:
                // _webSocketClient = _apiClient.CreateWebSocketClient();
                // await _webSocketClient.ConnectAsync();

                // TODO: Subscribe to account updates
                // await _webSocketClient.SubscribeToAccountUpdatesAsync();

                // For now, simulate successful connection
                // This will be replaced when MarqSpec.Client.ProjectX is integrated
                lock (_connectionLock)
                {
                    _isConnected = true;
                }

                // Start heartbeat monitoring
                StartHeartbeat();

                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, "CONNECTED", "Connected to ProjectX"));

                Log.Trace("ProjectXBrokerage.AttemptConnection(): Connection successful");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.AttemptConnection(): Connection failed");

                lock (_connectionLock)
                {
                    _isConnected = false;
                }

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

                        // TODO: Implement actual heartbeat/ping
                        // Example:
                        // var pingResult = await _apiClient.PingAsync();
                        // if (!pingResult.Success)
                        // {
                        //     Log.Warning("ProjectXBrokerage.Heartbeat(): Ping failed, triggering reconnection");
                        //     await ReconnectAsync();
                        // }

                        Log.Trace("ProjectXBrokerage.Heartbeat(): Heartbeat successful");
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

                // Attempt reconnection
                var reconnected = ConnectWithRetry();

                if (reconnected)
                {
                    Log.Trace("ProjectXBrokerage.HandleReconnection(): Reconnection successful");
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, "RECONNECTED", "Successfully reconnected to ProjectX"));

                    // TODO: Resubscribe to data feeds
                    // TODO: Resync account state
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
        private void AddOrderIdMapping(int leanId, string projectXId)
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
        private object ConvertToProjectXOrder(Order order)
        {
            // TODO: Replace with actual MarqSpec.Client.ProjectX types when available
            // Example implementation:
            // 
            // var projectXOrder = new ProjectXOrderRequest
            // {
            //     Symbol = _symbolMapper.GetBrokerageSymbol(order.Symbol),
            //     Quantity = Math.Abs(order.Quantity),
            //     Side = order.Quantity > 0 ? OrderSide.Buy : OrderSide.Sell,
            //     Type = ConvertOrderType(order.Type),
            //     TimeInForce = ConvertTimeInForce(order.TimeInForce),
            //     LimitPrice = (order as LimitOrder)?.LimitPrice,
            //     StopPrice = (order as StopMarketOrder)?.StopPrice,
            //     ClientOrderId = order.Id.ToString()
            // };
            //
            // return projectXOrder;

            Log.Trace($"ProjectXBrokerage.ConvertToProjectXOrder(): Converting LEAN Order {order.Id} to ProjectX format");

            // Placeholder return - will be replaced when MarqSpec.Client.ProjectX is integrated
            throw new NotImplementedException("ProjectXBrokerage.ConvertToProjectXOrder(): MarqSpec.Client.ProjectX integration pending");
        }

        /// <summary>
        /// Converts a ProjectX order to LEAN Order format
        /// </summary>
        /// <param name="projectXOrder">ProjectX order object</param>
        /// <returns>LEAN Order object</returns>
        private Order ConvertFromProjectXOrder(object projectXOrder)
        {
            // TODO: Replace with actual MarqSpec.Client.ProjectX types when available
            // Example implementation:
            //
            // var pxOrder = (ProjectXOrder)projectXOrder;
            // 
            // // Get LEAN symbol from ProjectX symbol
            // var symbol = _symbolMapper.GetLeanSymbol(pxOrder.Symbol, SecurityType.Future, Market.USA);
            //
            // // Determine order type and create appropriate LEAN order
            // Order leanOrder;
            // switch (pxOrder.Type)
            // {
            //     case ProjectXOrderType.Market:
            //         leanOrder = new MarketOrder(symbol, pxOrder.Quantity * (pxOrder.Side == OrderSide.Buy ? 1 : -1), DateTime.UtcNow);
            //         break;
            //     case ProjectXOrderType.Limit:
            //         leanOrder = new LimitOrder(symbol, pxOrder.Quantity * (pxOrder.Side == OrderSide.Buy ? 1 : -1), pxOrder.LimitPrice.Value, DateTime.UtcNow);
            //         break;
            //     case ProjectXOrderType.StopMarket:
            //         leanOrder = new StopMarketOrder(symbol, pxOrder.Quantity * (pxOrder.Side == OrderSide.Buy ? 1 : -1), pxOrder.StopPrice.Value, DateTime.UtcNow);
            //         break;
            //     case ProjectXOrderType.StopLimit:
            //         leanOrder = new StopLimitOrder(symbol, pxOrder.Quantity * (pxOrder.Side == OrderSide.Buy ? 1 : -1), pxOrder.StopPrice.Value, pxOrder.LimitPrice.Value, DateTime.UtcNow);
            //         break;
            //     default:
            //         throw new NotSupportedException($"ProjectX order type {pxOrder.Type} not supported");
            // }
            //
            // // Set brokerage ID
            // leanOrder.BrokerId.Add(pxOrder.OrderId);
            //
            // // Store mapping if we have LEAN ID from ClientOrderId
            // if (!string.IsNullOrEmpty(pxOrder.ClientOrderId) && int.TryParse(pxOrder.ClientOrderId, out var leanId))
            // {
            //     AddOrderIdMapping(leanId, pxOrder.OrderId);
            // }
            //
            // return leanOrder;

            Log.Trace("ProjectXBrokerage.ConvertFromProjectXOrder(): Converting ProjectX order to LEAN format");

            // Placeholder return - will be replaced when MarqSpec.Client.ProjectX is integrated
            throw new NotImplementedException("ProjectXBrokerage.ConvertFromProjectXOrder(): MarqSpec.Client.ProjectX integration pending");
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
    }
}
