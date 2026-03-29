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
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    [TestFixture]
    public class ProjectXBrokerageSymbolMapperTests
    {
        private ProjectXSymbolMapper _mapper;

        [SetUp]
        public void SetUp()
        {
            _mapper = new ProjectXSymbolMapper();
        }

        #region GetBrokerageSymbol

        private static readonly object[] GetBrokerageSymbolCases =
        {
            // CME equity index futures
            new TestCaseData(Symbol.CreateFuture("ES",  Market.CME,   new DateTime(2025, 3, 21)), "ESH25"),
            new TestCaseData(Symbol.CreateFuture("NQ",  Market.CME,   new DateTime(2025, 6, 20)), "NQM25"),
            new TestCaseData(Symbol.CreateFuture("RTY", Market.CME,   new DateTime(2025, 9, 19)), "RTYU25"),
            new TestCaseData(Symbol.CreateFuture("YM",  Market.CME,   new DateTime(2025, 12, 19)), "YMZ25"),
            new TestCaseData(Symbol.CreateFuture("MES", Market.CME,   new DateTime(2025, 3, 21)), "MESH25"),

            // CBOT treasury futures
            new TestCaseData(Symbol.CreateFuture("ZB",  Market.CBOT,  new DateTime(2025, 9, 19)), "ZBU25"),
            new TestCaseData(Symbol.CreateFuture("ZN",  Market.CBOT,  new DateTime(2025, 12, 19)), "ZNZ25"),

            // NYMEX energy and metals futures
            new TestCaseData(Symbol.CreateFuture("CL",  Market.NYMEX, new DateTime(2024, 12, 20)), "CLZ24"),
            new TestCaseData(Symbol.CreateFuture("NG",  Market.NYMEX, new DateTime(2025, 1, 17)), "NGF25"),
            new TestCaseData(Symbol.CreateFuture("GC",  Market.NYMEX, new DateTime(2025, 4, 17)), "GCJ25"),
            new TestCaseData(Symbol.CreateFuture("SI",  Market.NYMEX, new DateTime(2025, 5, 16)), "SIK25"),

            // ICE futures
            new TestCaseData(Symbol.CreateFuture("BRN", Market.ICE,   new DateTime(2025, 6, 20)), "BRNM25"),

            // All twelve month codes
            new TestCaseData(Symbol.CreateFuture("ES",  Market.CME,   new DateTime(2025, 1, 17)), "ESF25"),
            new TestCaseData(Symbol.CreateFuture("ES",  Market.CME,   new DateTime(2025, 2, 21)), "ESG25"),
            new TestCaseData(Symbol.CreateFuture("ES",  Market.CME,   new DateTime(2025, 4, 17)), "ESJ25"),
            new TestCaseData(Symbol.CreateFuture("ES",  Market.CME,   new DateTime(2025, 5, 16)), "ESK25"),
            new TestCaseData(Symbol.CreateFuture("ES",  Market.CME,   new DateTime(2025, 7, 18)), "ESN25"),
            new TestCaseData(Symbol.CreateFuture("ES",  Market.CME,   new DateTime(2025, 8, 15)), "ESQ25"),
            new TestCaseData(Symbol.CreateFuture("ES",  Market.CME,   new DateTime(2025, 10, 17)), "ESV25"),
            new TestCaseData(Symbol.CreateFuture("ES",  Market.CME,   new DateTime(2025, 11, 21)), "ESX25"),
        };

        [Test, TestCaseSource(nameof(GetBrokerageSymbolCases))]
        public void GetBrokerageSymbol_ReturnsCorrectTicker(Symbol leanSymbol, string expectedTicker)
        {
            var result = _mapper.GetBrokerageSymbol(leanSymbol);
            Assert.That(result, Is.EqualTo(expectedTicker));
        }

        [Test]
        public void GetBrokerageSymbol_NullSymbol_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _mapper.GetBrokerageSymbol(null));
        }

        [Test]
        public void GetBrokerageSymbol_NonFuturesSymbol_ThrowsArgumentException()
        {
            var equitySymbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSymbol(equitySymbol));
        }

        #endregion

        #region GetLeanSymbol

        private static readonly object[] GetLeanSymbolCases =
        {
            // CME equity index futures
            new TestCaseData("ESH25",  SecurityType.Future, Market.CME,   "ES",  Market.CME,   ProjectXSymbolMapper.GetThirdFriday(2025, 3)),
            new TestCaseData("NQM25",  SecurityType.Future, Market.CME,   "NQ",  Market.CME,   ProjectXSymbolMapper.GetThirdFriday(2025, 6)),
            new TestCaseData("RTYU25", SecurityType.Future, Market.CME,   "RTY", Market.CME,   ProjectXSymbolMapper.GetThirdFriday(2025, 9)),
            new TestCaseData("YMZ25",  SecurityType.Future, Market.CME,   "YM",  Market.CME,   ProjectXSymbolMapper.GetThirdFriday(2025, 12)),
            new TestCaseData("MESH25", SecurityType.Future, Market.CME,   "MES", Market.CME,   ProjectXSymbolMapper.GetThirdFriday(2025, 3)),

            // CBOT treasury futures
            new TestCaseData("ZBU25",  SecurityType.Future, Market.CBOT,  "ZB",  Market.CBOT,  ProjectXSymbolMapper.GetThirdFriday(2025, 9)),
            new TestCaseData("ZNZ25",  SecurityType.Future, Market.CBOT,  "ZN",  Market.CBOT,  ProjectXSymbolMapper.GetThirdFriday(2025, 12)),

            // NYMEX energy and metals futures
            new TestCaseData("CLZ24",  SecurityType.Future, Market.NYMEX, "CL",  Market.NYMEX, ProjectXSymbolMapper.GetThirdFriday(2024, 12)),
            new TestCaseData("NGF25",  SecurityType.Future, Market.NYMEX, "NG",  Market.NYMEX, ProjectXSymbolMapper.GetThirdFriday(2025, 1)),
            new TestCaseData("GCJ25",  SecurityType.Future, Market.NYMEX, "GC",  Market.NYMEX, ProjectXSymbolMapper.GetThirdFriday(2025, 4)),
            new TestCaseData("SIK25",  SecurityType.Future, Market.NYMEX, "SI",  Market.NYMEX, ProjectXSymbolMapper.GetThirdFriday(2025, 5)),

            // ICE futures
            new TestCaseData("BRNM25", SecurityType.Future, Market.ICE,   "BRN", Market.ICE,   ProjectXSymbolMapper.GetThirdFriday(2025, 6)),
        };

        [Test, TestCaseSource(nameof(GetLeanSymbolCases))]
        public void GetLeanSymbol_ReturnsCorrectSymbol(
            string brokerageSymbol,
            SecurityType securityType,
            string callerMarket,
            string expectedRoot,
            string expectedMarket,
            DateTime expectedExpiry)
        {
            var result = _mapper.GetLeanSymbol(brokerageSymbol, securityType, callerMarket);

            Assert.That(result.ID.Symbol,        Is.EqualTo(expectedRoot));
            Assert.That(result.ID.Market,        Is.EqualTo(expectedMarket));
            Assert.That(result.ID.Date,          Is.EqualTo(expectedExpiry));
            Assert.That(result.SecurityType,     Is.EqualTo(SecurityType.Future));
        }

        [Test]
        public void GetLeanSymbol_UnknownRoot_UsesFallbackMarket()
        {
            var result = _mapper.GetLeanSymbol("XXH25", SecurityType.Future, Market.CBOT);

            Assert.That(result.ID.Symbol, Is.EqualTo("XX"));
            Assert.That(result.ID.Market, Is.EqualTo(Market.CBOT));
        }

        [Test]
        public void GetLeanSymbol_UnknownRootNoFallback_DefaultsToCme()
        {
            var result = _mapper.GetLeanSymbol("XXH25", SecurityType.Future, string.Empty);

            Assert.That(result.ID.Symbol, Is.EqualTo("XX"));
            Assert.That(result.ID.Market, Is.EqualTo(Market.CME));
        }

        [Test]
        public void GetLeanSymbol_NullTicker_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _mapper.GetLeanSymbol(null, SecurityType.Future, Market.CME));
        }

        [Test]
        public void GetLeanSymbol_EmptyTicker_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _mapper.GetLeanSymbol(string.Empty, SecurityType.Future, Market.CME));
        }

        [Test]
        public void GetLeanSymbol_TooShortTicker_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _mapper.GetLeanSymbol("EH5", SecurityType.Future, Market.CME));
        }

        [Test]
        public void GetLeanSymbol_InvalidMonthCode_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _mapper.GetLeanSymbol("ESA25", SecurityType.Future, Market.CME));
        }

        [Test]
        public void GetLeanSymbol_InvalidYearDigits_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _mapper.GetLeanSymbol("ESHab", SecurityType.Future, Market.CME));
        }

        [Test]
        public void GetLeanSymbol_NonFuturesSecurityType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _mapper.GetLeanSymbol("ESH25", SecurityType.Equity, Market.CME));
        }

        #endregion

        #region Round-trip

        private static readonly object[] RoundTripCases =
        {
            new TestCaseData("ESH25",  Market.CME),
            new TestCaseData("NQM25",  Market.CME),
            new TestCaseData("CLZ24",  Market.NYMEX),
            new TestCaseData("ZBU25",  Market.CBOT),
            new TestCaseData("GCJ25",  Market.NYMEX),
            new TestCaseData("BRNM25", Market.ICE),
            new TestCaseData("MESH25", Market.CME),
        };

        [Test, TestCaseSource(nameof(RoundTripCases))]
        public void RoundTrip_BrokerageToLeanToBrokerage_ReturnsOriginalTicker(string originalTicker, string market)
        {
            var leanSymbol = _mapper.GetLeanSymbol(originalTicker, SecurityType.Future, market);
            var result = _mapper.GetBrokerageSymbol(leanSymbol);
            Assert.That(result, Is.EqualTo(originalTicker));
        }

        [Test, TestCaseSource(nameof(RoundTripCases))]
        public void RoundTrip_LeanToBrokerageToBrokerageToLean_ReturnsSameSymbol(string originalTicker, string market)
        {
            var leanSymbol = _mapper.GetLeanSymbol(originalTicker, SecurityType.Future, market);
            var brokerageSymbol = _mapper.GetBrokerageSymbol(leanSymbol);
            var result = _mapper.GetLeanSymbol(brokerageSymbol, SecurityType.Future, market);

            Assert.That(result.ID.Symbol, Is.EqualTo(leanSymbol.ID.Symbol));
            Assert.That(result.ID.Market, Is.EqualTo(leanSymbol.ID.Market));
            Assert.That(result.ID.Date,   Is.EqualTo(leanSymbol.ID.Date));
        }

        #endregion

        #region GetThirdFriday

        private static readonly object[] ThirdFridayCases =
        {
            new TestCaseData(2025,  3, new DateTime(2025,  3, 21)),
            new TestCaseData(2025,  6, new DateTime(2025,  6, 20)),
            new TestCaseData(2025,  9, new DateTime(2025,  9, 19)),
            new TestCaseData(2025, 12, new DateTime(2025, 12, 19)),
            new TestCaseData(2024, 12, new DateTime(2024, 12, 20)),
            new TestCaseData(2025,  1, new DateTime(2025,  1, 17)),
        };

        [Test, TestCaseSource(nameof(ThirdFridayCases))]
        public void GetThirdFriday_ReturnsCorrectDate(int year, int month, DateTime expected)
        {
            var result = ProjectXSymbolMapper.GetThirdFriday(year, month);
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(result.DayOfWeek, Is.EqualTo(DayOfWeek.Friday));
        }

        #endregion
    }
}
