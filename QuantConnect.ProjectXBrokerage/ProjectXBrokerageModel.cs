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
    /// Represents the brokerage model for ProjectX, defining trading rules, supported orders, and fee structures.
    /// </summary>
    public class ProjectXBrokerageModel : DefaultBrokerageModel
    {
        private static readonly HashSet<OrderType> _supportedOrderTypes = new HashSet<OrderType>
        {
            OrderType.Market,
            OrderType.Limit,
            OrderType.StopMarket,
            OrderType.StopLimit
        };

        /// <summary>
        /// Returns true if the order can be submitted; false for unsupported order types.
        /// </summary>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            if (!_supportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    $"ProjectX does not support {order.Type} orders. Supported types: Market, Limit, StopMarket, StopLimit.");
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Gets the fee model for the given security.
        /// For now, this returns a zero-fee model. A detailed implementation is planned for Phase 8.
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The fee model for this security</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            // A full implementation is planned for Phase 8.
            return new ConstantFeeModel(0);
        }
    }
}
