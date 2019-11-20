﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Recognizers.Text.DataTypes.TimexExpression
{
    public static class TimexRegex
    {
        private const string DateTimeCollectionName = "datetime";
        private const string DateCollectionName = "date";
        private const string TimeCollectionName = "time";
        private const string PeriodCollectionName = "period";

        private static IDictionary<string, Regex[]> timexRegex = new Dictionary<string, Regex[]>
        {
            {
                DateCollectionName, new Regex[]
                {
                    // date
                    new Regex(@"^(?<year>\d\d\d\d)-(?<month>\d\d)-(?<dayOfMonth>\d\d)"),
                    new Regex(@"^XXXX-WXX-(?<dayOfWeek>\d)"),
                    new Regex(@"^XXXX-(?<month>\d\d)-(?<dayOfMonth>\d\d)"),

                    // daterange
                    new Regex(@"^(?<year>\d\d\d\d)"),
                    new Regex(@"^(?<year>\d\d\d\d)-(?<month>\d\d)"),
                    new Regex(@"^(?<season>SP|SU|FA|WI)"),
                    new Regex(@"^(?<year>\d\d\d\d)-(?<season>SP|SU|FA|WI)"),
                    new Regex(@"^(?<year>\d\d\d\d)-W(?<weekOfYear>\d\d)"),
                    new Regex(@"^(?<year>\d\d\d\d)-W(?<weekOfYear>\d\d)-(?<weekend>WE)"),
                    new Regex(@"^XXXX-(?<month>\d\d)"),
                    new Regex(@"^XXXX-(?<month>\d\d)-W(?<weekOfMonth>\d\d)"),
                    new Regex(@"^XXXX-(?<month>\d\d)-WXX-(?<weekOfMonth>\d{1,2})"),
                    new Regex(@"^XXXX-(?<month>\d\d)-WXX-(?<weekOfMonth>\d)-(?<dayOfWeek>\d)"),
                }
            },
            {
                TimeCollectionName, new Regex[]
                {
                    // time
                    new Regex(@"T(?<hour>\d\d)Z?$"),
                    new Regex(@"T(?<hour>\d\d):(?<minute>\d\d)Z?$"),
                    new Regex(@"T(?<hour>\d\d):(?<minute>\d\d):(?<second>\d\d)Z?$"),

                    // timerange
                    new Regex(@"^T(?<partOfDay>DT|NI|MO|AF|EV)$"),
                }
            },
            {
                PeriodCollectionName, new Regex[]
                {
                    new Regex(@"^P(?<amount>\d*\.?\d+)(?<dateUnit>Y|M|W|D)$"),
                    new Regex(@"^PT(?<amount>\d*\.?\d+)(?<timeUnit>H|M|S)$"),
                }
            },
        };

        public static bool Extract(string name, string timex, IDictionary<string, string> result)
        {
            var lowerName = name.ToLower();
            var nameGroup = new string[lowerName == DateTimeCollectionName ? 2 : 1];

            if (lowerName == DateTimeCollectionName)
            {
                nameGroup[0] = DateCollectionName;
                nameGroup[1] = TimeCollectionName;
            }
            else
            {
                nameGroup[0] = lowerName;
            }

            var anyTrue = false;
            foreach (var nameItem in nameGroup)
            {
                foreach (var entry in timexRegex[nameItem])
                {
                    if (TryExtract(entry, timex, result))
                    {
                        anyTrue = true;
                    }
                }
            }

            return anyTrue;
        }

        private static bool TryExtract(Regex regex, string timex, IDictionary<string, string> result)
        {
            var regexResult = regex.Match(timex);
            if (!regexResult.Success)
            {
                return false;
            }

            foreach (var groupName in regex.GetGroupNames())
            {
                result[groupName] = regexResult.Groups[groupName].Value;
            }

            return true;
        }
    }
}
