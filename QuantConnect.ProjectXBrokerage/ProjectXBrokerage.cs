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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Configuration;

namespace QuantConnect.Brokerages.ProjectXBrokerage
{
    [BrokerageFactory(typeof(ProjectXBrokerageFactory))]
    public class ProjectXBrokerage : Brokerage, IDataQueueHandler, IDataQueueUniverseProvider
    {
        private readonly IDataAggregator _aggregator;
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;
        private volatile bool _isConnected;
        private readonly object _connectionLock = new object();
        private CancellationTokenSource _connectionCancellationTokenSource;
        private Task _heartbeatTask;

        // Configuration fields
        private string _apiKey;
        private string _apiSecret;
        private string _environment;
        private int _reconnectAttempts;
        private int _reconnectDelay;
        private int _heartbeatInterval;

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected => _isConnected;

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

            // Initialize connection state
            _isConnected = false;

            // Load configuration
            LoadConfiguration();

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
            if (job == null)
            {
                Log.Trace("ProjectXBrokerage.SetJob(): Received null job packet");
                return;
            }

            Log.Trace($"ProjectXBrokerage.SetJob(): Job UserId: {job.UserId}, AlgorithmId: {job.AlgorithmId}");

            // Override configuration with job-specific settings if provided
            if (job.BrokerageData.ContainsKey("project-x-api-key"))
            {
                _apiKey = job.BrokerageData["project-x-api-key"];
            }
            if (job.BrokerageData.ContainsKey("project-x-api-secret"))
            {
                _apiSecret = job.BrokerageData["project-x-api-secret"];
            }
            if (job.BrokerageData.ContainsKey("project-x-environment"))
            {
                _environment = job.BrokerageData["project-x-environment"];
            }

            ValidateConfiguration();
            Log.Trace($"ProjectXBrokerage.SetJob(): Configuration updated from job packet");
        }

        #endregion

        #region Brokerage

