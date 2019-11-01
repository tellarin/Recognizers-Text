﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.Recognizers.Text.DateTime.Utilities;

using DateObject = System.DateTime;

namespace Microsoft.Recognizers.Text.DateTime
{
    public enum AgoLaterMode
    {
        /// <summary>
        /// Date
        /// </summary>
        Date = 0,

        /// <summary>
        /// Datetime
        /// </summary>
        DateTime,
    }

    public static class AgoLaterUtil
    {
        public delegate int SwiftDayDelegate(string text);

        public static List<Token> ExtractorDurationWithBeforeAndAfter(string text, ExtractResult er, List<Token> ret,
                                                                      IDateTimeUtilityConfiguration utilityConfiguration)
        {
            var pos = (int)er.Start + (int)er.Length;

            if (pos <= text.Length)
            {
                var afterString = text.Substring(pos);
                var beforeString = text.Substring(0, (int)er.Start);
                var isTimeDuration = utilityConfiguration.TimeUnitRegex.Match(er.Text).Success;

                if (MatchingUtil.GetAgoLaterIndex(afterString, utilityConfiguration.AgoRegex, out var index))
                {
                    // We don't support cases like "5 minutes from today" for now
                    // Cases like "5 minutes ago" or "5 minutes from now" are supported
                    // Cases like "2 days before today" or "2 weeks from today" are also supported
                    var isDayMatchInAfterString = utilityConfiguration.AgoRegex.Match(afterString).Groups["day"].Success;

                    if (!(isTimeDuration && isDayMatchInAfterString))
                    {
                        ret.Add(new Token(er.Start ?? 0, (er.Start + er.Length ?? 0) + index));
                    }

                    if (utilityConfiguration.CheckBothBeforeAfter && !isDayMatchInAfterString)
                    {
                        // check if regex match is split between beforeString and afterString
                        string beforeAfterStr = beforeString + afterString.Substring(0, index);
                        if (MatchingUtil.GetAgoLaterIndexInBeforeString(beforeAfterStr, utilityConfiguration.AgoRegex, out var indexStart))
                        {
                            isDayMatchInAfterString = utilityConfiguration.AgoRegex.Match(beforeAfterStr).Groups["day"].Success;

                            if (isDayMatchInAfterString && !(isTimeDuration && isDayMatchInAfterString))
                            {
                                ret.Add(new Token(indexStart, (er.Start + er.Length ?? 0) + index));
                            }
                        }
                    }
                }
                else if (utilityConfiguration.CheckBothBeforeAfter && MatchingUtil.GetAgoLaterIndexInBeforeString(beforeString, utilityConfiguration.AgoRegex, out index))
                {
                    // Check also beforeString
                    var isDayMatchInBeforeString = utilityConfiguration.AgoRegex.Match(beforeString).Groups["day"].Success;
                    if (!(isTimeDuration && isDayMatchInBeforeString))
                    {
                        ret.Add(new Token(index, er.Start + er.Length ?? 0));
                    }
                }
                else if (MatchingUtil.GetAgoLaterIndex(afterString, utilityConfiguration.LaterRegex, out index) || (utilityConfiguration.CheckBothBeforeAfter &&
                         MatchingUtil.GetAgoLaterIndexInBeforeString(beforeString, utilityConfiguration.LaterRegex, out index)))
                {
                    Token tokAfter = null, tokBefore = null;
                    if (MatchingUtil.GetAgoLaterIndex(afterString, utilityConfiguration.LaterRegex, out index))
                    {
                        var isDayMatchInAfterString = utilityConfiguration.LaterRegex.Match(afterString).Groups["day"].Success;

                        if (!(isTimeDuration && isDayMatchInAfterString))
                        {
                            tokAfter = new Token(er.Start ?? 0, (er.Start + er.Length ?? 0) + index);
                        }
                    }

                    // Check also beforeString
                    if (utilityConfiguration.CheckBothBeforeAfter && MatchingUtil.GetAgoLaterIndexInBeforeString(beforeString, utilityConfiguration.LaterRegex, out index))
                    {
                        var isDayMatchInBeforeString = utilityConfiguration.LaterRegex.Match(beforeString).Groups["day"].Success;
                        if (!(isTimeDuration && isDayMatchInBeforeString))
                        {
                            tokBefore = new Token(index, er.Start + er.Length ?? 0);
                        }
                    }

                    if (tokAfter != null && tokBefore != null && tokBefore.Start + tokBefore.Length > tokAfter.Start)
                    {
                        // merge overlapping tokens
                        ret.Add(new Token(tokBefore.Start, tokAfter.Start + tokAfter.Length - tokBefore.Start));
                    }
                    else if (tokAfter != null)
                    {
                        ret.Add(tokAfter);
                    }
                    else if (tokBefore != null)
                    {
                        ret.Add(tokBefore);
                    }
                }
                else if (MatchingUtil.GetTermIndex(beforeString, utilityConfiguration.InConnectorRegex, out index))
                {
                    // For range unit like "week, month, year", it should output dateRange or datetimeRange
                    if (!utilityConfiguration.RangeUnitRegex.IsMatch(er.Text))
                    {
                        if (er.Start != null && er.Length != null && (int)er.Start >= index)
                        {
                            ret.Add(new Token((int)er.Start - index, (int)er.Start + (int)er.Length));
                        }
                    }
                }
                else if (utilityConfiguration.CheckBothBeforeAfter && MatchingUtil.GetAgoLaterIndex(afterString, utilityConfiguration.InConnectorRegex, out index))
                {
                    // Check also afterString
                    // For range unit like "week, month, year", it should output dateRange or datetimeRange
                    if (!utilityConfiguration.RangeUnitRegex.IsMatch(er.Text))
                    {
                        if (er.Start != null && er.Length != null)
                        {
                            ret.Add(new Token((int)er.Start, (int)er.Start + (int)er.Length + index));
                        }
                    }
                }
                else if (MatchingUtil.GetTermIndex(beforeString, utilityConfiguration.WithinNextPrefixRegex, out index))
                {
                    // For range unit like "week, month, year, day, second, minute, hour", it should output dateRange or datetimeRange
                    if (!utilityConfiguration.DateUnitRegex.IsMatch(er.Text) && !utilityConfiguration.TimeUnitRegex.IsMatch(er.Text))
                    {
                        if (er.Start != null && er.Length != null && (int)er.Start >= index)
                        {
                            ret.Add(new Token((int)er.Start - index, (int)er.Start + (int)er.Length));
                        }
                    }
                }
                else if (utilityConfiguration.CheckBothBeforeAfter && MatchingUtil.GetAgoLaterIndex(afterString, utilityConfiguration.WithinNextPrefixRegex, out index))
                {
                    // Check also afterString
                    // For range unit like "week, month, year, day, second, minute, hour", it should output dateRange or datetimeRange
                    if (!utilityConfiguration.DateUnitRegex.IsMatch(er.Text) && !utilityConfiguration.TimeUnitRegex.IsMatch(er.Text))
                    {
                        if (er.Start != null && er.Length != null)
                        {
                            ret.Add(new Token((int)er.Start, (int)er.Start + (int)er.Length + index));
                        }
                    }
                }
            }

            return ret;
        }

