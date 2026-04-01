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
    /// Fee model for ProjectX (TopstepX). Applies NFA and clearing fees per the TopstepX fee schedule.
    /// TopstepX charges no commissions — only pass-through NFA and clearing fees.
    /// Fees are charged on a round-turn (RT) basis; each fill is charged half the RT fee.
    /// Per-side fee = quantity × (RT fee ÷ 2).
    /// Source: https://help.topstep.com/en/articles/14363528-topstepx-commissions-fees
    /// </summary>
    public class ProjectXFeeModel : FeeModel
    {
        /// <summary>
        /// Default round-turn fee for symbols not in the fee schedule ($2.80 = standard E-mini rate).
        /// </summary>
        private const decimal DefaultRoundTurnFee = 2.80m;

        /// <summary>
        /// Round-turn fee schedule keyed by symbol root (e.g. "ES", "NQ").
        /// Values represent the total round-turn fee per contract in USD.
        /// </summary>
        private static readonly Dictionary<string, decimal> RoundTurnFees = new()
        {
            // CME Equity Futures
            { "ES",  2.80m },
            { "MES", 0.74m },
            { "NQ",  2.80m },
            { "MNQ", 0.74m },
            { "RTY", 2.80m },
            { "M2K", 0.74m },
            { "NKD", 4.34m },
            { "MBT", 2.34m },
            { "MET", 0.24m },

            // CME CBOT Equity Futures
            { "YM",  2.80m },
            { "MYM", 0.74m },

            // CME NYMEX Futures
            { "CL",  3.04m },
            { "MCL", 1.04m },
            { "QM",  2.44m },
            { "PL",  3.24m },
            { "QG",  1.04m },
            { "RB",  3.04m },
            { "HO",  3.04m },
            { "NG",  3.20m },
            { "MNG", 1.24m },

            // CME COMEX Futures
            { "GC",  3.24m },
            { "MGC", 1.24m },
            { "SI",  3.24m },
            { "SIL", 2.04m },
            { "HG",  3.24m },
            { "MHG", 1.24m },

            // CME Foreign Exchange Futures
            { "6E",  3.24m },
            { "M6E", 0.52m },
            { "6J",  3.24m },
            { "6B",  3.24m },
            { "M6B", 0.52m },
            { "6A",  3.24m },
            { "M6A", 0.52m },
            { "6C",  3.24m },
            { "6S",  3.24m },
            { "6N",  3.24m },
            { "6M",  3.24m },
            { "E7",  1.74m },

            // CME CBOT Financial / Interest Rate Futures
            { "ZT",  1.34m },
            { "ZF",  1.34m },
            { "ZN",  1.60m },
            { "ZB",  1.78m },
            { "UB",  1.94m },
            { "TN",  1.64m },

            // CME Agricultural Futures
            { "HE",  4.24m },
            { "LE",  4.24m },

            // CME CBOT Commodity Futures
            { "ZC",  4.30m },
            { "ZW",  4.30m },
            { "ZS",  4.30m },
            { "ZM",  4.30m },
            { "ZL",  4.30m },
        };

        /// <summary>
        /// Gets the order fee for the given order parameters.
        /// Returns quantity × (RT fee ÷ 2) — the per-side portion of the round-turn fee.
        /// </summary>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var root = parameters.Security.Symbol.ID.Symbol;
            if (!RoundTurnFees.TryGetValue(root, out var roundTurnFee))
            {
                roundTurnFee = DefaultRoundTurnFee;
            }

            var perSideFee = parameters.Order.AbsoluteQuantity * (roundTurnFee / 2m);
            return new OrderFee(new CashAmount(perSideFee, Currencies.USD));
        }
    }
}
