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

using System.Collections.Generic;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.ProjectXBrokerage
{
    /// <summary>
    /// Provides per-side commission calculations for ProjectX futures.
    /// Uses a round-turn (RT) fee table keyed by futures root ticker.
    /// Each order is charged half the RT fee (one side of the round trip).
    /// Defaults to $5.00 RT ($2.50/side) for unrecognised roots.
    /// </summary>
    public class ProjectXFeeModel : IFeeModel
    {
        // Round-turn fees in USD, keyed by root futures ticker (upper-case).
        // Source: ProjectX published fee schedule.
        private static readonly IReadOnlyDictionary<string, decimal> RoundTurnFees =
            new Dictionary<string, decimal>
            {
                // CME Equity Index
                { "ES",   4.06m },
                { "MES",  0.62m },
                { "NQ",   4.06m },
                { "MNQ",  0.62m },
                { "RTY",  4.06m },
                { "M2K",  0.62m },
                { "YM",   4.06m },
                { "MYM",  0.62m },
                // CME FX
                { "6E",   2.72m },
                { "6J",   2.72m },
                { "6B",   2.72m },
                { "6A",   2.72m },
                { "6C",   2.72m },
                { "6S",   2.72m },
                { "6M",   2.72m },
                { "6N",   2.72m },
                // CME Interest Rate
                { "ZN",   2.04m },
                { "ZB",   2.04m },
                { "ZT",   2.04m },
                { "ZF",   2.04m },
                { "UB",   2.04m },
                // CME Commodities
                { "GC",   2.72m },
                { "MGC",  0.62m },
                { "SI",   2.72m },
                { "HG",   2.72m },
                { "PL",   2.72m },
                { "CL",   2.72m },
                { "MCL",  0.62m },
                { "NG",   2.72m },
                { "RB",   2.72m },
                { "HO",   2.72m },
                // CBOT Grains
                { "ZC",   2.72m },
                { "ZS",   2.72m },
                { "ZW",   2.72m },
                { "ZL",   2.72m },
                { "ZM",   2.72m },
                // NYMEX/COMEX
                { "PA",   2.72m },
                { "QM",   2.72m },
                // CME Livestock
                { "LE",   2.72m },
                { "HE",   2.72m },
                { "GF",   2.72m },
                // Micro Metals
                { "MGC",  0.62m },
                { "SIL",  0.62m },
                // Crypto (CME)
                { "BTC",  6.00m },
                { "MBT",  2.50m },
                { "ETH",  4.00m },
                { "MET",  2.50m },
                // EUREX
                { "FGBL", 2.50m },
                { "FDAX", 2.50m },
                { "FESX", 2.50m },
                { "FGBM", 2.50m },
                { "FGBS", 2.50m },
                // Default placeholder — overridden by the fallback
            };

        private const decimal DefaultRoundTurnFee = 5.00m;

        /// <inheritdoc />
        public OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var root = parameters.Security.Symbol.ID.Symbol;
            if (!RoundTurnFees.TryGetValue(root.ToUpperInvariant(), out var rtFee))
                rtFee = DefaultRoundTurnFee;

            // Charge one side (half the round-turn) per fill.
            var perSide = rtFee / 2m;

            return new OrderFee(new CashAmount(perSide, Currencies.USD));
        }
    }
}