        public static DateTimeResolutionResult ParseDurationWithAgoAndLater(
            string text,
            DateObject referenceTime,
            IDateTimeExtractor durationExtractor,
            IDateTimeParser durationParser,
            IParser numberParser,
            IImmutableDictionary<string, string> unitMap,
            Regex unitRegex,
            IDateTimeUtilityConfiguration utilityConfiguration,
            SwiftDayDelegate swiftDay)
        {
            var ret = new DateTimeResolutionResult();
            var durationRes = durationExtractor.Extract(text, referenceTime);

            if (durationRes.Count > 0)
            {
                var pr = durationParser.Parse(durationRes[0], referenceTime);
                var matches = unitRegex.Matches(text);
                if (matches.Count > 0)
                {
                    var afterStr = text.Substring((int)durationRes[0].Start + (int)durationRes[0].Length).Trim();

                    var beforeStr = text.Substring(0, (int)durationRes[0].Start).Trim();

                    var mode = AgoLaterMode.Date;
                    if (pr.TimexStr.Contains("T"))
                    {
                        mode = AgoLaterMode.DateTime;
                    }

                    if (pr.Value != null)
                    {
                        return GetAgoLaterResult(pr, afterStr, beforeStr, referenceTime, numberParser, utilityConfiguration, mode, swiftDay);
                    }
                }
            }

            return ret;
        }

