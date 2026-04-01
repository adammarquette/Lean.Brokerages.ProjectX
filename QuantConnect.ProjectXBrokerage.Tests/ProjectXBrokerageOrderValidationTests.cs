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
using System.Reflection;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Configuration;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    /// <summary>
    /// Unit tests for the private ValidateOrder method, exercised via reflection.
    /// No API credentials or network access required.
    /// </summary>
    [TestFixture]
    public class ProjectXBrokerageOrderValidationTests
    {
        private ProjectXBrokerage _brokerage;
        private Symbol _testSymbol;

        [SetUp]
        public void SetUp()
        {
            Config.Set("brokerage-project-x-api-key", "unit-test-key");
            Config.Set("brokerage-project-x-api-secret", "unit-test-secret");
            Config.Set("brokerage-project-x-environment", "sandbox");
            _brokerage = new ProjectXBrokerage(new TestDataAggregator());
            _testSymbol = ProjectXBrokerageTestsHelper.GetFrontMonthES();
        }

        [TearDown]
        public void TearDown()
        {
            Config.Reset();
            _brokerage?.Dispose();
        }

        // ── Reflection helpers ──────────────────────────────────────────────────

        private bool InvokeValidateOrder(Order order, out string errorMessage)
        {
            var method = typeof(ProjectXBrokerage).GetMethod(
                "ValidateOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            var args = new object[] { order, null };
            var result = (bool)method.Invoke(_brokerage, args);
            errorMessage = (string)args[1];
            return result;
        }

        private void SetConnected(bool connected) =>
            typeof(ProjectXBrokerage)
                .GetField("_isConnected", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_brokerage, connected);

        // ── Connection state ─────────────────────────────────────────────────────

        [Test]
        public void ValidateOrder_WhenNotConnected_ReturnsFalse()
        {
            var order = new MarketOrder(_testSymbol, 1, DateTime.UtcNow);

            var result = InvokeValidateOrder(order, out var error);

            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("Not connected").Or.Contain("not connected"));
        }

        [Test]
        public void ValidateOrder_WhenConnected_AllowsValidOrder()
        {
            SetConnected(true);
            var order = new MarketOrder(_testSymbol, 1, DateTime.UtcNow);

            Assert.IsTrue(InvokeValidateOrder(order, out _));
        }

        // ── Null / missing input ─────────────────────────────────────────────────

        [Test]
        public void ValidateOrder_NullOrder_ReturnsFalse()
        {
            SetConnected(true);

            var result = InvokeValidateOrder(null, out var error);

            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("null").Or.Contain("cannot be null"));
        }

        // ── Supported order types ────────────────────────────────────────────────

        [Test]
        public void ValidateOrder_MarketOrder_ReturnsTrue()
        {
            SetConnected(true);
            Assert.IsTrue(InvokeValidateOrder(new MarketOrder(_testSymbol, 1, DateTime.UtcNow), out _));
        }

        [Test]
        public void ValidateOrder_LimitOrder_WithValidPrice_ReturnsTrue()
        {
            SetConnected(true);
            Assert.IsTrue(InvokeValidateOrder(new LimitOrder(_testSymbol, 1, 4500m, DateTime.UtcNow), out _));
        }

        [Test]
        public void ValidateOrder_StopMarketOrder_WithValidStop_ReturnsTrue()
        {
            SetConnected(true);
            Assert.IsTrue(InvokeValidateOrder(new StopMarketOrder(_testSymbol, 1, 4500m, DateTime.UtcNow), out _));
        }

        [Test]
        public void ValidateOrder_StopLimitOrder_WithValidPrices_ReturnsTrue()
        {
            SetConnected(true);
            Assert.IsTrue(InvokeValidateOrder(new StopLimitOrder(_testSymbol, 1, 4490m, 4500m, DateTime.UtcNow), out _));
        }

        // ── Unsupported order types ──────────────────────────────────────────────

        [Test]
        public void ValidateOrder_MarketOnOpenOrder_ReturnsFalse()
        {
            SetConnected(true);
            var result = InvokeValidateOrder(new MarketOnOpenOrder(_testSymbol, 1, DateTime.UtcNow), out var error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("not supported"));
        }

        [Test]
        public void ValidateOrder_MarketOnCloseOrder_ReturnsFalse()
        {
            SetConnected(true);
            var result = InvokeValidateOrder(new MarketOnCloseOrder(_testSymbol, 1, DateTime.UtcNow), out var error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("not supported"));
        }

        [Test]
        public void ValidateOrder_LimitIfTouchedOrder_ReturnsFalse()
        {
            SetConnected(true);
            var result = InvokeValidateOrder(new LimitIfTouchedOrder(_testSymbol, 1, 4490m, 4500m, DateTime.UtcNow), out var error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("not supported"));
        }

        // ── Price validation ─────────────────────────────────────────────────────

        [Test]
        public void ValidateOrder_LimitOrder_WithZeroPrice_ReturnsFalse()
        {
            SetConnected(true);
            var result = InvokeValidateOrder(new LimitOrder(_testSymbol, 1, 0m, DateTime.UtcNow), out var error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("Limit price").Or.Contain("limit price"));
        }

        [Test]
        public void ValidateOrder_LimitOrder_WithNegativePrice_ReturnsFalse()
        {
            SetConnected(true);
            var result = InvokeValidateOrder(new LimitOrder(_testSymbol, 1, -100m, DateTime.UtcNow), out var error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("Limit price").Or.Contain("limit price"));
        }

        [Test]
        public void ValidateOrder_StopMarketOrder_WithZeroStop_ReturnsFalse()
        {
            SetConnected(true);
            var result = InvokeValidateOrder(new StopMarketOrder(_testSymbol, 1, 0m, DateTime.UtcNow), out var error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("Stop price").Or.Contain("stop price"));
        }

        // ── Quantity validation ──────────────────────────────────────────────────

        [Test]
        public void ValidateOrder_ZeroQuantity_ReturnsFalse()
        {
            SetConnected(true);
            var result = InvokeValidateOrder(new MarketOrder(_testSymbol, 0, DateTime.UtcNow), out var error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("quantity").Or.Contain("Quantity"));
        }

        [Test]
        public void ValidateOrder_PositiveQuantity_Buy_ReturnsTrue()
        {
            SetConnected(true);
            Assert.IsTrue(InvokeValidateOrder(new MarketOrder(_testSymbol, 5, DateTime.UtcNow), out _));
        }

        [Test]
        public void ValidateOrder_NegativeQuantity_Sell_ReturnsTrue()
        {
            SetConnected(true);
            Assert.IsTrue(InvokeValidateOrder(new MarketOrder(_testSymbol, -3, DateTime.UtcNow), out _));
        }

        // ── Symbol validation ────────────────────────────────────────────────────

        [Test]
        public void ValidateOrder_EquitySymbol_ReturnsFalse()
        {
            SetConnected(true);
            var equity = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var result = InvokeValidateOrder(new MarketOrder(equity, 1, DateTime.UtcNow), out var error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("not supported"));
        }

        [Test]
        public void ValidateOrder_CanonicalFutureSymbol_ReturnsFalse()
        {
            SetConnected(true);
            // Canonical = no expiry date; IsCanonical() returns true → CanSubscribe = false
            var canonical = Symbol.Create("ES", SecurityType.Future, Market.CME);
            var result = InvokeValidateOrder(new MarketOrder(canonical, 1, DateTime.UtcNow), out var error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("not supported"));
        }

        [Test]
        public void ValidateOrder_UniverseSymbol_ReturnsFalse()
        {
            SetConnected(true);
            // "universe" in the symbol value → CanSubscribe returns false
            var universe = Symbol.CreateFuture("ES.universe", Market.CME, new DateTime(2025, 3, 21));
            var result = InvokeValidateOrder(new MarketOrder(universe, 1, DateTime.UtcNow), out var error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("not supported"));
        }

        // ── Rapid re-connection guard ────────────────────────────────────────────

        [Test]
        public void ValidateOrder_DisconnectMidOrder_ReturnsFalse()
        {
            // Simulate: brokerage was connected when order arrived but lost connection
            // before validation completes.
            SetConnected(true);
            var order = new MarketOrder(_testSymbol, 1, DateTime.UtcNow);
            // Flip connected to false before validation
            SetConnected(false);

            var result = InvokeValidateOrder(order, out var error);

            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("Not connected").Or.Contain("not connected"),
                "Should report connection failure when state changes under the order");
        }

        [Test]
        public void ValidateOrder_ExpiredFuturesContract_ReturnsFalse()
        {
            SetConnected(true);
            // A contract whose expiry is clearly in the past
            var expired = Symbol.CreateFuture("ES", Market.CME, new DateTime(2000, 3, 17));
            var result = InvokeValidateOrder(new MarketOrder(expired, 1, DateTime.UtcNow), out var error);

            // Expired contracts are not tradeable; validation should fail
            Assert.IsFalse(result);
        }

        [Test]
        public void ValidateOrder_LimitPrice_Zero_ReturnsFalse()
        {
            SetConnected(true);
            var order = new LimitOrder(_testSymbol, quantity: 1, limitPrice: 0m, time: DateTime.UtcNow);

            var result = InvokeValidateOrder(order, out var error);

            Assert.IsFalse(result,
                "A limit order with price 0 is invalid; ValidateOrder should reject it");
        }
    }
}
