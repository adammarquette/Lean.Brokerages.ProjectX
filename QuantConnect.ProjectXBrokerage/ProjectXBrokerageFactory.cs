﻿/*
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
using QuantConnect.Data;
using QuantConnect.Packets;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Logging;
using QuantConnect.Util;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.ProjectXBrokerage
{
    /// <summary>
    /// Provides a template implementation of BrokerageFactory
    /// </summary>
    public class ProjectXBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Gets the brokerage data required to run the brokerage from configuration/disk
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>
        {
            { "brokerage-project-x-api-key", "" },
            { "brokerage-project-x-api-secret", "" },
            { "brokerage-project-x-environment", "production" },
            { "brokerage-project-x-account-id", "0" }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectXBrokerageFactory"/> class
        /// </summary>
        public ProjectXBrokerageFactory() : base(typeof(ProjectXBrokerage))
        {
            Log.Trace("ProjectXBrokerageFactory(): Initializing factory");
        }

        /// <summary>
        /// Gets a brokerage model that can be used to model this brokerage's unique behaviors
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider)
        {
            Log.Trace("ProjectXBrokerageFactory.GetBrokerageModel(): Creating brokerage model");
            return new ProjectXBrokerageModel(); 
        }

        /// <summary>
        /// Creates a new IBrokerage instance
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            Log.Trace($"ProjectXBrokerageFactory.CreateBrokerage(): Creating brokerage for user {job?.UserId}");
            
            var errors = new List<string>();
            var apiKey = Read<string>(job.BrokerageData, "brokerage-project-x-api-key", errors);
            var apiSecret = Read<string>(job.BrokerageData, "brokerage-project-x-api-secret", errors);
            var environment = Read<string>(job.BrokerageData, "brokerage-project-x-environment", errors);
            var accountId = Read<int>(job.BrokerageData, "brokerage-project-x-account-id", errors);

            if (errors.Count > 0)
            {
                var missingKeys = string.Join(", ", errors);
                Log.Error($"ProjectXBrokerageFactory.CreateBrokerage(): Missing keys in configuration: {missingKeys}");
                throw new ArgumentException($"Missing required configuration keys: {missingKeys}");
            }

            var aggregator = Composer.Instance.GetPart<IDataAggregator>();
            return new ProjectXBrokerage(apiKey, apiSecret, environment, accountId, aggregator);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            Log.Trace("ProjectXBrokerageFactory.Dispose(): Disposing factory resources");
            // Nothing to dispose yet, but logging for future cleanup tracking
        }
    }
}