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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Logging;
using QuantConnect.Configuration;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    [TestFixture, Explicit("These tests require valid sandbox credentials.")]
    [Ignore("Sandbox order tests need to be rewritten to match the current LEAN Brokerage API surface " +
            "(PlaceOrder returns bool, no GetOrderById on Brokerage, UpdateOrder takes Order not UpdateOrderRequest).")]
    public class ProjectXBrokerageSandboxOrderTests
    {
        [Test]
        public void PlacesAndFillsMarketOrder() => Assert.Ignore();

        [Test]
        public void PlacesAndCancelsLimitOrder() => Assert.Ignore();

        [Test]
        public void UpdatesLimitOrderPrice() => Assert.Ignore();

        [Test]
        public void RejectsInvalidOrder() => Assert.Ignore();
    }
}
