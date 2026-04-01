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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.WebSocket;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ProjectXBrokerage"/> as <see cref="IDataQueueUniverseProvider"/>.
    /// All tests use mocked API and WebSocket clients — no network access required.
    /// </summary>
    [TestFixture]
    public class ProjectXBrokerageDataQueueUniverseProviderTests
    {
        private Mock<IProjectXApiClient>    _apiClientMock;
        private Mock<IProjectXWebSocketClient> _wsClientMock;
        private Mock<IOrderProvider>        _orderProviderMock;
        private Mock<ISecurityProvider>     _securityProviderMock;
        private ProjectXBrokerage          _brokerage;

        private static readonly Symbol CanonicalES =
            Symbol.Create("ES", SecurityType.Future, Market.CME);

        [SetUp]
        public void SetUp()
        {
            Config.Set("brokerage-project-x-api-key", "unit-test-key");
            Config.Set("brokerage-project-x-api-secret", "unit-test-secret");
            Config.Set("brokerage-project-x-environment", "sandbox");

            _apiClientMock        = new Mock<IProjectXApiClient>();
            _wsClientMock         = new Mock<IProjectXWebSocketClient>();
            _orderProviderMock    = new Mock<IOrderProvider>();
            _securityProviderMock = new Mock<ISecurityProvider>();

            _brokerage = new ProjectXBrokerage(
                _apiClientMock.Object,
                _wsClientMock.Object,
                new ProjectXSymbolMapper(),
                _orderProviderMock.Object,
                _securityProviderMock.Object,
                new TestDataAggregator()
            );

            // Set the brokerage as connected via reflection
            SetConnected(true);
        }

        [TearDown]
        public void TearDown()
        {
            Config.Reset();
            _brokerage?.Dispose();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private void SetConnected(bool connected) =>
            typeof(ProjectXBrokerage)
                .GetField("_isConnected", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_brokerage, connected);

        private static Contract MakeContract(string id, bool active = true) =>
            new Contract { Id = id, Name = id, ActiveContract = active, SymbolId = id };

        // ── CanPerformSelection ──────────────────────────────────────────────────

        [Test]
        public void CanPerformSelection_WhenConnected_ReturnsTrue()
        {
            SetConnected(true);
            Assert.IsTrue(_brokerage.CanPerformSelection());
        }

        [Test]
        public void CanPerformSelection_WhenDisconnected_ReturnsFalse()
        {
            SetConnected(false);
            Assert.IsFalse(_brokerage.CanPerformSelection());
        }

        // ── LookupSymbols ────────────────────────────────────────────────────────

        [Test]
        public void LookupSymbols_ReturnsActiveContracts_WhenIncludeExpiredFalse()
        {
            // Arrange
            _apiClientMock
                .Setup(c => c.SearchContractsAsync("ES", /*live=*/true, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<Contract>)new[]
                {
                    MakeContract("ESH25"),
                    MakeContract("ESM25"),
                    MakeContract("ESU25"),
                });

            // Act
            var symbols = _brokerage.LookupSymbols(CanonicalES, includeExpired: false).ToList();

            // Assert
            Assert.AreEqual(3, symbols.Count, "Should return one symbol per active contract");
            Assert.That(symbols, Is.All.Not.Null);
            Assert.IsTrue(symbols.All(s => s.SecurityType == SecurityType.Future),
                "All returned symbols should be futures");
        }

        [Test]
        public void LookupSymbols_IncludesExpiredContracts_WhenIncludeExpiredTrue()
        {
            // Arrange
            _apiClientMock
                .Setup(c => c.SearchContractsAsync("ES", /*live=*/false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<Contract>)new[]
                {
                    MakeContract("ESH25"),
                    MakeContract("ESH24", active: false),   // expired
                    MakeContract("ESH23", active: false),   // expired
                });

            // Act
            var symbols = _brokerage.LookupSymbols(CanonicalES, includeExpired: true).ToList();

            // Assert
            Assert.AreEqual(3, symbols.Count,
                "Should include expired contracts when includeExpired=true");
        }

        [Test]
        public void LookupSymbols_WhenNotConnected_ReturnsEmpty()
        {
            // Arrange
            SetConnected(false);

            // Act
            var symbols = _brokerage.LookupSymbols(CanonicalES, includeExpired: false);

            // Assert
            Assert.IsEmpty(symbols, "Should return empty when not connected");
            _apiClientMock.Verify(
                c => c.SearchContractsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "API should not be called when not connected");
        }

        [Test]
        public void LookupSymbols_ApiThrows_ReturnsEmpty()
        {
            // Arrange
            _apiClientMock
                .Setup(c => c.SearchContractsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("API unreachable"));

            // Act
            var symbols = _brokerage.LookupSymbols(CanonicalES, includeExpired: false);

            // Assert
            Assert.IsEmpty(symbols, "Should return empty collection when API throws");
        }

        [Test]
        public void LookupSymbols_ApiReturnsEmpty_ReturnsEmpty()
        {
            // Arrange
            _apiClientMock
                .Setup(c => c.SearchContractsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<Contract>)Array.Empty<Contract>());

            // Act
            var symbols = _brokerage.LookupSymbols(CanonicalES, includeExpired: false);

            // Assert
            Assert.IsEmpty(symbols, "Should return empty when API returns no contracts");
        }

        [Test]
        public void LookupSymbols_ContractIdBadFormat_SkipsInvalidAndContinues()
        {
            // Arrange — mix of valid and unparseable contract IDs
            _apiClientMock
                .Setup(c => c.SearchContractsAsync("ES", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<Contract>)new[]
                {
                    MakeContract("ESH25"),          // valid
                    MakeContract("INVALID_FORMAT"), // unmappable — should be skipped
                    MakeContract("ESM25"),          // valid
                });

            // Act
            var symbols = _brokerage.LookupSymbols(CanonicalES, includeExpired: false).ToList();

            // Assert — invalid entry skipped, two valid ones remain
            Assert.AreEqual(2, symbols.Count,
                "Unmappable contracts should be skipped without throwing");
        }

        [Test]
        public void LookupSymbols_PassesCorrectRoot_ToApiClient()
        {
            // Arrange
            var nqCanonical = Symbol.Create("NQ", SecurityType.Future, Market.CME);
            _apiClientMock
                .Setup(c => c.SearchContractsAsync("NQ", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<Contract>)new[] { MakeContract("NQH25") });

            // Act
            _brokerage.LookupSymbols(nqCanonical, includeExpired: false);

            // Assert — root "NQ" passed to API, not ticker "NQH25" or full symbol string
            _apiClientMock.Verify(
                c => c.SearchContractsAsync("NQ", true, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
