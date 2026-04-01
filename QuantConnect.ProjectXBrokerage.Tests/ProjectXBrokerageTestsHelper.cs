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

namespace QuantConnect.Brokerages.ProjectXBrokerage.Tests
{
    /// <summary>
    /// Shared helpers for ProjectX brokerage test fixtures.
    /// </summary>
    internal static class ProjectXBrokerageTestsHelper
    {
        /// <summary>
        /// Returns the front-month ES (E-mini S&amp;P 500) futures symbol.
        /// ES expires on the third Friday of March, June, September, and December.
        /// </summary>
        public static Symbol GetFrontMonthES()
        {
            var quarterlyMonths = new[] { 3, 6, 9, 12 };
            var now = DateTime.UtcNow;

            foreach (var year in new[] { now.Year, now.Year + 1 })
            {
                foreach (var month in quarterlyMonths)
                {
                    var expiry = GetThirdFriday(year, month);
                    if (expiry > now)
                        return Symbol.CreateFuture("ES", Market.CME, expiry);
                }
            }

            throw new InvalidOperationException("Could not determine front-month ES expiry.");
        }

        /// <summary>
        /// Returns the third Friday of the given month and year.
        /// </summary>
        public static DateTime GetThirdFriday(int year, int month)
        {
            var first = new DateTime(year, month, 1);
            var daysToFriday = ((int)DayOfWeek.Friday - (int)first.DayOfWeek + 7) % 7;
            return first.AddDays(daysToFriday + 14);
        }
    }
}
