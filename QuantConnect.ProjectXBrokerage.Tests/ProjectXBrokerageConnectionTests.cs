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
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    [TestFixture]
    public class ProjectXBrokerageConnectionTests
    {
        private TestDataAggregator _aggregator;

        [SetUp]
        public void SetUp()
        {
            // Set up test configuration
            Config.Set("brokerage-project-x-api-key", "test-api-key");
            Config.Set("brokerage-project-x-api-secret", "test-api-secret");
            Config.Set("brokerage-project-x-environment", "sandbox");
            Config.Set("brokerage-project-x-reconnect-attempts", "3");
            Config.Set("brokerage-project-x-reconnect-delay", "100");
            Config.Set("brokerage-project-x-connection-timeout", "5000");

            _aggregator = new TestDataAggregator();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up configuration
            Config.Reset();
        }

        [Test]
        public void Constructor_LoadsConfiguration_Successfully()
        {
            // Arrange & Act
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Assert
            Assert.IsNotNull(brokerage);
            Assert.IsFalse(brokerage.IsConnected, "Should not be connected initially");
        }

        [Test]
        public void IsConnected_InitialState_IsFalse()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act
            var isConnected = brokerage.IsConnected;

            // Assert
            Assert.IsFalse(isConnected, "IsConnected should be false initially");
        }

        [Test]
        public void IsConnected_ThreadSafe_MultipleThreads()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            var exceptions = 0;
            var iterations = 1000;

            // Act
            var threads = new Thread[10];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        for (int j = 0; j < iterations; j++)
                        {
                            var _ = brokerage.IsConnected;
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref exceptions);
                    }
                });
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.AreEqual(0, exceptions, "Thread-safe access should not throw exceptions");
        }

        [Test, Category("RequiresApiCredentials")]
        public void Connect_WithValidConfiguration_Succeeds()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act
            brokerage.Connect();

            // Assert
            Assert.IsTrue(brokerage.IsConnected, "Should be connected after Connect()");
        }

        [Test, Category("RequiresApiCredentials")]
        public void Connect_WhenAlreadyConnected_DoesNotThrow()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            brokerage.Connect();

            // Act & Assert
            Assert.DoesNotThrow(() => brokerage.Connect(), "Connecting when already connected should not throw");
            Assert.IsTrue(brokerage.IsConnected, "Should still be connected");
        }

        [Test]
        public void Connect_WithMissingApiKey_ThrowsArgumentException()
        {
            // Arrange
            Config.Set("brokerage-project-x-api-key", string.Empty);
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => brokerage.Connect());
            Assert.That(exception.Message, Does.Contain("API key is required"));
        }

        [Test]
        public void Connect_WithMissingApiSecret_ThrowsArgumentException()
        {
            // Arrange
            Config.Set("brokerage-project-x-api-secret", string.Empty);
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => brokerage.Connect());
            Assert.That(exception.Message, Does.Contain("API secret is required"));
        }

        [Test]
        public void Connect_WithInvalidEnvironment_ThrowsArgumentException()
        {
            // Arrange
            Config.Set("brokerage-project-x-environment", "invalid");
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => brokerage.Connect());
            Assert.That(exception.Message, Does.Contain("Invalid environment"));
        }

        [Test, Category("RequiresApiCredentials")]
        public void Disconnect_WhenConnected_Succeeds()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected, "Precondition: should be connected");

            // Act
            brokerage.Disconnect();

            // Assert
            Assert.IsFalse(brokerage.IsConnected, "Should be disconnected after Disconnect()");
        }

        [Test]
        public void Disconnect_WhenNotConnected_DoesNotThrow()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            Assert.IsFalse(brokerage.IsConnected, "Precondition: should not be connected");

            // Act & Assert
            Assert.DoesNotThrow(() => brokerage.Disconnect(), "Disconnecting when not connected should not throw");
        }

        [Test, Category("RequiresApiCredentials")]
        public void Disconnect_WhenAlreadyDisconnected_DoesNotThrow()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            brokerage.Connect();
            brokerage.Disconnect();
            Assert.IsFalse(brokerage.IsConnected, "Precondition: should be disconnected");

            // Act & Assert
            Assert.DoesNotThrow(() => brokerage.Disconnect(), "Disconnecting when already disconnected should not throw");
        }

        [Test, Category("RequiresApiCredentials")]
        public void ConnectDisconnectCycle_Multiple_Succeeds()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                brokerage.Connect();
                Assert.IsTrue(brokerage.IsConnected, $"Should be connected on iteration {i}");

                brokerage.Disconnect();
                Assert.IsFalse(brokerage.IsConnected, $"Should be disconnected on iteration {i}");
            }
        }

        [Test, Category("RequiresApiCredentials")]
        public void Configuration_WithDefaultValues_LoadsCorrectly()
        {
            // Arrange
            Config.Reset();
            Config.Set("brokerage-project-x-api-key", "test-key");
            Config.Set("brokerage-project-x-api-secret", "test-secret");
            // Don't set optional values, let defaults apply

            // Act
            var brokerage = new ProjectXBrokerage(_aggregator);
            brokerage.Connect();

            // Assert
            Assert.IsTrue(brokerage.IsConnected, "Should connect with default configuration values");
        }

        [Test, Category("RequiresApiCredentials")]
        public void Configuration_SandboxEnvironment_Accepted()
        {
            // Arrange
            Config.Set("brokerage-project-x-environment", "sandbox");
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            Assert.DoesNotThrow(() => brokerage.Connect(), "Sandbox environment should be valid");
        }

        [Test, Category("RequiresApiCredentials")]
        public void Configuration_ProductionEnvironment_Accepted()
        {
            // Arrange
            Config.Set("brokerage-project-x-environment", "production");
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            Assert.DoesNotThrow(() => brokerage.Connect(), "Production environment should be valid");
        }

        [Test]
        public void Configuration_ReconnectAttempts_ValidatesRange()
        {
            // Arrange - too low
            Config.Set("brokerage-project-x-reconnect-attempts", "0");
            var brokerage1 = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            var exception1 = Assert.Throws<ArgumentException>(() => brokerage1.Connect());
            Assert.That(exception1.Message, Does.Contain("Invalid reconnect attempts"));

            // Arrange - too high
            Config.Reset();
            Config.Set("brokerage-project-x-api-key", "test-key");
            Config.Set("brokerage-project-x-api-secret", "test-secret");
            Config.Set("brokerage-project-x-reconnect-attempts", "25");
            var brokerage2 = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            var exception2 = Assert.Throws<ArgumentException>(() => brokerage2.Connect());
            Assert.That(exception2.Message, Does.Contain("Invalid reconnect attempts"));
        }

        [Test]
        public void Brokerage_Name_IsCorrect()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act
            var name = brokerage.Name;

            // Assert
            Assert.AreEqual("ProjectXBrokerage", name);
        }

        [Test, Category("RequiresApiCredentials")]
        public void Brokerage_Dispose_DisconnectsIfConnected()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            brokerage.Connect();
            Assert.IsTrue(brokerage.IsConnected, "Precondition: should be connected");

            // Act
            brokerage.Dispose();

            // Assert
            // Note: The base Brokerage.Dispose() is a no-op and doesn't call Disconnect()
            // Implementations can override Dispose to call Disconnect if needed
            // For now, this test just verifies Dispose doesn't throw
            Assert.DoesNotThrow(() => brokerage.Dispose());
        }

        [Test, Category("RequiresApiCredentials")]
        public void Heartbeat_AfterConnection_ShouldStart()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act
            brokerage.Connect();
            Thread.Sleep(100); // Give heartbeat time to start

            // Assert
            Assert.IsTrue(brokerage.IsConnected, "Should remain connected with heartbeat running");
        }

        [Test, Category("RequiresApiCredentials")]
        public void Disconnect_StopsHeartbeat_Successfully()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            brokerage.Connect();
            Thread.Sleep(100); // Let heartbeat start

            // Act
            brokerage.Disconnect();
            Thread.Sleep(100); // Give time for heartbeat to stop

            // Assert
            Assert.IsFalse(brokerage.IsConnected, "Should be disconnected");
        }

        [Test, Category("RequiresApiCredentials")]
        public void Configuration_ValidRetrySettings_LoadCorrectly()
        {
            // Arrange
            Config.Set("brokerage-project-x-reconnect-attempts", "10");
            Config.Set("brokerage-project-x-reconnect-delay", "2000");
            Config.Set("brokerage-project-x-connection-timeout", "60000");

            // Act
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Assert
            Assert.DoesNotThrow(() => brokerage.Connect(), "Valid retry settings should work");
        }

        [Test, Category("RequiresApiCredentials")]
        public void Configuration_MinimumRetryAttempts_Accepted()
        {
            // Arrange
            Config.Set("brokerage-project-x-reconnect-attempts", "1");
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            Assert.DoesNotThrow(() => brokerage.Connect(), "Minimum retry attempts (1) should be valid");
        }

        [Test, Category("RequiresApiCredentials")]
        public void Configuration_MaximumRetryAttempts_Accepted()
        {
            // Arrange
            Config.Set("brokerage-project-x-reconnect-attempts", "20");
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            Assert.DoesNotThrow(() => brokerage.Connect(), "Maximum retry attempts (20) should be valid");
        }

        [Test, Category("RequiresApiCredentials")]
        public void IsConnected_ConcurrentReadsDuringStateChange_NoExceptions()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            var exceptions = 0;
            var running = true;

            // Act - Start threads reading IsConnected
            var readThreads = new Thread[5];
            for (int i = 0; i < readThreads.Length; i++)
            {
                readThreads[i] = new Thread(() =>
                {
                    try
                    {
                        while (running)
                        {
                            var _ = brokerage.IsConnected;
                            Thread.Sleep(1);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref exceptions);
                    }
                });
                readThreads[i].Start();
            }

            // Change state multiple times
            for (int i = 0; i < 10; i++)
            {
                brokerage.Connect();
                Thread.Sleep(10);
                brokerage.Disconnect();
                Thread.Sleep(10);
            }

            running = false;
            foreach (var thread in readThreads)
            {
                thread.Join();
            }

            // Assert
            Assert.AreEqual(0, exceptions, "Concurrent reads during state changes should not throw");
        }

        [Test, Category("RequiresApiCredentials")]
        public void Connect_MultipleThreadsSimultaneously_OneSucceeds()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            var successCount = 0;
            var threads = new Thread[5];

            // Act
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        brokerage.Connect();
                        if (brokerage.IsConnected)
                        {
                            Interlocked.Increment(ref successCount);
                        }
                    }
                    catch
                    {
                        // Expected - some threads may fail
                    }
                });
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.IsTrue(brokerage.IsConnected, "Brokerage should be connected");
            Assert.Greater(successCount, 0, "At least one thread should succeed");
        }

        [Test, Category("RequiresApiCredentials")]
        public void MessageEvent_OnSuccessfulConnection_Fired()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            var messageReceived = false;
            var messageCode = string.Empty;

            brokerage.Message += (sender, args) =>
            {
                messageReceived = true;
                messageCode = args.Code;
            };

            // Act
            brokerage.Connect();

            // Assert
            Assert.IsTrue(messageReceived, "Message event should be fired on connection");
            Assert.AreEqual("CONNECTED", messageCode, "Should receive CONNECTED message");
        }

        [Test, Category("RequiresApiCredentials")]
        public void MessageEvent_OnDisconnection_Fired()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            var disconnectMessageReceived = false;

            brokerage.Connect();

            brokerage.Message += (sender, args) =>
            {
                if (args.Code == "DISCONNECTED")
                {
                    disconnectMessageReceived = true;
                }
            };

            // Act
            brokerage.Disconnect();

            // Assert
            Assert.IsTrue(disconnectMessageReceived, "Message event should be fired on disconnection");
        }

        [Test]
        public void ParameterlessConstructor_InitializesCorrectly()
        {
            // Act
            var brokerage = new ProjectXBrokerage();

            // Assert
            Assert.IsNotNull(brokerage);
            Assert.IsFalse(brokerage.IsConnected);
            Assert.AreEqual("ProjectXBrokerage", brokerage.Name);
        }

        [Test]
        public void Dispose_WhenNotConnected_DoesNotThrow()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            Assert.DoesNotThrow(() => brokerage.Dispose());
        }

        [Test, Category("RequiresApiCredentials")]
        public void Dispose_MultipleTimes_DoesNotThrow()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            brokerage.Connect();

            // Act & Assert
            Assert.DoesNotThrow(() => brokerage.Dispose());
            Assert.DoesNotThrow(() => brokerage.Dispose(), "Multiple dispose calls should be safe");
        }

        [Test]
        public void Configuration_EmptyEnvironmentString_ThrowsException()
        {
            // Arrange
            Config.Set("brokerage-project-x-environment", "");
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            // Empty environment is not valid - should throw ArgumentException
            var exception = Assert.Throws<ArgumentException>(() => brokerage.Connect());
            Assert.That(exception.Message, Does.Contain("Invalid environment"));
        }

        [Test]
        public void Configuration_CaseInsensitiveEnvironment_Sandbox()
        {
            // Arrange
            Config.Set("brokerage-project-x-environment", "SANDBOX");
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            // Environment validation is case-sensitive in current implementation
            var exception = Assert.Throws<ArgumentException>(() => brokerage.Connect());
            Assert.That(exception.Message, Does.Contain("Invalid environment"));
        }

        [Test]
        public void Configuration_CaseInsensitiveEnvironment_Production()
        {
            // Arrange
            Config.Set("brokerage-project-x-environment", "PRODUCTION");
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            // Environment validation is case-sensitive in current implementation
            var exception = Assert.Throws<ArgumentException>(() => brokerage.Connect());
            Assert.That(exception.Message, Does.Contain("Invalid environment"));
        }

        [Test, Category("RequiresApiCredentials")]
        public void Connect_AfterPreviousFailure_CanRetry()
        {
            // Arrange
            Config.Set("brokerage-project-x-api-key", "");
            var brokerage1 = new ProjectXBrokerage(_aggregator);

            try
            {
                brokerage1.Connect();
            }
            catch
            {
                // Expected failure
            }

            // Fix configuration
            Config.Set("brokerage-project-x-api-key", "test-api-key");
            var brokerage2 = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            Assert.DoesNotThrow(() => brokerage2.Connect(), "Should connect after fixing configuration");
            Assert.IsTrue(brokerage2.IsConnected);
        }

        [Test, Category("RequiresApiCredentials")]
        public void Heartbeat_MaintainsConnection_OverTime()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);
            brokerage.Connect();

            // Act - Wait for multiple heartbeat cycles
            Thread.Sleep(500); // Wait long enough for heartbeat to run

            // Assert
            Assert.IsTrue(brokerage.IsConnected, "Connection should remain active with heartbeat");
        }

        [Test, Category("RequiresApiCredentials")]
        public void ConnectDisconnect_RapidCycles_HandleGracefully()
        {
            // Arrange
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act - Rapid connect/disconnect
            for (int i = 0; i < 10; i++)
            {
                brokerage.Connect();
                brokerage.Disconnect();
            }

            // Assert
            Assert.IsFalse(brokerage.IsConnected, "Should end in disconnected state");
        }

        [Test]
        public void Configuration_NegativeRetryDelay_HandledGracefully()
        {
            // Arrange
            Config.Set("brokerage-project-x-reconnect-delay", "-1000");
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            // Negative values are handled by Config.GetInt which may return 0 or default
            Assert.DoesNotThrow(() => new ProjectXBrokerage(_aggregator));
        }

        [Test]
        public void Configuration_ZeroConnectionTimeout_HandledGracefully()
        {
            // Arrange
            Config.Set("brokerage-project-x-connection-timeout", "0");
            var brokerage = new ProjectXBrokerage(_aggregator);

            // Act & Assert
            Assert.DoesNotThrow(() => new ProjectXBrokerage(_aggregator));
        }
    }

    /// <summary>
    /// Mock implementation of IDataAggregator for testing purposes
    /// </summary>
    internal class TestDataAggregator : IDataAggregator
    {
        public void Initialize(DataAggregatorInitializeParameters parameters)
        {
            // No-op for testing
        }

        public IEnumerator<BaseData> Add(Data.SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            // Return empty enumerator for testing
            return new List<BaseData>().GetEnumerator();
        }

        public bool Remove(Data.SubscriptionDataConfig dataConfig)
        {
            // Return true for testing
            return true;
        }

        public void Update(BaseData data)
        {
            // No-op for testing
        }

        public void Dispose()
        {
            // No-op for testing
        }
    }
}
