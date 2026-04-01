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
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Tests;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.HistoricalData;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    [TestFixture, Category("Integration")]
    public class ProjectXBrokerageHistoryProviderTests
    {
        // Front-month ES futures — expires third Friday of Mar/Jun/Sep/Dec
        private static Symbol FrontMonthES => ProjectXBrokerageTestsHelper.GetFrontMonthES();

        [SetUp]
        public void CheckCredentials()
        {
            var apiKey = QuantConnect.Configuration.Config.Get("brokerage-project-x-api-key", string.Empty);
            Assume.That(!string.IsNullOrEmpty(apiKey),
                "Skipping: brokerage-project-x-api-key not configured. " +
                "Set the QC_BROKERAGE_PROJECT_X_API_KEY environment variable to run integration tests.");
        }

        private static TestCaseData[] TestParameters
        {
            get
            {
                TestGlobals.Initialize();
                TestSetup.ReloadConfiguration();
                var es = ProjectXBrokerageTestsHelper.GetFrontMonthES();

                return
                [
                    // ES futures — minute trade bars
                    new TestCaseData(es, Resolution.Minute, TimeSpan.FromMinutes(30), TickType.Trade, typeof(TradeBar), false),

                    // ES futures — hourly trade bars
                    new TestCaseData(es, Resolution.Hour, TimeSpan.FromHours(8), TickType.Trade, typeof(TradeBar), false),

                    // ES futures — daily trade bars
                    new TestCaseData(es, Resolution.Daily, TimeSpan.FromDays(10), TickType.Trade, typeof(TradeBar), false),

                    // ES futures — minute quote bars
                    new TestCaseData(es, Resolution.Minute, TimeSpan.FromMinutes(30), TickType.Quote, typeof(QuoteBar), false),

                    // Invalid: equity not supported
                    new TestCaseData(Symbols.SPY, Resolution.Hour, TimeSpan.FromHours(14), TickType.Trade, typeof(TradeBar), true),
                ];
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, TickType tickType, Type dataType, bool invalidRequest)
        {
            var brokerage = new ProjectXBrokerage(new TestDataAggregator());

            var historyProvider = new BrokerageHistoryProvider();
            historyProvider.SetBrokerage(brokerage);
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null,
                null, null, null, null,
                false, null, null, new AlgorithmSettings()));

            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var now = DateTime.UtcNow;
            var requests = new[]
            {
                new HistoryRequest(now.Add(-period),
                    now,
                    dataType,
                    symbol,
                    resolution,
                    marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType),
                    marketHoursDatabase.GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType),
                    resolution,
                    false,
                    false,
                    DataNormalizationMode.Adjusted,
                    tickType)
            };

            var historyArray = historyProvider.GetHistory(requests, TimeZones.Utc)?.ToArray();
            if (invalidRequest)
            {
                Assert.Null(historyArray);
                return;
            }

            Assert.NotNull(historyArray);
            foreach (var slice in historyArray)
            {
                if (resolution == Resolution.Tick)
                {
                    foreach (var tick in slice.Ticks[symbol])
                    {
                        Log.Debug($"{tick}");
                    }
                }
                else if (slice.QuoteBars.TryGetValue(symbol, out var quoteBar))
                {
                    Log.Debug($"{quoteBar}");
                }
                else if (slice.Bars.TryGetValue(symbol, out var tradeBar))
                {
                    Log.Debug($"{tradeBar}");
                }
            }

            if (historyProvider.DataPointCount > 0)
            {
                // Ordered by time
                Assert.That(historyArray, Is.Ordered.By("Time"));

                // No repeating bars
                var timesArray = historyArray.Select(x => x.Time).ToArray();
                Assert.AreEqual(timesArray.Length, timesArray.Distinct().Count());
            }

            Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
        }
    }
}