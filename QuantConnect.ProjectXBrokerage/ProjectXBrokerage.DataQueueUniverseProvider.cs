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

using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QuantConnect.Brokerages.ProjectXBrokerage
{
    public partial class ProjectXBrokerage : IDataQueueUniverseProvider
    {
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
            try
            {
                if (!IsConnected)
                {
                    Log.Error("ProjectXBrokerage.LookupSymbols(): Not connected to ProjectX");
                    return Enumerable.Empty<Symbol>();
                }

                var root = symbol.ID.Symbol;
                // live=true returns only active contracts; live=false includes expired contracts
                var contracts = _apiClient.SearchContractsAsync(root, !includeExpired, CancellationToken.None).GetAwaiter().GetResult();

                var symbols = new List<Symbol>();
                foreach (var contract in contracts)
                {
                    try
                    {
                        var leanSymbol = _symbolMapper.GetLeanSymbol(contract.Id, SecurityType.Future, string.Empty);
                        symbols.Add(leanSymbol);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"ProjectXBrokerage.LookupSymbols(): Failed to map contract {contract.Id}");
                    }
                }

                Log.Debug($"ProjectXBrokerage.LookupSymbols(): Found {symbols.Count} symbol(s) for {root}");
                return symbols;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ProjectXBrokerage.LookupSymbols(): Error looking up symbols for {symbol}");
                return Enumerable.Empty<Symbol>();
            }
        }

        /// <summary>
        /// Returns whether selection can take place or not.
        /// </summary>
        /// <remarks>This is useful to avoid a selection taking place during invalid times, for example IB reset times or when not connected,
        /// because if allowed selection would fail since IB isn't running and would kill the algorithm</remarks>
        /// <returns>True if selection can take place</returns>
        public bool CanPerformSelection()
        {
            return IsConnected;
        }

        #endregion
    }
}
