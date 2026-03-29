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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Logging;
using QuantConnect.Configuration;

namespace QuantConnect.Brokerages.ProjectX.Tests
{
    [TestFixture, Explicit("These tests require valid sandbox credentials.")]
    public class ProjectXBrokerageSandboxOrderTests
    {
        private ProjectXBrokerage _brokerage;
        private readonly ManualResetEventSlim _orderStatusChangedEvent = new ManualResetEventSlim(false);

        [SetUp]
        public void Setup()
        {
            Log.LogHandler = new ConsoleLogHandler();

            var apiKey = Config.Get("brokerage-project-x-sandbox-api-key");
            var apiSecret = Config.Get("brokerage-project-x-sandbox-api-secret");
            var accountId = Config.GetInt("brokerage-project-x-sandbox-account-id");

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret) || accountId == 0)
            {
                Assert.Inconclusive("Sandbox credentials are not set in config.json. Required fields: brokerage-project-x-sandbox-api-key, brokerage-project-x-sandbox-api-secret, brokerage-project-x-sandbox-account-id.");
            }

            _brokerage = new ProjectXBrokerage(apiKey, apiSecret, "sandbox", accountId, new TestDataAggregator());
            _brokerage.Connect();

            _brokerage.OrderStatusChanged += (sender, args) =>
            {
                Log.Trace($"OrderStatusChanged event received: {args}");
                _orderStatusChangedEvent.Set();
            };
        }

        [TearDown]
        public void Teardown()
        {
            _brokerage?.Disconnect();
            _brokerage?.Dispose();
        }

        [Test]
        public void PlacesAndFillsMarketOrder()
        {
            var symbol = Symbol.CreateFuture("ES", Market.CME, new DateTime(2024, 6, 21));
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

            _orderStatusChangedEvent.Reset();
            
            var response = _brokerage.PlaceOrder(order);
            Assert.IsTrue(response.IsSuccess);

            var orderStatusReceived = _orderStatusChangedEvent.Wait(TimeSpan.FromSeconds(30));
            Assert.IsTrue(orderStatusReceived, "Timed out waiting for order status event.");

            var cachedOrder = _brokerage.GetOrderById(order.Id);
            Assert.IsNotNull(cachedOrder);
            Assert.AreEqual(OrderStatus.Filled, cachedOrder.Status);
        }

        [Test]
        public void PlacesAndCancelsLimitOrder()
        {
            var symbol = Symbol.CreateFuture("ES", Market.CME, new DateTime(2024, 6, 21));
            var order = new LimitOrder(symbol, 1, 1m, DateTime.UtcNow);

            _orderStatusChangedEvent.Reset();

            var placeResponse = _brokerage.PlaceOrder(order);
            Assert.IsTrue(placeResponse.IsSuccess);

            // Wait for the 'Submitted' event
            var submittedEventReceived = _orderStatusChangedEvent.Wait(TimeSpan.FromSeconds(15));
            Assert.IsTrue(submittedEventReceived, "Timed out waiting for order submission event.");
            
            var submittedOrder = _brokerage.GetOrderById(order.Id);
            Assert.IsNotNull(submittedOrder);
            Assert.AreEqual(OrderStatus.Submitted, submittedOrder.Status);

            _orderStatusChangedEvent.Reset();

            var cancelResponse = _brokerage.CancelOrder(order);
            Assert.IsTrue(cancelResponse);

            // Wait for the 'Canceled' event
            var canceledEventReceived = _orderStatusChangedEvent.Wait(TimeSpan.FromSeconds(15));
            Assert.IsTrue(canceledEventReceived, "Timed out waiting for order cancellation event.");

            var canceledOrder = _brokerage.GetOrderById(order.Id);
            Assert.IsNotNull(canceledOrder);
            Assert.AreEqual(OrderStatus.Canceled, canceledOrder.Status);
        }

        [Test]
        public void UpdatesLimitOrderPrice()
        {
            var symbol = Symbol.CreateFuture("ES", Market.CME, new DateTime(2024, 6, 21));
            var order = new LimitOrder(symbol, 1, 1m, DateTime.UtcNow);

            _orderStatusChangedEvent.Reset();

            var placeResponse = _brokerage.PlaceOrder(order);
            Assert.IsTrue(placeResponse.IsSuccess);

            // Wait for the 'Submitted' event
            var submittedEventReceived = _orderStatusChangedEvent.Wait(TimeSpan.FromSeconds(15));
            Assert.IsTrue(submittedEventReceived, "Timed out waiting for order submission event.");

            var submittedOrder = _brokerage.GetOrderById(order.Id);
            Assert.IsNotNull(submittedOrder);
            Assert.AreEqual(OrderStatus.Submitted, submittedOrder.Status);
            
            _orderStatusChangedEvent.Reset();

            var updateRequest = new UpdateOrderRequest(DateTime.UtcNow)
            {
                OrderId = order.Id,
                LimitPrice = 2m
            };

            var updateResponse = _brokerage.UpdateOrder(updateRequest);
            Assert.IsTrue(updateResponse.IsSuccess);

            // Wait for the 'Update' event
            var updatedEventReceived = _orderStatusChangedEvent.Wait(TimeSpan.FromSeconds(15));
            Assert.IsTrue(updatedEventReceived, "Timed out waiting for order update event.");
            
            var updatedOrder = _brokerage.GetOrderById(order.Id);
            Assert.IsNotNull(updatedOrder);
            Assert.AreEqual(2m, updatedOrder.Price);
            Assert.AreEqual(OrderStatus.Submitted, updatedOrder.Status);

            // Cancel the order to clean up
            _brokerage.CancelOrder(updatedOrder);
        }

        [Test]
        public void RejectsInvalidOrder()
        {
            var symbol = Symbol.CreateFuture("ES", Market.CME, new DateTime(2024, 6, 21));
            var order = new MarketOrder(symbol, 0, DateTime.UtcNow);

            var response = _brokerage.PlaceOrder(order);

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(OrderResponseErrorCode.InvalidOrderQuantity, response.ErrorCode);
        }
    }
}