        private static DateTimeResolutionResult GetAgoLaterResult(
            DateTimeParseResult durationParseResult,
            string afterStr,
            string beforeStr,
            DateObject referenceTime,
            IParser numberParser,
            IDateTimeUtilityConfiguration utilityConfiguration,
            AgoLaterMode mode,
            SwiftDayDelegate swiftDay)
        {
            var ret = new DateTimeResolutionResult();
            var resultDateTime = referenceTime;
            var timex = durationParseResult.TimexStr;

            if (((DateTimeResolutionResult)durationParseResult.Value).Mod == Constants.MORE_THAN_MOD)
            {
                ret.Mod = Constants.MORE_THAN_MOD;
            }
            else if (((DateTimeResolutionResult)durationParseResult.Value).Mod == Constants.LESS_THAN_MOD)
            {
                ret.Mod = Constants.LESS_THAN_MOD;
            }

            if (MatchingUtil.ContainsAgoLaterIndex(afterStr, utilityConfiguration.AgoRegex))
            {
                var match = utilityConfiguration.AgoRegex.Match(afterStr);
                var swift = 0;

                // Handle cases like "3 days before yesterday"
                if (match.Success && !string.IsNullOrEmpty(match.Groups["day"].Value))
                {
                    swift = swiftDay(match.Groups["day"].Value);
                }
                else if (utilityConfiguration.CheckBothBeforeAfter && match.Success && !MatchingUtil.ContainsAgoLaterIndexInBeforeString(beforeStr, utilityConfiguration.AgoRegex))
                {
                    match = utilityConfiguration.AgoRegex.Match(beforeStr + " " + afterStr);
                    if (match.Success && !string.IsNullOrEmpty(match.Groups["day"].Value))
                    {
                        swift = swiftDay(match.Groups["day"].Value);
                    }
                }

                resultDateTime = DurationParsingUtil.ShiftDateTime(timex, referenceTime.AddDays(swift), false);

                ((DateTimeResolutionResult)durationParseResult.Value).Mod = Constants.BEFORE_MOD;
            }
            else if (utilityConfiguration.CheckBothBeforeAfter && MatchingUtil.ContainsAgoLaterIndexInBeforeString(beforeStr, utilityConfiguration.AgoRegex))
            {
                var match = utilityConfiguration.AgoRegex.Match(beforeStr);
                var swift = 0;

                // Handle cases like "3 days before yesterday"
                if (match.Success && !string.IsNullOrEmpty(match.Groups["day"].Value))
                {
                    swift = swiftDay(match.Groups["day"].Value);
                }

                resultDateTime = DurationParsingUtil.ShiftDateTime(timex, referenceTime.AddDays(swift), false);

                ((DateTimeResolutionResult)durationParseResult.Value).Mod = Constants.BEFORE_MOD;
            }
            else if (MatchingUtil.ContainsAgoLaterIndex(afterStr, utilityConfiguration.LaterRegex) ||
                     MatchingUtil.ContainsTermIndex(beforeStr, utilityConfiguration.InConnectorRegex) ||
                     (utilityConfiguration.CheckBothBeforeAfter && MatchingUtil.ContainsAgoLaterIndexInBeforeString(beforeStr, utilityConfiguration.LaterRegex)))
            {
                var match = utilityConfiguration.LaterRegex.Match(afterStr);
                var swift = 0;

                if (utilityConfiguration.CheckBothBeforeAfter && MatchingUtil.ContainsAgoLaterIndexInBeforeString(beforeStr, utilityConfiguration.LaterRegex) && string.IsNullOrEmpty(match.Groups["day"].Value))
                {
                    match = utilityConfiguration.LaterRegex.Match(beforeStr);
                }

                // Handle cases like "3 days after tomorrow"
                if (match.Success && !string.IsNullOrEmpty(match.Groups["day"].Value))
                {
                    swift = swiftDay(match.Groups["day"].Value);
                }

                var yearMatch = utilityConfiguration.SinceYearSuffixRegex.Match(afterStr);
                if (yearMatch.Success)
                {
                    var yearString = yearMatch.Groups[Constants.YearGroupName].Value;
                    var yearEr = new ExtractResult { Text = yearString };
                    var year = Convert.ToInt32((double)(numberParser.Parse(yearEr).Value ?? 0));
                    referenceTime = DateObject.MinValue.SafeCreateFromValue(year, 1, 1);
                }

                resultDateTime = DurationParsingUtil.ShiftDateTime(timex, referenceTime.AddDays(swift), true);

                ((DateTimeResolutionResult)durationParseResult.Value).Mod = Constants.AFTER_MOD;
            }
            else if (utilityConfiguration.CheckBothBeforeAfter && (MatchingUtil.ContainsAgoLaterIndexInBeforeString(beforeStr, utilityConfiguration.LaterRegex) ||
                     MatchingUtil.ContainsAgoLaterIndex(afterStr, utilityConfiguration.InConnectorRegex) ||
                     MatchingUtil.ContainsAgoLaterIndex(afterStr, utilityConfiguration.LaterRegex)))
            {
                // Check also beforeStr
                var match = utilityConfiguration.LaterRegex.Match(beforeStr);
                var swift = 0;

                if (MatchingUtil.ContainsAgoLaterIndex(afterStr, utilityConfiguration.LaterRegex) && string.IsNullOrEmpty(match.Groups["day"].Value))
                {
                    match = utilityConfiguration.LaterRegex.Match(beforeStr);
                }

                // Handle cases like "3 days after tomorrow"
                if (match.Success && !string.IsNullOrEmpty(match.Groups["day"].Value))
                {
                    swift = swiftDay(match.Groups["day"].Value);
                }

                var yearMatch = utilityConfiguration.SinceYearSuffixRegex.Match(beforeStr);
                if (yearMatch.Success)
                {
                    var yearString = yearMatch.Groups[Constants.YearGroupName].Value;
                    var yearEr = new ExtractResult { Text = yearString };
                    var year = Convert.ToInt32((double)(numberParser.Parse(yearEr).Value ?? 0));
                    referenceTime = DateObject.MinValue.SafeCreateFromValue(year, 1, 1);
                }

                resultDateTime = DurationParsingUtil.ShiftDateTime(timex, referenceTime.AddDays(swift), true);

                ((DateTimeResolutionResult)durationParseResult.Value).Mod = Constants.AFTER_MOD;
            }

            if (resultDateTime != referenceTime)
            {
                if (mode.Equals(AgoLaterMode.Date))
                {
                    ret.Timex = $"{DateTimeFormatUtil.LuisDate(resultDateTime)}";
                }
                else if (mode.Equals(AgoLaterMode.DateTime))
                {
                    ret.Timex = $"{DateTimeFormatUtil.LuisDateTime(resultDateTime)}";
                }

                ret.FutureValue = ret.PastValue = resultDateTime;
                ret.SubDateTimeEntities = new List<object> { durationParseResult };
                ret.Success = true;
            }

            return ret;
        }
    }
}