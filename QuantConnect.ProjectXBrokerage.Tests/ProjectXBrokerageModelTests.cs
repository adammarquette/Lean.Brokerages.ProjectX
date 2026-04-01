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
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Tests;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    [TestFixture]
    public class ProjectXBrokerageModelTests
    {
        private ProjectXBrokerageModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new ProjectXBrokerageModel();
        }

        // ── GetFeeModel type check ───────────────────────────────────────────────

        [Test]
        public void GetFeeModel_ReturnsFeeModel_IsProjectXFeeModel()
        {
            var symbol = Symbol.CreateFuture("ES", Market.CME, new DateTime(2026, 6, 19));
            var security = CreateFutureSecurity(symbol);

            var feeModel = _model.GetFeeModel(security);

            Assert.IsInstanceOf<ProjectXFeeModel>(feeModel);
        }

        // ── Per-side fee spot-checks (1 contract) ───────────────────────────────

        [TestCase("ES",  Market.CME,  1, 1.40)]   // $2.80 RT ÷ 2
        [TestCase("MES", Market.CME,  1, 0.37)]   // $0.74 RT ÷ 2
        [TestCase("NQ",  Market.CME,  1, 1.40)]   // $2.80 RT ÷ 2
        [TestCase("ZB",  Market.CBOT, 1, 0.89)]   // $1.78 RT ÷ 2
        [TestCase("CL",  Market.NYMEX, 1, 1.52)]  // $3.04 RT ÷ 2
        [TestCase("GC",  Market.NYMEX, 1, 1.62)]  // $3.24 RT ÷ 2
        [TestCase("6E",  Market.CME,  1, 1.62)]   // $3.24 RT ÷ 2
        [TestCase("ZC",  Market.CBOT, 1, 2.15)]   // $4.30 RT ÷ 2
        public void GetOrderFee_KnownSymbol_ReturnsCorrectPerSideFee(
            string ticker, string market, int quantity, double expectedFee)
        {
            var symbol = Symbol.CreateFuture(ticker, market, new DateTime(2026, 6, 19));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, quantity, DateTime.UtcNow);

            var fee = GetOrderFee(security, order);

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual((decimal)expectedFee, fee.Value.Amount, $"Expected {expectedFee} per-side fee for {ticker}");
        }

        // ── Unknown symbol uses default ($2.80 RT → $1.40/side) ─────────────────

        [Test]
        public void GetOrderFee_UnknownSymbol_FallsBackToDefault()
        {
            var symbol = Symbol.CreateFuture("XX", Market.CME, new DateTime(2026, 6, 19));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

            var fee = GetOrderFee(security, order);

            Assert.AreEqual(1.40m, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        // ── Fee scales linearly with quantity ────────────────────────────────────

        [Test]
        public void GetOrderFee_ES_5Contracts_Returns7_00()
        {
            var symbol = Symbol.CreateFuture("ES", Market.CME, new DateTime(2026, 6, 19));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 5, DateTime.UtcNow);

            var fee = GetOrderFee(security, order);

            Assert.AreEqual(7.00m, fee.Value.Amount);
        }

        [Test]
        public void GetOrderFee_ES_SellOrder_FeeIsPositive()
        {
            var symbol = Symbol.CreateFuture("ES", Market.CME, new DateTime(2026, 6, 19));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, -1, DateTime.UtcNow); // sell

            var fee = GetOrderFee(security, order);

            Assert.AreEqual(1.40m, fee.Value.Amount);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static Security CreateFutureSecurity(Symbol symbol)
        {
            return new Future(
                symbol,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0m, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
        }

        private OrderFee GetOrderFee(Security security, Order order)
        {
            var feeModel = _model.GetFeeModel(security);
            return feeModel.GetOrderFee(new OrderFeeParameters(security, order));
        }
    }
}
