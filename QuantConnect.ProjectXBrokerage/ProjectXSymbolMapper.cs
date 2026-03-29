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
using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.ProjectXBrokerage
{
    /// <summary>
    /// Provides symbol mapping between LEAN and ProjectX futures ticker formats.
    /// ProjectX uses a compact ticker format: root + CME month code + 2-digit year (e.g., "ESH25").
    /// </summary>
    public class ProjectXSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// Maps CME month code characters to their corresponding month numbers (1–12).
        /// </summary>
        private static readonly Dictionary<char, int> _monthCodes = new Dictionary<char, int>
        {
            { 'F',  1 }, { 'G',  2 }, { 'H',  3 }, { 'J',  4 },
            { 'K',  5 }, { 'M',  6 }, { 'N',  7 }, { 'Q',  8 },
            { 'U',  9 }, { 'V', 10 }, { 'X', 11 }, { 'Z', 12 }
        };

        /// <summary>
        /// Maps month numbers (1–12) to their corresponding CME month code characters.
        /// </summary>
        private static readonly Dictionary<int, char> _monthToCode = new Dictionary<int, char>
        {
            {  1, 'F' }, {  2, 'G' }, {  3, 'H' }, {  4, 'J' },
            {  5, 'K' }, {  6, 'M' }, {  7, 'N' }, {  8, 'Q' },
            {  9, 'U' }, { 10, 'V' }, { 11, 'X' }, { 12, 'Z' }
        };

        /// <summary>
        /// Maps futures ticker roots to their LEAN market identifiers.
        /// </summary>
        private static readonly Dictionary<string, string> _rootToMarket = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // CME – Equity Index Futures
            { "ES",   Market.CME },
            { "MES",  Market.CME },
            { "NQ",   Market.CME },
            { "MNQ",  Market.CME },
            { "RTY",  Market.CME },
            { "M2K",  Market.CME },
            { "YM",   Market.CME },
            { "MYM",  Market.CME },

            // CME – FX Futures
            { "6E",   Market.CME },
            { "6J",   Market.CME },
            { "6B",   Market.CME },
            { "6A",   Market.CME },
            { "6C",   Market.CME },
            { "6S",   Market.CME },
            { "6N",   Market.CME },
            { "6Z",   Market.CME },

            // CME – Interest Rate Futures
            { "SR1",  Market.CME },
            { "SR3",  Market.CME },
            { "GE",   Market.CME },

            // CBOT – Treasury Futures
            { "ZB",   Market.CBOT },
            { "ZN",   Market.CBOT },
            { "ZF",   Market.CBOT },
            { "ZT",   Market.CBOT },
            { "UB",   Market.CBOT },

            // CBOT – Agricultural Futures
            { "ZC",   Market.CBOT },
            { "ZS",   Market.CBOT },
            { "ZW",   Market.CBOT },
            { "ZL",   Market.CBOT },
            { "ZM",   Market.CBOT },
            { "ZO",   Market.CBOT },
            { "ZR",   Market.CBOT },

            // NYMEX – Energy Futures
            { "CL",   Market.NYMEX },
            { "MCL",  Market.NYMEX },
            { "NG",   Market.NYMEX },
            { "RB",   Market.NYMEX },
            { "HO",   Market.NYMEX },

            // COMEX (traded via NYMEX in LEAN) – Metal Futures
            { "GC",   Market.NYMEX },
            { "MGC",  Market.NYMEX },
            { "SI",   Market.NYMEX },
            { "SIL",  Market.NYMEX },
            { "HG",   Market.NYMEX },
            { "PA",   Market.NYMEX },
            { "PL",   Market.NYMEX },

            // ICE – Energy and Soft Commodity Futures
            { "BRN",  Market.ICE },
            { "G",    Market.ICE },
            { "SB",   Market.ICE },
            { "KC",   Market.ICE },
            { "CT",   Market.ICE },
            { "CC",   Market.ICE },
            { "RC",   Market.ICE },
        };

        /// <summary>
        /// Converts a LEAN <see cref="Symbol"/> to the ProjectX ticker string.
        /// </summary>
        /// <param name="symbol">A LEAN futures symbol</param>
        /// <returns>ProjectX ticker string, e.g. "ESH25"</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="symbol"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="symbol"/> is not a futures contract.</exception>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            if (symbol.SecurityType != SecurityType.Future)
                throw new ArgumentException(
                    $"SecurityType.{symbol.SecurityType} is not supported. Only Futures are supported.", nameof(symbol));

            var root = symbol.ID.Symbol;
            var expiry = symbol.ID.Date;
            var monthCode = _monthToCode[expiry.Month];
            var year2digit = expiry.Year % 100;

            return $"{root}{monthCode}{year2digit:D2}";
        }

        /// <summary>
        /// Converts a ProjectX futures ticker string to a LEAN <see cref="Symbol"/>.
        /// The <paramref name="market"/> parameter is used as a fallback if the ticker root is not
        /// in the known routing table; pass <see cref="string.Empty"/> to use the default fallback.
        /// </summary>
        /// <param name="brokerageSymbol">ProjectX ticker, e.g. "ESH25"</param>
        /// <param name="securityType">Must be <see cref="SecurityType.Future"/></param>
        /// <param name="market">Caller-supplied market hint (used as fallback for unknown roots)</param>
        /// <param name="expirationDate">Not used; expiry is derived from the ticker string</param>
        /// <param name="strike">Not applicable for futures; ignored</param>
        /// <param name="optionRight">Not applicable for futures; ignored</param>
        /// <returns>A LEAN <see cref="Symbol"/> representing the futures contract</returns>
        /// <exception cref="ArgumentException">Thrown for null/empty ticker or unsupported security type or invalid format.</exception>
        public Symbol GetLeanSymbol(
            string brokerageSymbol,
            SecurityType securityType,
            string market,
            DateTime expirationDate = default,
            decimal strike = 0,
            OptionRight optionRight = 0)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException("Brokerage symbol cannot be null or empty.", nameof(brokerageSymbol));

            if (securityType != SecurityType.Future)
                throw new ArgumentException(
                    $"SecurityType.{securityType} is not supported. Only Futures are supported.", nameof(securityType));

            return ParseFuturesTicker(brokerageSymbol, market);
        }

        /// <summary>
        /// Parses a ProjectX futures ticker string (e.g. "ESH25") and returns a LEAN Symbol.
        /// Format: &lt;root&gt;&lt;monthCode&gt;&lt;YY&gt; where monthCode is a CME month letter and YY is a 2-digit year.
        /// </summary>
        private Symbol ParseFuturesTicker(string ticker, string callerMarket = null)
        {
            if (ticker.Length < 4)
            {
                throw new ArgumentException(
                    $"Invalid futures ticker '{ticker}': minimum length is 4 characters (root + month code + 2-digit year).",
                    nameof(ticker));
            }

            var year2digitStr = ticker.Substring(ticker.Length - 2);
            var monthChar     = ticker[ticker.Length - 3];
            var root          = ticker.Substring(0, ticker.Length - 3);

            if (!int.TryParse(year2digitStr, out var year2digit))
            {
                throw new ArgumentException(
                    $"Invalid futures ticker '{ticker}': last two characters '{year2digitStr}' are not a valid year.", nameof(ticker));
            }

            if (!_monthCodes.TryGetValue(monthChar, out var month))
            {
                throw new ArgumentException(
                    $"Invalid futures ticker '{ticker}': '{monthChar}' is not a recognized CME month code.", nameof(ticker));
            }

            var year   = 2000 + year2digit;
            var expiry = GetThirdFriday(year, month);
            var market = GetMarket(root, callerMarket);

            return Symbol.CreateFuture(root, market, expiry);
        }

        /// <summary>
        /// Resolves the LEAN market for a given ticker root. Falls back to the caller-supplied market
        /// hint, and then to <see cref="Market.CME"/> if neither is available.
        /// </summary>
        private string GetMarket(string root, string callerMarket)
        {
            if (_rootToMarket.TryGetValue(root, out var market))
                return market;

            if (!string.IsNullOrWhiteSpace(callerMarket))
            {
                Log.Trace($"ProjectXSymbolMapper.GetMarket(): Unknown ticker root '{root}', using caller-supplied market '{callerMarket}'.");
                return callerMarket;
            }

            Log.Trace($"ProjectXSymbolMapper.GetMarket(): Unknown ticker root '{root}', defaulting to {Market.CME}.");
            return Market.CME;
        }

        /// <summary>
        /// Returns the date of the third Friday of the given month and year.
        /// This is the standard expiry convention for most CME equity index futures.
        /// </summary>
        public static DateTime GetThirdFriday(int year, int month)
        {
            var firstDay = new DateTime(year, month, 1);
            var daysUntilFirstFriday = ((int)DayOfWeek.Friday - (int)firstDay.DayOfWeek + 7) % 7;
            return firstDay.AddDays(daysUntilFirstFriday + 14);
        }
    }
}
