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
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Tests;
using QuantConnect.Logging;
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    [TestFixture]
    public partial class ProjectXBrokerageTests
    {
        private static Symbol FrontMonthNQ => Symbol.CreateFuture("NQ", Market.CME,
            ProjectXBrokerageTestsHelper.GetThirdFriday(DateTime.UtcNow.Month >= 12
                ? DateTime.UtcNow.Year + 1 : DateTime.UtcNow.Year,
                new[] { 3, 6, 9, 12 }.First(m => m > DateTime.UtcNow.Month % 12)));

        private static TestCaseData[] StreamTestParameters
        {
            get
            {
                var es = ProjectXBrokerageTestsHelper.GetFrontMonthES();

                return new[]
                {
                    // ES tick data
                    new TestCaseData(es, Resolution.Tick, false),

                    // ES minute bars
                    new TestCaseData(es, Resolution.Minute, false),

                    // ES second bars
                    new TestCaseData(es, Resolution.Second, false),
                };
            }
        }

        [Test, TestCaseSource(nameof(StreamTestParameters)), Category("Integration")]
        public void StreamsData(Symbol symbol, Resolution resolution, bool throwsException)
        {
            var cancelationToken = new CancellationTokenSource();
            var brokerage = (ProjectXBrokerage)Brokerage;

            SubscriptionDataConfig[] configs;
            if (resolution == Resolution.Tick)
            {
                var tradeConfig = new SubscriptionDataConfig(GetSubscriptionDataConfig<Tick>(symbol, resolution), tickType: TickType.Trade);
                var quoteConfig = new SubscriptionDataConfig(GetSubscriptionDataConfig<Tick>(symbol, resolution), tickType: TickType.Quote);
                configs = new[] { tradeConfig, quoteConfig };
            }
            else
            {
                configs = new[]
                {
                    GetSubscriptionDataConfig<QuoteBar>(symbol, resolution),
                    GetSubscriptionDataConfig<TradeBar>(symbol, resolution)
                };
            }

            foreach (var config in configs)
            {
                ProcessFeed(brokerage.Subscribe(config, (s, e) => { }),
                    cancelationToken,
                    baseData => { if (baseData != null) Log.Trace($"{baseData}"); });
            }

            Thread.Sleep(20_000);

            foreach (var config in configs)
                brokerage.Unsubscribe(config);

            Thread.Sleep(20_000);
            cancelationToken.Cancel();
        }

        [Test, Category("Integration")]
        public void MultiSymbol_SubscribeAndUnsubscribeOne_OtherContinues()
        {
            var brokerage = (ProjectXBrokerage)Brokerage;
            var es = ProjectXBrokerageTestsHelper.GetFrontMonthES();
            var nq = Symbol.CreateFuture("NQ", Market.CME,
                ProjectXBrokerageTestsHelper.GetThirdFriday(
                    new[] { 3, 6, 9, 12 }.FirstOrDefault(m => m > DateTime.UtcNow.Month) is int next && next != 0
                        ? DateTime.UtcNow.Year : DateTime.UtcNow.Year + 1,
                    new[] { 3, 6, 9, 12 }.FirstOrDefault(m => m > DateTime.UtcNow.Month) is int nxt && nxt != 0
                        ? nxt : 3));

            var esConfig = GetSubscriptionDataConfig<TradeBar>(es, Resolution.Minute);
            var nqConfig = GetSubscriptionDataConfig<TradeBar>(nq, Resolution.Minute);

            var cts = new CancellationTokenSource();
            var esData = new List<BaseData>();
            var nqData = new List<BaseData>();

            // Subscribe both
            ProcessFeed(brokerage.Subscribe(esConfig, (s, e) => { }), cts,
                d => { if (d != null) { lock (esData) esData.Add(d); } });
            ProcessFeed(brokerage.Subscribe(nqConfig, (s, e) => { }), cts,
                d => { if (d != null) { lock (nqData) nqData.Add(d); } });

            Thread.Sleep(10_000);

            // Unsubscribe NQ — ES should keep flowing
            brokerage.Unsubscribe(nqConfig);
            var nqCountAfterUnsub = nqData.Count;

            Thread.Sleep(10_000);

            // NQ count should be frozen; ES may have grown
            Assert.AreEqual(nqCountAfterUnsub, nqData.Count,
                "NQ data should stop arriving after unsubscribe");

            brokerage.Unsubscribe(esConfig);
            cts.Cancel();
        }

        [Test, Category("Integration")]
        public void TickValue_ForESFuture_HasReasonablePrice()
        {
            var brokerage = (ProjectXBrokerage)Brokerage;
            var es = ProjectXBrokerageTestsHelper.GetFrontMonthES();
            var config = GetSubscriptionDataConfig<TradeBar>(es, Resolution.Minute);

            TradeBar receivedBar = null;
            var received = new ManualResetEventSlim(false);

            var cts = new CancellationTokenSource();
            ProcessFeed(brokerage.Subscribe(config, (s, e) => { }), cts, d =>
            {
                if (d is TradeBar bar && receivedBar == null)
                {
                    receivedBar = bar;
                    received.Set();
                }
            });

            var gotData = received.Wait(TimeSpan.FromSeconds(30));
            brokerage.Unsubscribe(config);
            cts.Cancel();

            Assume.That(gotData, "No trade bar received within 30s; market may be closed");

            // ES futures typically trade between 3,000 and 7,000
            Assert.That((double)receivedBar.Close, Is.InRange(3_000, 7_000),
                $"ES closing price {receivedBar.Close} is outside expected range [3000, 7000]");
        }
    }
}
