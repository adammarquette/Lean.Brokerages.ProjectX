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
using System.Threading;
using QuantConnect.ToolBox;
using QuantConnect.Logging;
using QuantConnect.Configuration;
using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QuantConnect.Brokerages.ProjectXBrokerage.ToolBox
{
    /// <summary>
    /// ProjectXBrokerage implementation of <see cref="IExchangeInfoDownloader"/>
    /// </summary>
    public class ProjectXBrokerageExchangeInfoDownloader : IExchangeInfoDownloader, IDisposable
    {
        private static readonly string[] CmeRoots = new[]
        {
            "ES", "MES", "NQ", "MNQ", "RTY", "M2K", "YM", "MYM",
            "6E", "6J", "6B", "6A", "6C", "6S", "6N", "6Z",
            "SR1", "SR3", "GE"
        };

        private readonly IProjectXApiClient _apiClient;
        private ServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectXBrokerageExchangeInfoDownloader"/>
        /// </summary>
        public ProjectXBrokerageExchangeInfoDownloader()
        {
            var apiKey = Config.Get("brokerage-project-x-api-key");
            var apiSecret = Config.Get("brokerage-project-x-api-secret");
            var baseUrl = Config.Get("brokerage-project-x-base-url", "https://gateway.projectx.com/api");

            var configValues = new Dictionary<string, string>
            {
                ["ProjectX:ApiKey"] = apiKey,
                ["ProjectX:ApiSecret"] = apiSecret,
                ["ProjectX:BaseUrl"] = baseUrl
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProjectXApiClient(configuration);

            _serviceProvider = services.BuildServiceProvider();
            _apiClient = _serviceProvider.GetRequiredService<IProjectXApiClient>();
        }

        /// <summary>
        /// Market
        /// </summary>
        public string Market => QuantConnect.Market.CME;

        /// <summary>
        /// Get exchange info comma-separated data
        /// </summary>
        /// <returns>Enumerable of exchange info for this market</returns>
        public IEnumerable<string> Get()
        {
            var rows = new List<string>();

            foreach (var root in CmeRoots)
            {
                IEnumerable<MarqSpec.Client.ProjectX.Api.Models.Contract> contracts;
                try
                {
                    contracts = _apiClient.SearchContractsAsync(root, true, CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"ProjectXBrokerageExchangeInfoDownloader.Get(): Error fetching contracts for root {root}");
                    continue;
                }

                if (contracts == null)
                {
                    continue;
                }

                foreach (var contract in contracts)
                {
                    if (contract.TickSize <= 0 || contract.TickValue <= 0)
                    {
                        Log.Trace($"ProjectXBrokerageExchangeInfoDownloader.Get(): Skipping {contract.Id} — invalid TickSize or TickValue");
                        continue;
                    }

                    var contractMultiplier = contract.TickValue / contract.TickSize;
                    var description = (contract.Description ?? contract.Name ?? contract.Id).Replace(",", " ");
                    rows.Add($"{Market},{contract.Id},future,{description},USD,{contractMultiplier},{contract.TickSize},1,{contract.Id},1");
                }
            }

            return rows.OrderBy(r => r.Split(',')[1], StringComparer.Ordinal);
        }

        /// <summary>
        /// Releases unmanaged resources
        /// </summary>
        public void Dispose()
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;
        }
    }
}