﻿/**************************************************************************
Copyright 2016 Carsten Gehling

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
**************************************************************************/
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace StopWatch
{
    static public class JiraTimeHelpers
    {
        public static TimeTrackingConfiguration Configuration { get; set; }

        public static string DateTimeToJiraDateTime(DateTimeOffset date)
        {
            string formatted = date.ToString("yyyy-MM-dd\\THH:mm:ss.fffzzzz", CultureInfo.InvariantCulture);
            return formatted.Substring(0, formatted.Length - 3) + formatted.Substring(formatted.Length - 2);
        }

        public static string TimeSpanToJiraTime(TimeSpan ts)
        {
            if (Configuration == null || ts.TotalHours < Configuration.workingHoursPerDay)
            {
                if (ts.Days > 0)
                    return String.Format("{0:%d}d {0:%h}h {0:%m}m", ts);

                if (ts.Hours > 0)
                    return String.Format("{0:%h}h {0:%m}m", ts);

                return String.Format("{0:%m}m", ts);
            }
            else
            {
                int days =(int) Math.Floor(ts.TotalMinutes / (Configuration.workingHoursPerDay * 60));
                int hours = (int) Math.Floor(ts.TotalHours - (days * Configuration.workingHoursPerDay));
                int minutes = (int) Math.Floor(ts.TotalMinutes - ((days * Configuration.workingHoursPerDay) + hours) * 60);
                if (days > 0)
                {
                    return String.Format("{0}d {1}h {2}m", days, hours, minutes);
                }
                else if (hours > 0)
                {
                    return String.Format("{0}h {1}m", hours, minutes);
                }
                else
                {
                    return String.Format("{0}m", minutes);
                }
            }

        }


        public static TimeSpan? JiraTimeToTimeSpan(string time)
        {
            string s;
            decimal t;
            int minutes = 0;
            bool validFormat = true;

            time = time.Trim();

            if (time == "0")
                return TimeSpan.Zero;

            MatchCollection matches = new Regex(@"([0-9,\.]+[dhm] *?)+?", RegexOptions.IgnoreCase).Matches(time);
            if (matches.Count == 0)
                return null;

            foreach (Match match in matches)
            {
                s = match.Value.ToUpper();
                s = s.Trim();

                if (!s.Contains("M") && !s.Contains("H") && !s.Contains("D"))
                {
                    validFormat = false;
                    break;
                }

                if (!decimal.TryParse(s.Replace("M", "").Replace("H", "").Replace("D", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out t))
                {
                    validFormat = false;
                    break;
                }

                if (s.Contains("M"))
                    minutes += (int)t;

                if (s.Contains("H"))
                    minutes += (int)(t * 60);

                if (s.Contains("D"))
                    minutes += (int)(t * 60 * (decimal)(Configuration?.workingHoursPerDay ?? 8));
            }

            if (!validFormat)
                return null;

            return new TimeSpan(minutes / 60, minutes % 60, 0);
        }

        public static TimeSpan RoundUp(TimeSpan timeSpan)
        {
            double RoundTo(decimal minutes, int roundTo)
            {
                return (double)(Math.Ceiling(minutes / roundTo) * roundTo);
            }

            StringCollection roundingSettings = Properties.Settings.Default.Rounding;
            var settingsData = from string setting in roundingSettings
                let settingArray = setting.Split(';')
                let minutesLimitStr = settingArray[0]
                let rounding = int.Parse(settingArray[1])
                let orderLast = minutesLimitStr == "*"
                let minutesLimit = minutesLimitStr != "*" ? int.Parse(minutesLimitStr) : (int?)null
                orderby orderLast, minutesLimit
                select new { MinutesLimit  = minutesLimit, Rounding = rounding };

            var totalMinutes = (decimal)timeSpan.TotalMinutes;

            int roundingValue = settingsData.First(sett => totalMinutes <= sett.MinutesLimit || sett.MinutesLimit == null)
                .Rounding;

            /*double roundedTo6 = RoundTo(totalMinutes, 6);
            double roundedTo12 = RoundTo(totalMinutes, 12);
            double roundedTo15 = RoundTo(totalMinutes, 15);*/
            //double roundedValue = Math.Min(roundedTo6, roundedTo18);
            //double roundedValue = totalMinutes <= 6 ? roundedTo6 : totalMinutes <= 12 ? roundedTo12 : roundedTo15;
            double roundedValue = RoundTo(totalMinutes, roundingValue);
            return TimeSpan.FromMinutes(roundedValue);
        }
    }
}

