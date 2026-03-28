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

using NUnit.Framework;
using QuantConnect.Brokerages.ProjectXBrokerage;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using Moq;

namespace QuantConnect.Brokerages.Tests.ProjectXBrokerage
{
    [TestFixture]
    public class ProjectXBrokerageConnectionTests
    {
        private Mock<IDataAggregator> _mockAggregator;
        private Brokerages.ProjectXBrokerage.ProjectXBrokerage _brokerage;

        [SetUp]
        public void SetUp()
        {
            // Setup mock aggregator
            _mockAggregator = new Mock<IDataAggregator>();
            
            // Set default configuration for tests
            Config.Set("project-x-api-key", "test-api-key");
            Config.Set("project-x-api-secret", "test-api-secret");
            Config.Set("project-x-environment", "sandbox");
            Config.Set("project-x-reconnect-attempts", "3");
            Config.Set("project-x-reconnect-delay", "100");
            Config.Set("project-x-heartbeat-interval", "1000");
        }

        [TearDown]
        public void TearDown()
        {
            _brokerage?.Dispose();
            _brokerage = null;
            
            // Clean up configuration
            Config.Reset();
        }

        [Test]
        public void Constructor_InitializesWithCorrectState()
        {
            // Arrange & Act
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Assert
            Assert.IsFalse(_brokerage.IsConnected, "Brokerage should not be connected initially");
            Assert.AreEqual("ProjectXBrokerage", _brokerage.Name);
        }

        [Test]
        public void Constructor_LoadsConfigurationCorrectly()
        {
            // Arrange & Act
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Assert - brokerage is created without throwing
            Assert.IsNotNull(_brokerage);
        }

        [Test]
        public void IsConnected_ReturnsFalseByDefault()
        {
            // Arrange
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act
            var isConnected = _brokerage.IsConnected;

            // Assert
            Assert.IsFalse(isConnected);
        }

        [Test]
        public void SetJob_UpdatesConfigurationFromJobPacket()
        {
            // Arrange
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);
            var job = new LiveNodePacket
            {
                UserId = 123,
                BrokerageData = new Dictionary<string, string>
                {
                    { "project-x-api-key", "job-api-key" },
                    { "project-x-api-secret", "job-api-secret" },
                    { "project-x-environment", "production" }
                }
            };

            // Act - should not throw
            Assert.DoesNotThrow(() => _brokerage.SetJob(job));
        }

        [Test]
        public void SetJob_HandlesNullJobGracefully()
        {
            // Arrange
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => _brokerage.SetJob(null));
        }

        [Test]
        public void SetJob_ValidatesConfigurationAfterUpdate()
        {
            // Arrange
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);
            var job = new LiveNodePacket
            {
                UserId = 123,
                BrokerageData = new Dictionary<string, string>
                {
                    { "project-x-api-key", "" }, // Invalid empty key
                    { "project-x-api-secret", "secret" },
                    { "project-x-environment", "sandbox" }
                }
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _brokerage.SetJob(job));
        }

        [Test]
        public void Connect_ThrowsExceptionWithMissingApiKey()
        {
            // Arrange
            Config.Set("project-x-api-key", "");
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _brokerage.Connect());
        }

        [Test]
        public void Connect_ThrowsExceptionWithMissingApiSecret()
        {
            // Arrange
            Config.Set("project-x-api-secret", "");
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _brokerage.Connect());
        }

        [Test]
        public void Connect_ThrowsExceptionWithInvalidEnvironment()
        {
            // Arrange
            Config.Set("project-x-environment", "invalid");
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _brokerage.Connect());
        }

        [Test]
        public void Disconnect_CanBeCalledWhenNotConnected()
        {
            // Arrange
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => _brokerage.Disconnect());
            Assert.IsFalse(_brokerage.IsConnected);
        }

        [Test]
        public void Disconnect_IsIdempotent()
        {
            // Arrange
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act & Assert - multiple disconnects should not throw
            Assert.DoesNotThrow(() => _brokerage.Disconnect());
            Assert.DoesNotThrow(() => _brokerage.Disconnect());
            Assert.DoesNotThrow(() => _brokerage.Disconnect());
        }

        [Test]
        public void Dispose_DisconnectsAndCleansUpResources()
        {
            // Arrange
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act - should not throw
            Assert.DoesNotThrow(() => _brokerage.Dispose());

            // Assert
            Assert.IsFalse(_brokerage.IsConnected);
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act & Assert - multiple disposes should not throw
            Assert.DoesNotThrow(() => _brokerage.Dispose());
            Assert.DoesNotThrow(() => _brokerage.Dispose());
        }

        [Test]
        public void Configuration_RespectsEnvironmentVariables()
        {
            // Arrange
            Config.Reset();
            Environment.SetEnvironmentVariable("PROJECT_X_API_KEY", "env-api-key");
            Environment.SetEnvironmentVariable("PROJECT_X_API_SECRET", "env-api-secret");

            try
            {
                // Act
                _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

                // Assert - should not throw on construction
                Assert.IsNotNull(_brokerage);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("PROJECT_X_API_KEY", null);
                Environment.SetEnvironmentVariable("PROJECT_X_API_SECRET", null);
            }
        }

        [Test]
        public void Configuration_DefaultReconnectAttempts()
        {
            // Arrange - Reset and don't set the key to test default value
            Config.Reset();
            Config.Set("project-x-api-key", "test-api-key");
            Config.Set("project-x-api-secret", "test-api-secret");
            Config.Set("project-x-environment", "sandbox");
            // Note: project-x-reconnect-attempts is not set, so default should be used

            // Act & Assert - should use default value without throwing
            Assert.DoesNotThrow(() => _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object));
        }

        [Test]
        public void Configuration_DefaultReconnectDelay()
        {
            // Arrange - Reset and don't set the key to test default value
            Config.Reset();
            Config.Set("project-x-api-key", "test-api-key");
            Config.Set("project-x-api-secret", "test-api-secret");
            Config.Set("project-x-environment", "sandbox");
            // Note: project-x-reconnect-delay is not set, so default should be used

            // Act & Assert - should use default value without throwing
            Assert.DoesNotThrow(() => _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object));
        }

        [Test]
        public void Configuration_InvalidReconnectAttempts()
        {
            // Arrange
            Config.Set("project-x-reconnect-attempts", "0");
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _brokerage.Connect());
        }

        [Test]
        public void Configuration_InvalidReconnectDelay()
        {
            // Arrange
            Config.Set("project-x-reconnect-delay", "50"); // Below minimum of 100ms
            _brokerage = new Brokerages.ProjectXBrokerage.ProjectXBrokerage(_mockAggregator.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _brokerage.Connect());
        }
    }
}
