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
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Configuration;
using QuantConnect.Securities;
using MarqSpec.Client.ProjectX;
using MarqSpec.Client.ProjectX.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PxAggregateBarUnit = MarqSpec.Client.ProjectX.Api.Models.AggregateBarUnit;

namespace QuantConnect.Brokerages.ProjectXBrokerage.ToolBox
{
    /// <summary>
    /// ProjectXBrokerage Data Downloader implementation
    /// </summary>
    public class ProjectXBrokerageDownloader : IDataDownloader, IDisposable
    {
        private readonly IProjectXApiClient _apiClient;
        private readonly ProjectXSymbolMapper _symbolMapper;
        private ServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectXBrokerageDownloader"/>
        /// </summary>
        public ProjectXBrokerageDownloader()
        {
            _symbolMapper = new ProjectXSymbolMapper();

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
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="dataDownloaderGetParameters">model class for passing in parameters for historical data</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            var symbol = dataDownloaderGetParameters.Symbol;

            if (symbol.SecurityType != SecurityType.Future)
            {
                Log.Trace($"ProjectXBrokerageDownloader.Get(): {symbol} is not a Future. Only futures are supported.");
                return null;
            }

            if (symbol.IsCanonical() || symbol.Value.IndexOfInvariant("universe", true) != -1)
            {
                Log.Trace($"ProjectXBrokerageDownloader.Get(): {symbol} is canonical or universe. Skipping.");
                return null;
            }

            if (dataDownloaderGetParameters.Resolution == Resolution.Tick)
            {
                Log.Trace($"ProjectXBrokerageDownloader.Get(): Tick resolution is not supported.");
                return null;
            }

            var contractId = _symbolMapper.GetBrokerageSymbol(symbol);

            PxAggregateBarUnit unit;
            int limit;
            switch (dataDownloaderGetParameters.Resolution)
            {
                case Resolution.Second:
                    unit = PxAggregateBarUnit.Second;
                    limit = (int)Math.Ceiling((dataDownloaderGetParameters.EndUtc - dataDownloaderGetParameters.StartUtc).TotalSeconds) + 1;
                    break;
                case Resolution.Minute:
                    unit = PxAggregateBarUnit.Minute;
                    limit = (int)Math.Ceiling((dataDownloaderGetParameters.EndUtc - dataDownloaderGetParameters.StartUtc).TotalMinutes) + 1;
                    break;
                case Resolution.Hour:
                    unit = PxAggregateBarUnit.Hour;
                    limit = (int)Math.Ceiling((dataDownloaderGetParameters.EndUtc - dataDownloaderGetParameters.StartUtc).TotalHours) + 1;
                    break;
                case Resolution.Daily:
                    unit = PxAggregateBarUnit.Day;
                    limit = (int)Math.Ceiling((dataDownloaderGetParameters.EndUtc - dataDownloaderGetParameters.StartUtc).TotalDays) + 1;
                    break;
                default:
                    Log.Trace($"ProjectXBrokerageDownloader.Get(): Resolution {dataDownloaderGetParameters.Resolution} is not supported.");
                    return null;
            }

            limit = Math.Min(limit, 10000);

            Log.Trace($"ProjectXBrokerageDownloader.Get(): Requesting {limit} {unit} bars for {contractId} from {dataDownloaderGetParameters.StartUtc} to {dataDownloaderGetParameters.EndUtc}");

            var bars = _apiClient.GetHistoricalBarsAsync(
                contractId,
                dataDownloaderGetParameters.StartUtc,
                dataDownloaderGetParameters.EndUtc,
                unit,
                1,
                limit,
                true,
                false,
                CancellationToken.None
            ).GetAwaiter().GetResult();

            if (bars == null)
            {
                Log.Trace($"ProjectXBrokerageDownloader.Get(): No bars returned for {contractId}");
                return null;
            }

            var period = dataDownloaderGetParameters.Resolution.ToTimeSpan();
            var result = new List<BaseData>();
            foreach (var bar in bars)
            {
                result.Add(new TradeBar(bar.Timestamp, symbol, bar.Open, bar.High, bar.Low, bar.Close, (decimal)bar.Volume, period));
            }
            return result;
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