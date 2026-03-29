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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    /// <summary>
    /// Tests for Phase 2.2 (Order Management) and Phase 2.4 (Event Handling).
    ///
    /// Unit tests (no category / "Unit"): exercise pre-flight validation and
    /// early-exit paths that return before any API call, using reflection to
    /// control the _isConnected field without a live connection.
    ///
    /// Integration tests ("RequiresApiCredentials"): require real sandbox
    /// credentials supplied via QC_ environment variables (picked up by
    /// TestSetup.ReloadConfiguration).
    /// </summary>
    [TestFixture]
    public class ProjectXBrokerageOrderManagementTests
    {
        private ProjectXBrokerage _brokerage;
        private TestDataAggregator _aggregator;
        private Symbol _testSymbol;

        [SetUp]
        public void SetUp()
        {
            TestSetup.ReloadConfiguration();
            _aggregator = new TestDataAggregator();
            _brokerage = new ProjectXBrokerage(_aggregator);
            _testSymbol = Symbol.CreateFuture("ES", Market.CME, new DateTime(2025, 3, 21));
        }

        [TearDown]
        public void TearDown()
        {
            _brokerage?.Disconnect();
            _brokerage?.Dispose();
        }

        // ── Reflection helpers ──────────────────────────────────────────────────

        private void SetConnected(bool connected) =>
            typeof(ProjectXBrokerage)
                .GetField("_isConnected", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_brokerage, connected);

        private ConcurrentDictionary<int, DateTime> GetRecentlySubmitted() =>
            (ConcurrentDictionary<int, DateTime>)typeof(ProjectXBrokerage)
                .GetField("_recentlySubmittedOrders", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(_brokerage);

        private static MarketOrder CreateOrder(Symbol symbol, int id = 1, decimal qty = 1m)
        {
            var order = new MarketOrder(symbol, qty, DateTime.UtcNow);
            typeof(Order).GetProperty("Id").SetValue(order, id);
            return order;
        }

        private static LimitOrder CreateLimitOrder(Symbol symbol, decimal limitPrice, int id = 1, decimal qty = 1m)
        {
            var order = new LimitOrder(symbol, qty, limitPrice, DateTime.UtcNow);
            typeof(Order).GetProperty("Id").SetValue(order, id);
            return order;
        }

        // ── Helpers for integration tests ────────────────────────────────────────

        private void RequireApiCredentials()
        {
            if (string.IsNullOrEmpty(Config.Get("brokerage-project-x-api-key")))
                Assert.Ignore("API credentials not configured. Set QC_BROKERAGE_PROJECT_X_API_KEY and related env vars.");
        }

        /// <summary>
        /// Returns a Symbol whose Value matches a real ProjectX contract ID.
        /// Skips the test when QC_BROKERAGE_PROJECT_X_TEST_CONTRACT_ID is not set.
        /// Note: Phase 3 (SymbolMapper) is pending; the raw contract ID is used as
        /// the symbol value so PlaceOrderRequest.ContractId receives the correct value.
        /// </summary>
        private Symbol GetTestContractSymbol()
        {
            var contractId = Config.Get("brokerage-project-x-test-contract-id", string.Empty);
            if (string.IsNullOrEmpty(contractId))
                Assert.Ignore("No test contract configured. Set QC_BROKERAGE_PROJECT_X_TEST_CONTRACT_ID to a valid sandbox contract ID.");

            return Symbol.CreateFuture(contractId, Market.USA, DateTime.UtcNow.AddMonths(3));
        }

        // ── PlaceOrder — unit tests ──────────────────────────────────────────────

        [Test]
        public void PlaceOrder_WhenNotConnected_ReturnsFalse()
        {
            var order = CreateOrder(_testSymbol);

            Assert.IsFalse(_brokerage.PlaceOrder(order));
        }

        [Test]
        public void PlaceOrder_WhenNotConnected_FiresInvalidOrderEvent()
        {
            var order = CreateOrder(_testSymbol);
            OrderEvent captured = null;
            _brokerage.OrdersStatusChanged += (_, e) => captured = e[0];

            _brokerage.PlaceOrder(order);

            Assert.IsNotNull(captured);
            Assert.AreEqual(OrderStatus.Invalid, captured.Status);
        }

        [Test]
        public void PlaceOrder_WithZeroQuantity_ReturnsFalse()
        {
            SetConnected(true);
            var order = CreateOrder(_testSymbol, qty: 0m);

            Assert.IsFalse(_brokerage.PlaceOrder(order));
        }

        [Test]
        public void PlaceOrder_WithZeroQuantity_FiresInvalidOrderEvent()
        {
            SetConnected(true);
            var order = CreateOrder(_testSymbol, qty: 0m);
            OrderEvent captured = null;
            _brokerage.OrdersStatusChanged += (_, e) => captured = e[0];

            _brokerage.PlaceOrder(order);

            Assert.IsNotNull(captured);
            Assert.AreEqual(OrderStatus.Invalid, captured.Status);
            Assert.That(captured.Message, Does.Contain("Validation").Or.Contain("validation").Or.Contain("quantity").Or.Contain("Quantity"));
        }

        [Test]
        public void PlaceOrder_WithUnsupportedOrderType_ReturnsFalse()
        {
            SetConnected(true);
            var order = new MarketOnOpenOrder(_testSymbol, 1, DateTime.UtcNow);

            Assert.IsFalse(_brokerage.PlaceOrder(order));
        }

        [Test]
        public void PlaceOrder_WithUnsupportedOrderType_FiresInvalidOrderEvent()
        {
            SetConnected(true);
            var order = new MarketOnOpenOrder(_testSymbol, 1, DateTime.UtcNow);
            OrderEvent captured = null;
            _brokerage.OrdersStatusChanged += (_, e) => captured = e[0];

            _brokerage.PlaceOrder(order);

            Assert.IsNotNull(captured);
            Assert.AreEqual(OrderStatus.Invalid, captured.Status);
        }

        [Test]
        public void PlaceOrder_WithNonFuturesSymbol_ReturnsFalse()
        {
            SetConnected(true);
            var equity = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var order = CreateOrder(equity);

            Assert.IsFalse(_brokerage.PlaceOrder(order));
        }

        [Test]
        public void PlaceOrder_DuplicateSubmission_ReturnsFalse_WithoutFiringEvent()
        {
            SetConnected(true);
            var order = CreateOrder(_testSymbol, id: 77);
            // Pre-populate the duplicate detection window for this order ID
            GetRecentlySubmitted().TryAdd(order.Id, DateTime.UtcNow);

            OrderEvent captured = null;
            _brokerage.OrdersStatusChanged += (_, e) => captured = e[0];

            var result = _brokerage.PlaceOrder(order);

            Assert.IsFalse(result);
            Assert.IsNull(captured, "Duplicate submissions should not fire an order event");
        }

        // ── CancelOrder — unit tests ─────────────────────────────────────────────

        [Test]
        public void CancelOrder_WhenNotConnected_ReturnsFalse()
        {
            var order = CreateOrder(_testSymbol);

            Assert.IsFalse(_brokerage.CancelOrder(order));
        }

        [Test]
        public void CancelOrder_WhenNotConnected_FiresInvalidOrderEvent()
        {
            var order = CreateOrder(_testSymbol);
            OrderEvent captured = null;
            _brokerage.OrdersStatusChanged += (_, e) => captured = e[0];

            _brokerage.CancelOrder(order);

            Assert.IsNotNull(captured);
            Assert.AreEqual(OrderStatus.Invalid, captured.Status);
        }

        [Test]
        public void CancelOrder_WithUnmappedOrderId_ReturnsFalse()
        {
            SetConnected(true);
            var order = CreateOrder(_testSymbol, id: 8888);

            // No mapping exists for this order — returns false before any API call
            Assert.IsFalse(_brokerage.CancelOrder(order));
        }

        // ── UpdateOrder — unit tests ─────────────────────────────────────────────

        [Test]
        public void UpdateOrder_WhenNotConnected_ReturnsFalse()
        {
            var order = CreateLimitOrder(_testSymbol, limitPrice: 4500m);

            Assert.IsFalse(_brokerage.UpdateOrder(order));
        }

        [Test]
        public void UpdateOrder_WithUnmappedOrderId_ReturnsFalse()
        {
            SetConnected(true);
            var order = CreateLimitOrder(_testSymbol, limitPrice: 4500m, id: 7777);

            // No mapping exists — returns false before any API call
            Assert.IsFalse(_brokerage.UpdateOrder(order));
        }

        // ── GetOpenOrders — unit tests ───────────────────────────────────────────

        [Test]
        public void GetOpenOrders_WhenNotConnected_ReturnsEmptyList()
        {
            var orders = _brokerage.GetOpenOrders();

            Assert.IsNotNull(orders);
            Assert.IsEmpty(orders);
        }

        // ── GetAccountHoldings / GetCashBalance — unit tests ─────────────────────

        [Test]
        public void GetAccountHoldings_WhenNotConnected_ReturnsEmptyList()
        {
            var holdings = _brokerage.GetAccountHoldings();

            Assert.IsNotNull(holdings);
            Assert.IsEmpty(holdings);
        }

        [Test]
        public void GetCashBalance_WhenNotConnected_ReturnsEmptyList()
        {
            var balances = _brokerage.GetCashBalance();

            Assert.IsNotNull(balances);
            Assert.IsEmpty(balances);
        }

        // ── Integration tests ────────────────────────────────────────────────────

        [Test, Category("RequiresApiCredentials")]
        public void GetOpenOrders_WhenConnected_ReturnsNonNullList()
        {
            RequireApiCredentials();
            _brokerage.Connect();

            var orders = _brokerage.GetOpenOrders();

            Assert.IsNotNull(orders, "GetOpenOrders should never return null when connected");
        }

        [Test, Category("RequiresApiCredentials")]
        public void GetAccountHoldings_WhenConnected_ReturnsNonNullList()
        {
            RequireApiCredentials();
            _brokerage.Connect();

            var holdings = _brokerage.GetAccountHoldings();

            // API v1.0.1 does not expose a positions endpoint — expect empty list
            Assert.IsNotNull(holdings);
        }

        [Test, Category("RequiresApiCredentials")]
        public void GetCashBalance_WhenConnected_ReturnsNonNullList()
        {
            RequireApiCredentials();
            _brokerage.Connect();

            var balances = _brokerage.GetCashBalance();

            // API v1.0.1 does not expose a balance endpoint — expect empty list
            Assert.IsNotNull(balances);
        }

        [Test, Category("RequiresApiCredentials")]
        public void PlaceLimitOrder_FiresSubmittedEvent_AndReturnsTrue()
        {
            RequireApiCredentials();
            var symbol = GetTestContractSymbol();
            _brokerage.Connect();

            OrderEvent captured = null;
            _brokerage.OrdersStatusChanged += (_, e) => captured = e[0];

            // Use a price far from market so the order rests in the book
            var order = CreateLimitOrder(symbol, limitPrice: 1m, id: 1001);
            var result = _brokerage.PlaceOrder(order);

            Assert.IsTrue(result, "PlaceOrder should return true for a valid order");
            Assert.IsNotNull(captured, "OrdersStatusChanged should fire synchronously");
            Assert.AreEqual(OrderStatus.Submitted, captured.Status);

            // Clean up — best-effort cancel
            _brokerage.CancelOrder(order);
        }

        [Test, Category("RequiresApiCredentials")]
        public void CancelOrder_AfterPlace_FiresCancelPendingEvent()
        {
            RequireApiCredentials();
            var symbol = GetTestContractSymbol();
            _brokerage.Connect();

            var order = CreateLimitOrder(symbol, limitPrice: 1m, id: 1002);
            _brokerage.PlaceOrder(order);

            var events = new ConcurrentBag<OrderEvent>();
            _brokerage.OrdersStatusChanged += (_, e) => events.Add(e[0]);

            var result = _brokerage.CancelOrder(order);

            Assert.IsTrue(result, "CancelOrder should return true");
            Assert.IsTrue(
                SpinWait(() => ContainsStatus(events, OrderStatus.CancelPending), TimeSpan.FromSeconds(5)),
                "CancelPending event should be fired");
        }

        [Test, Category("RequiresApiCredentials")]
        public void PlaceAndCancel_FullLifecycle_WebSocketFiresCanceledEvent()
        {
            RequireApiCredentials();
            var symbol = GetTestContractSymbol();
            _brokerage.Connect();

            var events = new ConcurrentBag<OrderEvent>();
            _brokerage.OrdersStatusChanged += (_, e) => events.Add(e[0]);

            var order = CreateLimitOrder(symbol, limitPrice: 1m, id: 1003);

            Assert.IsTrue(_brokerage.PlaceOrder(order), "PlaceOrder failed");
            Assert.IsTrue(SpinWait(() => ContainsStatus(events, OrderStatus.Submitted), TimeSpan.FromSeconds(5)),
                "Submitted event not received");

            Assert.IsTrue(_brokerage.CancelOrder(order), "CancelOrder failed");
            Assert.IsTrue(SpinWait(() => ContainsStatus(events, OrderStatus.CancelPending), TimeSpan.FromSeconds(5)),
                "CancelPending event not received");

            // Wait for the async WebSocket confirmation
            Assert.IsTrue(SpinWait(() => ContainsStatus(events, OrderStatus.Canceled), TimeSpan.FromSeconds(15)),
                "Canceled event was not received from WebSocket within 15 s");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static bool SpinWait(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return true;
                Thread.Sleep(100);
            }
            return condition();
        }

        private static bool ContainsStatus(ConcurrentBag<OrderEvent> bag, OrderStatus status)
        {
            foreach (var e in bag)
                if (e.Status == status) return true;
            return false;
        }
    }
}
