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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    /// <summary>
    /// Performance regression tests for the ProjectX brokerage adapter.
    /// Run explicitly with: dotnet test --filter "Category=Performance"
    /// No API credentials required — all tests use in-process operations.
    /// </summary>
    [TestFixture, Category("Performance"), Explicit("Performance tests; run explicitly")]
    public class ProjectXBrokeragePerformanceTests
    {
        private ProjectXBrokerage _brokerage;
        private Symbol _testSymbol;

        [SetUp]
        public void SetUp()
        {
            Config.Set("brokerage-project-x-api-key", "perf-test-key");
            Config.Set("brokerage-project-x-api-secret", "perf-test-secret");
            Config.Set("brokerage-project-x-environment", "sandbox");

            _brokerage = new ProjectXBrokerage(new TestDataAggregator());
            _testSymbol = ProjectXBrokerageTestsHelper.GetFrontMonthES();

            // Set connected so ValidateOrder paths are exercised
            typeof(ProjectXBrokerage)
                .GetField("_isConnected", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_brokerage, true);
        }

        [TearDown]
        public void TearDown()
        {
            Config.Reset();
            _brokerage?.Dispose();
        }

        // ── Symbol mapper throughput ─────────────────────────────────────────────

        [Test]
        public void SymbolMapper_GetBrokerageSymbol_100kOps_Under500ms()
        {
            const int iterations = 100_000;
            var mapper = new ProjectXSymbolMapper();
            var symbols = new[]
            {
                Symbol.CreateFuture("ES",  Market.CME, new DateTime(2025,  3, 21)),
                Symbol.CreateFuture("NQ",  Market.CME, new DateTime(2025,  3, 21)),
                Symbol.CreateFuture("RTY", Market.CME, new DateTime(2025,  3, 21)),
                Symbol.CreateFuture("YM",  Market.CME, new DateTime(2025,  3, 21)),
            };

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                mapper.GetBrokerageSymbol(symbols[i % symbols.Length]);
            sw.Stop();

            TestContext.WriteLine($"SymbolMapper.GetBrokerageSymbol: {iterations:N0} ops in {sw.ElapsedMilliseconds} ms");
            Assert.Less(sw.ElapsedMilliseconds, 500,
                $"Expected <500 ms for {iterations:N0} ops; got {sw.ElapsedMilliseconds} ms");
        }

        [Test]
        public void SymbolMapper_GetLeanSymbol_100kOps_Under500ms()
        {
            const int iterations = 100_000;
            var mapper = new ProjectXSymbolMapper();
            var brokerageIds = new[] { "ESH25", "NQH25", "RTYH25", "YMH25" };

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                mapper.GetLeanSymbol(brokerageIds[i % brokerageIds.Length], SecurityType.Future, string.Empty);
            sw.Stop();

            TestContext.WriteLine($"SymbolMapper.GetLeanSymbol: {iterations:N0} ops in {sw.ElapsedMilliseconds} ms");
            Assert.Less(sw.ElapsedMilliseconds, 500,
                $"Expected <500 ms for {iterations:N0} ops; got {sw.ElapsedMilliseconds} ms");
        }

        // ── Order validation throughput ──────────────────────────────────────────

        [Test]
        public void ValidateOrder_50kOps_Under1000ms()
        {
            const int iterations = 50_000;
            var validateMethod = typeof(ProjectXBrokerage)
                .GetMethod("ValidateOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            var order = new MarketOrder(_testSymbol, 1, DateTime.UtcNow);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                validateMethod.Invoke(_brokerage, new object[] { order, null });
            sw.Stop();

            TestContext.WriteLine($"ValidateOrder: {iterations:N0} ops in {sw.ElapsedMilliseconds} ms");
            Assert.Less(sw.ElapsedMilliseconds, 1_000,
                $"Expected <1000 ms for {iterations:N0} validation ops; got {sw.ElapsedMilliseconds} ms");
        }

        // ── Fee model throughput ─────────────────────────────────────────────────

        [Test]
        public void FeeModel_GetOrderFee_100kOps_Under200ms()
        {
            const int iterations = 100_000;
            var feeModel = new ProjectXFeeModel();
            var security = new QuantConnect.Securities.Future.Future(
                _testSymbol,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Chicago),
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            var order = new MarketOrder(_testSymbol, 1, DateTime.UtcNow);
            var parameters = new OrderFeeParameters(security, order);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                feeModel.GetOrderFee(parameters);
            sw.Stop();

            TestContext.WriteLine($"FeeModel.GetOrderFee: {iterations:N0} ops in {sw.ElapsedMilliseconds} ms");
            Assert.Less(sw.ElapsedMilliseconds, 200,
                $"Expected <200 ms for {iterations:N0} fee model ops; got {sw.ElapsedMilliseconds} ms");
        }

        // ── Parallel symbol mapping ──────────────────────────────────────────────

        [Test]
        public void SymbolMapper_ParallelGetBrokerageSymbol_NoExceptions()
        {
            const int parallelOps = 10_000;
            var mapper = new ProjectXSymbolMapper();
            var symbols = Enumerable.Range(0, 12).Select(m =>
                Symbol.CreateFuture("ES", Market.CME, new DateTime(2025, (m % 12) + 1, 1))).ToArray();

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            Parallel.For(0, parallelOps, i =>
            {
                try { mapper.GetBrokerageSymbol(symbols[i % symbols.Length]); }
                catch (Exception ex) { exceptions.Add(ex); }
            });

            Assert.IsEmpty(exceptions,
                $"Parallel symbol mapping raised {exceptions.Count} exception(s): " +
                string.Join("; ", exceptions.Take(3).Select(e => e.Message)));
        }
    }
}
