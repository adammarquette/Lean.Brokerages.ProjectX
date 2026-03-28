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
using QuantConnect.Packets;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Logging;
using System.Collections.Generic;
using QuantConnect.Util;
using QuantConnect.Data;

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
        public override Dictionary<string, string> BrokerageData { get; }

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
            throw new NotImplementedException("ProjectXBrokerageFactory.GetBrokerageModel(): Implementation pending Phase 4");
        }

        /// <summary>
        /// Creates a new IBrokerage instance
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job), "ProjectXBrokerageFactory.CreateBrokerage(): Job packet cannot be null");
            }

            Log.Trace($"ProjectXBrokerageFactory.CreateBrokerage(): Creating brokerage for user {job.UserId}");

            var aggregator = Composer.Instance.GetPart<IDataAggregator>();
            var brokerage = new ProjectXBrokerage(aggregator);

            // Configure the brokerage with job-specific settings
            brokerage.SetJob(job);

            Log.Trace("ProjectXBrokerageFactory.CreateBrokerage(): Brokerage instance created successfully");

            return brokerage;
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