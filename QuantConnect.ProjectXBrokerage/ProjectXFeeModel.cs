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
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.ProjectXBrokerage
{
    /// <summary>
    /// Provides per-side commission calculations for ProjectX futures.
    /// Uses a round-turn (RT) fee table keyed by futures root ticker.
    /// Each order is charged half the RT fee (one side of the round trip),
    /// scaled by the absolute order quantity.
    /// Defaults to $2.80 RT ($1.40/side) for unrecognised roots.
    /// </summary>
    public class ProjectXFeeModel : IFeeModel
    {
        // Round-turn fees in USD, keyed by root futures ticker (upper-case).
        // Source: ProjectX published fee schedule.
        private static readonly IReadOnlyDictionary<string, decimal> RoundTurnFees =
            new Dictionary<string, decimal>
            {
                // CME Equity Index
                { "ES",   2.80m },
                { "MES",  0.74m },
                { "NQ",   2.80m },
                { "MNQ",  0.74m },
                { "RTY",  2.80m },
                { "M2K",  0.74m },
                { "YM",   2.80m },
                { "MYM",  0.74m },
                // CME FX
                { "6E",   3.24m },
                { "6J",   3.24m },
                { "6B",   3.24m },
                { "6A",   3.24m },
                { "6C",   3.24m },
                { "6S",   3.24m },
                { "6M",   3.24m },
                { "6N",   3.24m },
                // CME/CBOT Interest Rate
                { "ZN",   1.78m },
                { "ZB",   1.78m },
                { "ZT",   1.78m },
                { "ZF",   1.78m },
                { "UB",   1.78m },
                // COMEX Metals
                { "GC",   3.24m },
                { "MGC",  0.74m },
                { "SI",   3.24m },
                { "SIL",  0.74m },
                { "HG",   3.24m },
                { "PL",   3.24m },
                { "PA",   3.24m },
                // NYMEX Energy
                { "CL",   3.04m },
                { "MCL",  0.74m },
                { "NG",   3.04m },
                { "RB",   3.04m },
                { "HO",   3.04m },
                { "QM",   3.04m },
                // CBOT Grains
                { "ZC",   4.30m },
                { "ZS",   4.30m },
                { "ZW",   4.30m },
                { "ZL",   4.30m },
                { "ZM",   4.30m },
                // CME Livestock
                { "LE",   3.04m },
                { "HE",   3.04m },
                { "GF",   3.04m },
                // Crypto (CME)
                { "BTC",  6.00m },
                { "MBT",  2.50m },
                { "ETH",  4.00m },
                { "MET",  2.50m },
                // EUREX
                { "FGBL", 3.24m },
                { "FDAX", 3.24m },
                { "FESX", 3.24m },
                { "FGBM", 3.24m },
                { "FGBS", 3.24m },
            };

        private const decimal DefaultRoundTurnFee = 2.80m;

        /// <inheritdoc />
        public OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var root = parameters.Security.Symbol.ID.Symbol;
            if (!RoundTurnFees.TryGetValue(root.ToUpperInvariant(), out var rtFee))
                rtFee = DefaultRoundTurnFee;

            // Charge one side (half the RT) per contract, scaled by absolute quantity.
            var perSide = rtFee / 2m * Math.Abs(parameters.Order.Quantity);

            return new OrderFee(new CashAmount(perSide, Currencies.USD));
        }
    }
}
