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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Tests;
using QuantConnect.Tests.Brokerages;
using MarqSpec.Client.ProjectX.Api.Models;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    [TestFixture, Category("Integration")]
    public partial class ProjectXBrokerageTests : BrokerageTests
    {
        // ----------------------------------------------------------------
        // BrokerageTests abstract member implementations
        // ----------------------------------------------------------------

        protected override Symbol Symbol => GetFrontMonthES();

        protected override SecurityType SecurityType => SecurityType.Future;

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            TestSetup.ReloadConfiguration();
            return new ProjectXBrokerage(new TestDataAggregator());
        }

        protected override bool IsAsync() => false;

        protected override decimal GetAskPrice(Symbol symbol)
        {
            var brokerage = (ProjectXBrokerage)Brokerage;
            var apiClient = (MarqSpec.Client.ProjectX.IProjectXApiClient)typeof(ProjectXBrokerage)
                .GetField("_apiClient", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(brokerage);
            var symbolMapper = (ProjectXSymbolMapper)typeof(ProjectXBrokerage)
                .GetField("_symbolMapper", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(brokerage);

            var contractId = symbolMapper.GetBrokerageSymbol(symbol);
            var now = DateTime.UtcNow;
            var bars = apiClient.GetHistoricalBarsAsync(
                contractId, now.AddMinutes(-5), now,
                AggregateBarUnit.Minute, 1, 5, live: true, includePartialBar: false)
                .GetAwaiter().GetResult();

            return (decimal)(bars?.LastOrDefault()?.Close ?? 0m);
        }

        // ----------------------------------------------------------------
        // Credential guard — skip if API key not configured
        // ----------------------------------------------------------------

        [SetUp]
        public void CheckCredentials()
        {
            var apiKey = Config.Get("brokerage-project-x-api-key", string.Empty);
            Assume.That(!string.IsNullOrEmpty(apiKey),
                "Skipping: brokerage-project-x-api-key not configured. " +
                "Set the QC_BROKERAGE_PROJECT_X_API_KEY environment variable to run integration tests.");
        }

        // ----------------------------------------------------------------
        // Order parameters — ES front-month futures only
        // ----------------------------------------------------------------

        /// <summary>
        /// Provides the data required to test each order type in various cases
        /// </summary>
        private static IEnumerable<TestCaseData> OrderParameters()
        {
            var symbol = GetFrontMonthES();

            // Wide high/low limits so orders don't accidentally fill at market
            const decimal highLimit = 99_000m;
            const decimal lowLimit  = 100m;

            yield return new TestCaseData(new MarketOrderTestParameters(symbol));
            yield return new TestCaseData(new LimitOrderTestParameters(symbol, highLimit, lowLimit));
            yield return new TestCaseData(new StopMarketOrderTestParameters(symbol, highLimit, lowLimit));
            yield return new TestCaseData(new StopLimitOrderTestParameters(symbol, highLimit, lowLimit));
            yield return new TestCaseData(new TrailingStopOrderTestParameters(symbol, highLimit, lowLimit,
                trailingAmount: 10m, trailingAsPercentage: false));
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CancelOrders(OrderTestParameters parameters)
        {
            base.CancelOrders(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromZero(OrderTestParameters parameters)
        {
            base.LongFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CloseFromLong(OrderTestParameters parameters)
        {
            base.CloseFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void ShortFromZero(OrderTestParameters parameters)
        {
            base.ShortFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CloseFromShort(OrderTestParameters parameters)
        {
            base.CloseFromShort(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void ShortFromLong(OrderTestParameters parameters)
        {
            base.ShortFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromShort(OrderTestParameters parameters)
        {
            base.LongFromShort(parameters);
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        /// <summary>
        /// Returns the front-month ES (E-mini S&amp;P 500) futures symbol.
        /// </summary>
        private static Symbol GetFrontMonthES() => ProjectXBrokerageTestsHelper.GetFrontMonthES();
    }
}