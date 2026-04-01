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
using System.Globalization;
using QuantConnect.ToolBox;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using static QuantConnect.Configuration.ApplicationParser;

namespace QuantConnect.Brokerages.ProjectXBrokerage.ToolBox
{
    static class Program
    {
        static void Main(string[] args)
        {
            var optionsObject = ToolboxArgumentParser.ParseArguments(args);
            if (optionsObject.Count == 0)
            {
                PrintMessageAndExit();
            }

            if (!optionsObject.TryGetValue("app", out var targetApp))
            {
                PrintMessageAndExit(1, "ERROR: --app value is required");
            }

            var targetAppName = targetApp.ToString();
            if (targetAppName.Contains("download") || targetAppName.Contains("dl"))
            {
                var tickers = GetParameterOrExit(optionsObject, "tickers");
                var resolutionParam = GetParameterOrExit(optionsObject, "resolution");
                var fromDate = GetParameterOrExit(optionsObject, "from-date");
                var toDate = GetParameterOrExit(optionsObject, "to-date");

                var resolution = (Resolution)Enum.Parse(typeof(Resolution), resolutionParam, true);
                var startUtc = DateTime.ParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                var endUtc = DateTime.ParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

                var symbolMapper = new ProjectXSymbolMapper();

                using var downloader = new ProjectXBrokerageDownloader();

                foreach (var ticker in tickers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    Symbol symbol;
                    try
                    {
                        symbol = symbolMapper.GetLeanSymbol(ticker, SecurityType.Future, string.Empty);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Program.Main(): Failed to map ticker '{ticker}' to a LEAN symbol. Skipping.");
                        continue;
                    }

                    var parameters = new DataDownloaderGetParameters(symbol, resolution, startUtc, endUtc);
                    var bars = downloader.Get(parameters);

                    if (bars == null)
                    {
                        Log.Trace($"Program.Main(): No data returned for {ticker}. Skipping.");
                        continue;
                    }

                    new LeanDataWriter(resolution, symbol, Globals.DataFolder).Write(bars);
                    Log.Trace($"Program.Main(): Download complete for {ticker}");
                }
            }
            else if (targetAppName.Contains("updater") || targetAppName.EndsWith("spu"))
            {
                using var eid = new ProjectXBrokerageExchangeInfoDownloader();
                new ExchangeInfoUpdater(eid).Run();
            }
            else
            {
                PrintMessageAndExit(1, "ERROR: Unrecognized --app value");
            }
        }
    }
}