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

namespace QuantConnect.Brokerages.ProjectXBrokerage
{
    [BrokerageFactory(typeof(ProjectXBrokerageFactory))]
    public class ProjectXBrokerage : Brokerage, IDataQueueHandler, IDataQueueUniverseProvider
    {
        private readonly IDataAggregator _aggregator;
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected { get; }

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
            Log.Trace("ProjectXBrokerage.Connect(): Connecting to ProjectX API");
            throw new NotImplementedException("ProjectXBrokerage.Connect(): Implementation pending Phase 2");
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            Log.Trace("ProjectXBrokerage.Disconnect(): Disconnecting from ProjectX API");
            throw new NotImplementedException("ProjectXBrokerage.Disconnect(): Implementation pending Phase 2");
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
    }
}
