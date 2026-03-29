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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using PxPriceUpdate = MarqSpec.Client.ProjectX.Api.Models.PriceUpdate;
using PxTradeUpdate = MarqSpec.Client.ProjectX.Api.Models.TradeUpdate;

namespace QuantConnect.Brokerages.ProjectXBrokerage
{
    public partial class ProjectXBrokerage : IDataQueueHandler
    {
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
            Log.Trace($"ProjectXBrokerage.SetJob(): Job UserId={job?.UserId}, AlgorithmId={job?.AlgorithmId}");

            if (!IsConnected)
            {
                Connect();
            }
        }

        #endregion

        private bool CanSubscribe(Symbol symbol)
        {
            if (symbol.Value.IndexOfInvariant("universe", true) != -1 || symbol.IsCanonical())
            {
                return false;
            }

            return symbol.SecurityType == SecurityType.Future;
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        private bool Subscribe(IEnumerable<Symbol> symbols)
        {
            var symbolList = symbols?.ToList() ?? new List<Symbol>();
            Log.Trace($"ProjectXBrokerage.Subscribe(): Subscribing to {symbolList.Count} symbols");
            if (_wsClient == null)
            {
                Log.Error("ProjectXBrokerage.Subscribe(): WebSocket client not initialized");
                return false;
            }

            var success = true;
            foreach (var symbol in symbolList)
            {
                try
                {
                    var contractId = _symbolMapper.GetBrokerageSymbol(symbol);
                    _wsClient.SubscribeToPriceUpdatesAsync(contractId, _connectionCts.Token).GetAwaiter().GetResult();
                    _wsClient.SubscribeToTradeUpdatesAsync(contractId, _connectionCts.Token).GetAwaiter().GetResult();
                    _subscribedContractIds[contractId] = symbol;
                    Log.Trace($"ProjectXBrokerage.Subscribe(): Subscribed to {contractId} ({symbol})");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"ProjectXBrokerage.Subscribe(): Error subscribing to {symbol}");
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        private bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            var symbolList = symbols?.ToList() ?? new List<Symbol>();
            Log.Trace($"ProjectXBrokerage.Unsubscribe(): Unsubscribing from {symbolList.Count} symbols");
            if (_wsClient == null)
            {
                Log.Debug("ProjectXBrokerage.Unsubscribe(): WebSocket client not initialized, skipping");
                return true;
            }

            var success = true;
            foreach (var symbol in symbolList)
            {
                try
                {
                    var contractId = _symbolMapper.GetBrokerageSymbol(symbol);
                    _wsClient.UnsubscribeFromPriceUpdatesAsync(contractId, _connectionCts.Token).GetAwaiter().GetResult();
                    _wsClient.UnsubscribeFromTradeUpdatesAsync(contractId, _connectionCts.Token).GetAwaiter().GetResult();
                    _subscribedContractIds.TryRemove(contractId, out _);
                    Log.Trace($"ProjectXBrokerage.Unsubscribe(): Unsubscribed from {contractId} ({symbol})");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"ProjectXBrokerage.Unsubscribe(): Error unsubscribing from {symbol}");
                    success = false;
                }
            }

            return success;
        }

        #region WebSocket Market Data Event Handlers

        /// <summary>
        /// Handles real-time best bid/offer price updates from the ProjectX WebSocket.
        /// </summary>
        private void OnPriceUpdateReceived(object sender, PxPriceUpdate e)
        {
            try
            {
                if (!_subscribedContractIds.TryGetValue(e.ContractId, out var symbol))
                {
                    Log.Debug($"ProjectXBrokerage.OnPriceUpdateReceived(): Received price update for untracked contract {e.ContractId}");
                    return;
                }

                var tick = new Tick
                {
                    Symbol = symbol,
                    Time = e.Timestamp,
                    TickType = TickType.Quote,
                    BidPrice = e.BidPrice,
                    AskPrice = e.AskPrice,
                    BidSize = (decimal)e.BidSize,
                    AskSize = (decimal)e.AskSize,
                    Value = (e.BidPrice + e.AskPrice) / 2m
                };

                _aggregator.Update(tick);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.OnPriceUpdateReceived(): Error processing price update for contract {e.ContractId}");
            }
        }

        /// <summary>
        /// Handles real-time trade print updates from the ProjectX WebSocket.
        /// </summary>
        private void OnTradeUpdateReceived(object sender, PxTradeUpdate e)
        {
            try
            {
                if (!_subscribedContractIds.TryGetValue(e.ContractId, out var symbol))
                {
                    Log.Debug($"ProjectXBrokerage.OnTradeUpdateReceived(): Received trade update for untracked contract {e.ContractId}");
                    return;
                }

                var tick = new Tick
                {
                    Symbol = symbol,
                    Time = e.Timestamp,
                    TickType = TickType.Trade,
                    Value = e.Price,
                    Quantity = (decimal)e.Quantity
                };

                _aggregator.Update(tick);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.OnTradeUpdateReceived(): Error processing trade update for contract {e.ContractId}");
            }
        }

        #endregion
    }
}
