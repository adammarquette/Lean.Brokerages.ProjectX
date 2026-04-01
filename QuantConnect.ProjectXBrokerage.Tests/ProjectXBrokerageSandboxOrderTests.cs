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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    /// <summary>
    /// Integration tests that submit real orders to the ProjectX sandbox.
    /// Requires valid sandbox credentials set via the QC_BROKERAGE_PROJECT_X_API_KEY
    /// environment variable (and companions). Run with:
    ///   dotnet test --filter "Category=Integration"
    /// </summary>
    [TestFixture, Category("Integration"), Explicit("Requires ProjectX sandbox credentials")]
    public class ProjectXBrokerageSandboxOrderTests
    {
        private ProjectXBrokerage _brokerage;
        private Symbol _symbol;

        // Far-OTM limit prices so orders rest without filling in sandbox
        private const decimal HighLimit = 99_000m;
        private const decimal LowLimit  = 100m;

        // Timeout for order status events
        private static readonly TimeSpan OrderEventTimeout = TimeSpan.FromSeconds(15);

        [SetUp]
        public void SetUp()
        {
            var apiKey = QuantConnect.Configuration.Config.Get("brokerage-project-x-api-key", string.Empty);
            Assume.That(!string.IsNullOrEmpty(apiKey),
                "Skipping: brokerage-project-x-api-key not configured. " +
                "Set the QC_BROKERAGE_PROJECT_X_API_KEY environment variable to run sandbox order tests.");

            TestSetup.ReloadConfiguration();
            _brokerage = new ProjectXBrokerage(new TestDataAggregator());
            _brokerage.Connect();
            Assert.IsTrue(_brokerage.IsConnected, "Precondition: brokerage must be connected");

            _symbol = ProjectXBrokerageTestsHelper.GetFrontMonthES();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                // Cancel any pending orders left over from the test
                foreach (var order in _brokerage.GetOpenOrders())
                    _brokerage.CancelOrder(order);
            }
            catch { /* best-effort cleanup */ }

            _brokerage?.Disconnect();
            _brokerage?.Dispose();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Waits for an <see cref="OrderStatus"/> matching <paramref name="expected"/> to be
        /// raised on <see cref="IBrokerage.OrdersStatusChanged"/>.
        /// </summary>
        private bool WaitForOrderStatus(OrderStatus expected, out OrderEvent receivedEvent)
        {
            var tcs = new ManualResetEventSlim(false);
            OrderEvent captured = null;

            void Handler(object sender, List<OrderEvent> events)
            {
                foreach (var e in events)
                {
                    if (e.Status == expected)
                    {
                        captured = e;
                        tcs.Set();
                        break;
                    }
                }
            }

            _brokerage.OrdersStatusChanged += Handler;
            try
            {
                return tcs.Wait(OrderEventTimeout);
            }
            finally
            {
                _brokerage.OrdersStatusChanged -= Handler;
                receivedEvent = captured;
            }
        }

        // ── Tests ────────────────────────────────────────────────────────────────

        [Test]
        public void PlacesAndCancelsLimitOrder()
        {
            // Arrange — resting far-OTM limit buy (will not fill)
            var order = new LimitOrder(_symbol, quantity: 1, limitPrice: LowLimit, time: DateTime.UtcNow);

            // Act — place
            var placed = _brokerage.PlaceOrder(order);
            Assert.IsTrue(placed, "PlaceOrder should return true for a valid limit order");

            // Wait for the order to be acknowledged by the exchange
            var acknowledged = WaitForOrderStatus(OrderStatus.Submitted, out var submitEvent)
                            || WaitForOrderStatus(OrderStatus.New, out submitEvent);
            Log.Trace($"SandboxOrderTests: limit order acknowledged={acknowledged}");

            // Act — cancel
            var cancelled = _brokerage.CancelOrder(order);
            Assert.IsTrue(cancelled, "CancelOrder should return true");

            var gotCancelled = WaitForOrderStatus(OrderStatus.Canceled, out var cancelEvent);
            Assert.IsTrue(gotCancelled, $"Expected OrderStatus.Canceled within {OrderEventTimeout.TotalSeconds}s");
        }

        [Test]
        public void PlacesAndCancelsStopMarketOrder()
        {
            // Arrange — stop that would never trigger at far HighLimit
            var order = new StopMarketOrder(_symbol, quantity: 1, stopPrice: HighLimit, time: DateTime.UtcNow);

            // Act
            var placed = _brokerage.PlaceOrder(order);
            Assert.IsTrue(placed, "PlaceOrder should return true for a valid stop market order");

            WaitForOrderStatus(OrderStatus.Submitted, out _);

            var cancelled = _brokerage.CancelOrder(order);
            Assert.IsTrue(cancelled, "CancelOrder should return true for a resting stop order");

            var gotCancelled = WaitForOrderStatus(OrderStatus.Canceled, out _);
            Assert.IsTrue(gotCancelled, $"Expected OrderStatus.Canceled within {OrderEventTimeout.TotalSeconds}s");
        }

        [Test]
        public void PlacesAndCancelsTrailingStopOrder()
        {
            // Arrange — trailing stop well below market
            var order = new TrailingStopOrder(
                _symbol, quantity: 1, trailingAmount: 50m, trailingAsPercentage: false,
                stopPrice: LowLimit, time: DateTime.UtcNow);

            // Act
            var placed = _brokerage.PlaceOrder(order);
            Assert.IsTrue(placed, "PlaceOrder should return true for a trailing stop order");

            WaitForOrderStatus(OrderStatus.Submitted, out _);

            var cancelled = _brokerage.CancelOrder(order);
            Assert.IsTrue(cancelled, "CancelOrder should return true for a resting trailing stop");

            var gotCancelled = WaitForOrderStatus(OrderStatus.Canceled, out _);
            Assert.IsTrue(gotCancelled, $"Expected OrderStatus.Canceled within {OrderEventTimeout.TotalSeconds}s");
        }

        [Test]
        public void RejectsInvalidOrder_ZeroQuantity()
        {
            // Arrange — quantity of 0 is invalid
            var order = new MarketOrder(_symbol, quantity: 0, time: DateTime.UtcNow);

            // Act
            var placed = _brokerage.PlaceOrder(order);

            // Assert — brokerage rejects the order before sending to exchange
            Assert.IsFalse(placed, "PlaceOrder should return false for a zero-quantity order");
        }
    }
}
