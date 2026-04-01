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
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.WebSocket;
using PlaceOrderRequest = MarqSpec.Client.ProjectX.Api.Models.PlaceOrderRequest;
using PlaceOrderResponse = MarqSpec.Client.ProjectX.Api.Models.PlaceOrderResponse;
using PxOrderUpdate = MarqSpec.Client.ProjectX.Api.Models.OrderUpdate;
using PxOrderStatus = MarqSpec.Client.ProjectX.Api.Models.OrderStatus;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    [TestFixture]
    public class ProjectXBrokerageOrderManagementTests
    {
        private Mock<IProjectXApiClient> _apiClientMock;
        private Mock<IProjectXWebSocketClient> _wsClientMock;
        private Mock<IOrderProvider> _orderProviderMock;
        private Mock<ISecurityProvider> _securityProviderMock;
        private ProjectXBrokerage _brokerage;
        private Symbol _testSymbol;

        [SetUp]
        public void SetUp()
        {
            _apiClientMock = new Mock<IProjectXApiClient>();
            _wsClientMock = new Mock<IProjectXWebSocketClient>();
            _orderProviderMock = new Mock<IOrderProvider>();
            _securityProviderMock = new Mock<ISecurityProvider>();
            var symbolMapper = new ProjectXSymbolMapper();
            var aggregator = new TestDataAggregator();

            _brokerage = new ProjectXBrokerage(
                _apiClientMock.Object,
                _wsClientMock.Object,
                symbolMapper,
                _orderProviderMock.Object,
                _securityProviderMock.Object,
                aggregator
            );

            _testSymbol = Symbol.CreateFuture("ESH25", Market.CME, new DateTime(2025, 3, 21));
        }

        [Test]
        [Ignore("CustomTag race condition handling was removed — OrderUpdate in library v1.0.2 does not have CustomTag. " +
                "This test must be rewritten once an alternative correlation mechanism is implemented.")]
        public void OnOrderUpdateReceived_HandlesEventBeforePlaceOrderCompletes()
        {
            Assert.Ignore("CustomTag property does not exist on OrderUpdate in library v1.0.2.");
        }
    }
}