        /// <summary>
        /// Gets all open orders on the account.
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public override List<Order> GetOpenOrders()
        {
            Log.Trace("ProjectXBrokerage.GetOpenOrders(): Retrieving open orders");
            throw new NotImplementedException("ProjectXBrokerage.GetOpenOrders(): Implementation pending Phase 2");
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
            Log.Trace($"ProjectXBrokerage.PlaceOrder(): Symbol: {order.Symbol}, Quantity: {order.Quantity}, Type: {order.Type}");
            throw new NotImplementedException("ProjectXBrokerage.PlaceOrder(): Implementation pending Phase 2");
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            Log.Trace($"ProjectXBrokerage.UpdateOrder(): OrderId: {order.Id}, Symbol: {order.Symbol}, Quantity: {order.Quantity}");
            throw new NotImplementedException("ProjectXBrokerage.UpdateOrder(): Implementation pending Phase 2");
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Log.Trace($"ProjectXBrokerage.CancelOrder(): OrderId: {order.Id}, Symbol: {order.Symbol}");
            throw new NotImplementedException("ProjectXBrokerage.CancelOrder(): Implementation pending Phase 2");
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            lock (_connectionLock)
            {
                if (_isConnected)
                {
                    Log.Trace("ProjectXBrokerage.Connect(): Already connected");
                    return;
                }

                Log.Trace("ProjectXBrokerage.Connect(): Connecting to ProjectX API");

                ValidateConfiguration();

                int attempt = 0;
                int delay = _reconnectDelay;
                Exception lastException = null;

                while (attempt < _reconnectAttempts)
                {
                    attempt++;

                    try
                    {
                        Log.Trace($"ProjectXBrokerage.Connect(): Connection attempt {attempt}/{_reconnectAttempts}");

                        // Initialize ProjectX client
                        // TODO: Initialize MarqSpec.Client.ProjectX client instance
                        // var client = new ProjectXClient(_apiKey, _apiSecret, _environment);
                        // await client.ConnectAsync();

                        // Initialize WebSocket connections
                        // TODO: Setup WebSocket connections

                        // Initialize cancellation token for heartbeat
                        _connectionCancellationTokenSource = new CancellationTokenSource();

                        // Start heartbeat monitoring
                        StartHeartbeat();

                        _isConnected = true;

                        Log.Trace($"ProjectXBrokerage.Connect(): Successfully connected to ProjectX API (Environment: {SanitizeEnvironment(_environment)})");
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, "Connected", 
                            "Successfully connected to ProjectX API"));

                        return;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        Log.Error(ex, $"ProjectXBrokerage.Connect(): Connection attempt {attempt} failed: {SanitizeErrorMessage(ex.Message)}");

                        if (attempt < _reconnectAttempts)
                        {
                            Log.Trace($"ProjectXBrokerage.Connect(): Retrying in {delay}ms...");
                            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "Reconnecting", 
                                $"Connection attempt {attempt} failed, retrying in {delay}ms"));

                            Thread.Sleep(delay);

                            // Exponential backoff with max delay of 60 seconds
                            delay = Math.Min(delay * 2, 60000);
                        }
                    }
                }

                // All retry attempts failed
                var errorMessage = $"Failed to connect after {_reconnectAttempts} attempts";
                Log.Error($"ProjectXBrokerage.Connect(): {errorMessage}");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "ConnectionFailed", errorMessage));

                throw new Exception($"ProjectXBrokerage.Connect(): {errorMessage}", lastException);
            }
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            lock (_connectionLock)
            {
                if (!_isConnected)
                {
                    Log.Trace("ProjectXBrokerage.Disconnect(): Already disconnected");
                    return;
                }

                Log.Trace("ProjectXBrokerage.Disconnect(): Disconnecting from ProjectX API");

                try
                {
                    // Stop heartbeat monitoring
                    StopHeartbeat();

                    // Close WebSocket connections gracefully
                    // TODO: Close WebSocket connections
                    // await _webSocketClient?.DisconnectAsync();

                    // Dispose ProjectX client resources
                    // TODO: Dispose client
                    // _client?.Dispose();

                    _isConnected = false;

                    Log.Trace("ProjectXBrokerage.Disconnect(): Successfully disconnected from ProjectX API");
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, "Disconnected", 
                        "Successfully disconnected from ProjectX API"));
                }
                catch (Exception ex)
                {
                    // Log the error but don't throw - disconnect should be idempotent and safe
                    Log.Error(ex, $"ProjectXBrokerage.Disconnect(): Error during disconnect: {SanitizeErrorMessage(ex.Message)}");

                    // Force disconnected state even if cleanup failed
                    _isConnected = false;
                }
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

        /// <summary>
        /// Loads configuration from Config
        /// </summary>
        private void LoadConfiguration()
        {
            // Load API credentials
            _apiKey = Config.Get("project-x-api-key", Environment.GetEnvironmentVariable("PROJECT_X_API_KEY"));
            _apiSecret = Config.Get("project-x-api-secret", Environment.GetEnvironmentVariable("PROJECT_X_API_SECRET"));
            _environment = Config.Get("project-x-environment", "production");

            // Load reconnection settings with defaults
            _reconnectAttempts = Config.GetInt("project-x-reconnect-attempts", 5);
            _reconnectDelay = Config.GetInt("project-x-reconnect-delay", 1000);
            _heartbeatInterval = Config.GetInt("project-x-heartbeat-interval", 30000);

            Log.Trace($"ProjectXBrokerage.LoadConfiguration(): Configuration loaded - Environment: {SanitizeEnvironment(_environment)}, " +
                     $"ReconnectAttempts: {_reconnectAttempts}, ReconnectDelay: {_reconnectDelay}ms, HeartbeatInterval: {_heartbeatInterval}ms");
        }

        /// <summary>
        /// Validates that required configuration is present
        /// </summary>
        private void ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                errors.Add("API key is required. Set 'project-x-api-key' in config or PROJECT_X_API_KEY environment variable.");
            }

            if (string.IsNullOrWhiteSpace(_apiSecret))
            {
                errors.Add("API secret is required. Set 'project-x-api-secret' in config or PROJECT_X_API_SECRET environment variable.");
            }

            if (string.IsNullOrWhiteSpace(_environment))
            {
                errors.Add("Environment is required. Set 'project-x-environment' in config (production/sandbox).");
            }
            else if (_environment != "production" && _environment != "sandbox")
            {
                errors.Add($"Invalid environment '{_environment}'. Must be 'production' or 'sandbox'.");
            }

            if (_reconnectAttempts < 1)
            {
                errors.Add($"Invalid reconnect attempts '{_reconnectAttempts}'. Must be >= 1.");
            }

            if (_reconnectDelay < 100)
            {
                errors.Add($"Invalid reconnect delay '{_reconnectDelay}'. Must be >= 100ms.");
            }

            if (errors.Any())
            {
                var errorMessage = $"ProjectXBrokerage configuration validation failed:\n{string.Join("\n", errors)}";
                Log.Error(errorMessage);
                throw new ArgumentException(errorMessage);
            }
        }

        /// <summary>
        /// Starts the heartbeat monitoring task
        /// </summary>
        private void StartHeartbeat()
        {
            if (_heartbeatTask != null)
            {
                Log.Trace("ProjectXBrokerage.StartHeartbeat(): Heartbeat already running");
                return;
            }

            Log.Trace($"ProjectXBrokerage.StartHeartbeat(): Starting heartbeat with interval {_heartbeatInterval}ms");

            _heartbeatTask = Task.Run(async () =>
            {
                while (!_connectionCancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(_heartbeatInterval, _connectionCancellationTokenSource.Token);

                        if (_connectionCancellationTokenSource.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        // Perform heartbeat check
                        // TODO: Implement actual ping/pong with ProjectX API
                        // bool isHealthy = await CheckConnectionHealth();

                        // For now, just log the heartbeat
                        Log.Debug("ProjectXBrokerage.Heartbeat(): Connection health check");

                        // If connection is unhealthy, trigger reconnection
                        // if (!isHealthy)
                        // {
                        //     Log.Warning("ProjectXBrokerage.Heartbeat(): Connection unhealthy, triggering reconnection");
                        //     OnConnectionLost();
                        // }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when stopping heartbeat
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"ProjectXBrokerage.Heartbeat(): Error during heartbeat: {SanitizeErrorMessage(ex.Message)}");
                    }
                }

                Log.Debug("ProjectXBrokerage.StartHeartbeat(): Heartbeat monitoring stopped");
            }, _connectionCancellationTokenSource.Token);
        }

        /// <summary>
        /// Stops the heartbeat monitoring task
        /// </summary>
        private void StopHeartbeat()
        {
            if (_heartbeatTask == null)
            {
                return;
            }

            Log.Trace("ProjectXBrokerage.StopHeartbeat(): Stopping heartbeat monitoring");

            try
            {
                _connectionCancellationTokenSource?.Cancel();

                // Wait for heartbeat task to complete (with timeout)
                if (!_heartbeatTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    Log.Error("ProjectXBrokerage.StopHeartbeat(): Heartbeat task did not stop within timeout");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.StopHeartbeat(): Error stopping heartbeat");
            }
            finally
            {
                _connectionCancellationTokenSource?.Dispose();
                _connectionCancellationTokenSource = null;
                _heartbeatTask = null;
            }
        }

        /// <summary>
        /// Handles connection loss and triggers reconnection
        /// </summary>
        private void OnConnectionLost()
        {
            lock (_connectionLock)
            {
                if (!_isConnected)
                {
                    return;
                }

                Log.Error("ProjectXBrokerage.OnConnectionLost(): Connection lost, attempting to reconnect");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ConnectionLost",
                    "Connection to ProjectX API lost, reconnecting..."));

                _isConnected = false;
                StopHeartbeat();

                try
                {
                    // Attempt to reconnect
                    Connect();

                    // Resubscribe to data feeds
                    if (_isConnected)
                    {
                        Log.Trace("ProjectXBrokerage.OnConnectionLost(): Reconnected, resubscribing to data feeds");
                        // TODO: Resubscribe logic will be implemented in Phase 5
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"ProjectXBrokerage.OnConnectionLost(): Reconnection failed: {SanitizeErrorMessage(ex.Message)}");
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "ReconnectionFailed", 
                        "Failed to reconnect to ProjectX API"));
                }
            }
        }

        /// <summary>
        /// Sanitizes error messages to remove sensitive information
        /// </summary>
        /// <param name="message">The error message</param>
        /// <returns>Sanitized message</returns>
        private string SanitizeErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            var sanitized = message;

            // Remove API key if present
            if (!string.IsNullOrEmpty(_apiKey))
            {
                sanitized = sanitized.Replace(_apiKey, "***API_KEY***");
            }

            // Remove API secret if present
            if (!string.IsNullOrEmpty(_apiSecret))
            {
                sanitized = sanitized.Replace(_apiSecret, "***API_SECRET***");
            }

            return sanitized;
        }

        /// <summary>
        /// Sanitizes environment string for logging
        /// </summary>
        /// <param name="environment">The environment name</param>
        /// <returns>Sanitized environment string</returns>
        private string SanitizeEnvironment(string environment)
        {
            return string.IsNullOrWhiteSpace(environment) ? "not-set" : environment;
        }

        /// <summary>
        /// Disposes of brokerage resources
        /// </summary>
        public new void Dispose()
        {
            Log.Trace("ProjectXBrokerage.Dispose(): Disposing brokerage resources");

            try
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ProjectXBrokerage.Dispose(): Error during disconnect");
            }

            _connectionCancellationTokenSource?.Dispose();

            base.Dispose();
        }
    }
}
