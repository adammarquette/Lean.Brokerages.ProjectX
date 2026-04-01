/*
﻿ * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
﻿ * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
﻿ *
﻿ * Licensed under the Apache License, Version 2.0 (the "License");
﻿ * you may not use this file except in compliance with the License.
﻿ * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
﻿ *
﻿ * Unless required by applicable law or agreed to in writing, software
﻿ * distributed under the License is distributed on an "AS IS" BASIS,
﻿ * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
﻿ * See the License for the specific language governing permissions and
﻿ * limitations under the License.
﻿*/
﻿
﻿using System;
﻿using System.Collections.Generic;
﻿using Moq;
﻿using NUnit.Framework;
﻿using QuantConnect.Interfaces;
﻿using QuantConnect.Packets;
﻿using QuantConnect.Securities;
﻿
﻿namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
﻿{
﻿    [TestFixture]
﻿    public class ProjectXBrokerageFactoryTests
﻿    {
﻿        private ProjectXBrokerageFactory _factory;
﻿
﻿        [SetUp]
﻿        public void SetUp()
﻿        {
﻿            _factory = new ProjectXBrokerageFactory();
﻿        }
﻿
﻿        [Test]
﻿        public void BrokerageType_IsCorrect()
﻿        {
﻿            Assert.AreEqual(typeof(ProjectXBrokerage), _factory.BrokerageType);
﻿        }
﻿
﻿        [Test]
﻿        public void GetBrokerageModel_ReturnsProjectXBrokerageModel()
﻿        {
﻿            // Arrange
﻿            var orderProvider = new Mock<IOrderProvider>();
﻿
﻿            // Act
﻿            var model = _factory.GetBrokerageModel(orderProvider.Object);
﻿
﻿            // Assert
﻿            Assert.IsInstanceOf<ProjectXBrokerageModel>(model);
﻿        }
﻿
﻿        [Test]
﻿        public void BrokerageData_ContainsRequiredKeys()
﻿        {
﻿            // Act
﻿            var brokerageData = _factory.BrokerageData;
﻿
﻿            // Assert
﻿            Assert.IsTrue(brokerageData.ContainsKey("brokerage-project-x-api-key"));
﻿            Assert.IsTrue(brokerageData.ContainsKey("brokerage-project-x-api-secret"));
﻿            Assert.IsTrue(brokerageData.ContainsKey("brokerage-project-x-environment"));
﻿            Assert.IsTrue(brokerageData.ContainsKey("brokerage-project-x-account-id"));
﻿        }
﻿
﻿        [Test]
﻿        public void CreateBrokerage_WithValidJob_ReturnsBrokerageInstance()
﻿        {
﻿            // Arrange
﻿            var job = new LiveNodePacket
﻿            {
﻿                BrokerageData = new Dictionary<string, string>
﻿                {
﻿                    { "brokerage-project-x-api-key", "test-key" },
﻿                    { "brokerage-project-x-api-secret", "test-secret" },
﻿                    { "brokerage-project-x-environment", "sandbox" },
﻿                    { "brokerage-project-x-account-id", "12345" }
﻿                }
﻿            };
﻿            var algorithm = new Mock<IAlgorithm>();
﻿
﻿            // Act
﻿            var brokerage = _factory.CreateBrokerage(job, algorithm.Object);
﻿
﻿            // Assert
﻿            Assert.IsInstanceOf<ProjectXBrokerage>(brokerage);
﻿        }
﻿
﻿        [Test]
﻿        public void CreateBrokerage_WithMissingApiKey_ThrowsArgumentException()
﻿        {
﻿            // Arrange
﻿            var job = new LiveNodePacket
﻿            {
﻿                BrokerageData = new Dictionary<string, string>
﻿                {
﻿                    // Missing API key
﻿                    { "brokerage-project-x-api-secret", "test-secret" },
﻿                    { "brokerage-project-x-environment", "sandbox" },
﻿                    { "brokerage-project-x-account-id", "12345" }
﻿                }
﻿            };
﻿            var algorithm = new Mock<IAlgorithm>();
﻿
﻿            // Act & Assert
﻿            Assert.Throws<ArgumentException>(() => _factory.CreateBrokerage(job, algorithm.Object));
﻿        }
﻿
﻿        [Test]
﻿        public void Dispose_DoesNotThrow()
﻿        {
﻿            Assert.DoesNotThrow(() => _factory.Dispose());
﻿        }
﻿    }
﻿}
﻿