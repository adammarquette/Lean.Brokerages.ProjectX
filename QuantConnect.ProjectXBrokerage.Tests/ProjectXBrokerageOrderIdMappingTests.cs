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
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    /// <summary>
    /// Unit tests for bidirectional order ID mapping and duplicate-submission detection.
    /// No API credentials or network access required.
    /// </summary>
    [TestFixture]
    public class ProjectXBrokerageOrderIdMappingTests
    {
        private ProjectXBrokerage _brokerage;

        [SetUp]
        public void SetUp()
        {
            Config.Set("brokerage-project-x-api-key", "unit-test-key");
            Config.Set("brokerage-project-x-api-secret", "unit-test-secret");
            Config.Set("brokerage-project-x-environment", "sandbox");
            _brokerage = new ProjectXBrokerage(new TestDataAggregator());
        }

        [TearDown]
        public void TearDown()
        {
            Config.Reset();
            _brokerage?.Dispose();
        }

        // ── Reflection helpers ──────────────────────────────────────────────────

        private void InvokeAddMapping(int leanId, long projectXId) =>
            typeof(ProjectXBrokerage)
                .GetMethod("AddOrderIdMapping", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_brokerage, new object[] { leanId, projectXId });

        private void InvokeRemoveMapping(int leanId) =>
            typeof(ProjectXBrokerage)
                .GetMethod("RemoveOrderIdMapping", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_brokerage, new object[] { leanId });

        private bool InvokeIsDuplicateSubmission(Order order) =>
            (bool)typeof(ProjectXBrokerage)
                .GetMethod("IsDuplicateSubmission", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_brokerage, new object[] { order });

        private ConcurrentDictionary<int, long> GetLeanToProjectX() =>
            (ConcurrentDictionary<int, long>)typeof(ProjectXBrokerage)
                .GetField("_leanToProjectXOrderIds", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(_brokerage);

        private ConcurrentDictionary<long, int> GetProjectXToLean() =>
            (ConcurrentDictionary<long, int>)typeof(ProjectXBrokerage)
                .GetField("_projectXToLeanOrderIds", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(_brokerage);

        private ConcurrentDictionary<int, DateTime> GetRecentlySubmitted() =>
            (ConcurrentDictionary<int, DateTime>)typeof(ProjectXBrokerage)
                .GetField("_recentlySubmittedOrders", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(_brokerage);

        private static MarketOrder CreateTestOrder(int id)
        {
            var symbol = Symbol.CreateFuture("ES", Market.CME, new DateTime(2025, 3, 21));
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);
            typeof(Order).GetProperty("Id").SetValue(order, id);
            return order;
        }

        // ── Bidirectional mapping ────────────────────────────────────────────────

        [Test]
        public void AddOrderIdMapping_StoresBidirectionalMapping()
        {
            InvokeAddMapping(100, 999L);

            Assert.IsTrue(GetLeanToProjectX().TryGetValue(100, out var pxId));
            Assert.AreEqual(999L, pxId);
            Assert.IsTrue(GetProjectXToLean().TryGetValue(999L, out var leanId));
            Assert.AreEqual(100, leanId);
        }

        [Test]
        public void AddOrderIdMapping_MultipleDistinctMappings_AllStored()
        {
            InvokeAddMapping(1, 10L);
            InvokeAddMapping(2, 20L);
            InvokeAddMapping(3, 30L);

            var d = GetLeanToProjectX();
            Assert.AreEqual(3, d.Count);
            Assert.AreEqual(10L, d[1]);
            Assert.AreEqual(20L, d[2]);
            Assert.AreEqual(30L, d[3]);
        }

        [Test]
        public void AddOrderIdMapping_OverwritesExistingLeanId()
        {
            InvokeAddMapping(100, 999L);
            InvokeAddMapping(100, 888L);

            Assert.IsTrue(GetLeanToProjectX().TryGetValue(100, out var pxId));
            Assert.AreEqual(888L, pxId);
        }

        [Test]
        public void RemoveOrderIdMapping_RemovesBothDirections()
        {
            InvokeAddMapping(100, 999L);
            InvokeRemoveMapping(100);

            Assert.IsFalse(GetLeanToProjectX().ContainsKey(100));
            Assert.IsFalse(GetProjectXToLean().ContainsKey(999L));
        }

        [Test]
        public void RemoveOrderIdMapping_NonexistentMapping_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => InvokeRemoveMapping(9999));
        }

        [Test]
        public void RemoveOrderIdMapping_LeavesOtherMappingsIntact()
        {
            InvokeAddMapping(1, 10L);
            InvokeAddMapping(2, 20L);

            InvokeRemoveMapping(1);

            Assert.IsFalse(GetLeanToProjectX().ContainsKey(1));
            Assert.IsTrue(GetLeanToProjectX().ContainsKey(2));
            Assert.IsTrue(GetProjectXToLean().ContainsKey(20L));
        }

        [Test]
        public void OrderIdMapping_ThreadSafe_ConcurrentAddRemove()
        {
            var exceptions = 0;
            var threads = new Thread[10];

            for (int i = 0; i < threads.Length; i++)
            {
                var idx = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            var leanId = idx * 100 + j;
                            InvokeAddMapping(leanId, (long)leanId * 10);
                            InvokeRemoveMapping(leanId);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref exceptions);
                    }
                });
                threads[i].Start();
            }

            foreach (var t in threads) t.Join();

            Assert.AreEqual(0, exceptions, "Concurrent mapping operations should not throw");
        }

        // ── Duplicate submission detection ───────────────────────────────────────

        [Test]
        public void IsDuplicateSubmission_RecentEntry_ReturnsTrue()
        {
            var order = CreateTestOrder(42);
            GetRecentlySubmitted().TryAdd(order.Id, DateTime.UtcNow);

            Assert.IsTrue(InvokeIsDuplicateSubmission(order));
        }

        [Test]
        public void IsDuplicateSubmission_ExpiredEntry_ReturnsFalse_AndRemovesEntry()
        {
            var order = CreateTestOrder(42);
            // Timestamp outside the 5-second window
            GetRecentlySubmitted().TryAdd(order.Id, DateTime.UtcNow - TimeSpan.FromSeconds(10));

            Assert.IsFalse(InvokeIsDuplicateSubmission(order));
            Assert.IsFalse(GetRecentlySubmitted().ContainsKey(order.Id), "Expired entry should be removed");
        }

        [Test]
        public void IsDuplicateSubmission_NewOrder_ReturnsFalse()
        {
            var order = CreateTestOrder(99);

            Assert.IsFalse(InvokeIsDuplicateSubmission(order));
        }

        [Test]
        public void IsDuplicateSubmission_DifferentOrders_OnlyDuplicateBlocked()
        {
            var order1 = CreateTestOrder(1);
            var order2 = CreateTestOrder(2);
            GetRecentlySubmitted().TryAdd(order1.Id, DateTime.UtcNow);

            Assert.IsTrue(InvokeIsDuplicateSubmission(order1));
            Assert.IsFalse(InvokeIsDuplicateSubmission(order2));
        }

        [Test]
        public void IsDuplicateSubmission_CleansUpExpiredEntries_WhenCountExceedsThreshold()
        {
            var submitted = GetRecentlySubmitted();
            var oldTimestamp = DateTime.UtcNow - TimeSpan.FromSeconds(10);

            for (int i = 0; i < 101; i++)
                submitted.TryAdd(i, oldTimestamp);

            Assert.AreEqual(101, submitted.Count);

            // Any call when count > 100 triggers cleanup of expired entries
            InvokeIsDuplicateSubmission(CreateTestOrder(9999));

            Assert.Less(submitted.Count, 101, "Expired entries should have been removed");
        }
    }
}
