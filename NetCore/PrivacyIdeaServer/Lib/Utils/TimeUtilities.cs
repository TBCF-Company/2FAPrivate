// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using System.Globalization;
using System.Text.RegularExpressions;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Time and date parsing, conversion, and validation utilities.
/// </summary>
public static partial class TimeUtilities
{
    private static readonly Dictionary<string, int> DayOfWeekIndex = new()
    {
        { "mon", 1 }, { "tue", 2 }, { "wed", 3 }, { "thu", 4 },
        { "fri", 5 }, { "sat", 6 }, { "sun", 7 }
    };

    /// <summary>
    /// Check if the given time is contained in the time_range string.
    /// The time_range can be something like:
    /// DOW-DOW: hh:mm-hh:mm, DOW-DOW: hh:mm-hh:mm
    /// DOW being: Mon, Tue, Wed, Thu, Fri, Sat, Sun
    /// </summary>
    /// <param name="timeRange">The time range string</param>
    /// <param name="checkTime">The time to check (defaults to now)</param>
    /// <returns>True if time is within time_range</returns>
    public static bool CheckTimeInRange(string timeRange, DateTime? checkTime = null)
    {
        checkTime ??= DateTime.Now;
        bool timeMatch = false;

        int checkDay = (int)checkTime.Value.DayOfWeek;
        if (checkDay == 0) checkDay = 7; // Sunday
        
        var checkHour = checkTime.Value.TimeOfDay;

        // Remove whitespaces
        timeRange = timeRange.Replace(" ", "");
        
        // Split into list of time ranges
        var timeRanges = timeRange.Split(',');

        foreach (var tr in timeRanges)
        {
            // tr is something like: Mon-Tue:09:30-17:30
            var parts = tr.ToLowerInvariant().Split(':', 2);
            if (parts.Length != 2) continue;

            var dow = parts[0];
            var t = parts[1];

            string dowStart, dowEnd;
            if (dow.Contains('-'))
            {
                var dowParts = dow.Split('-');
                dowStart = dowParts[0];
                dowEnd = dowParts[1];
            }
            else
            {
                dowStart = dowEnd = dow;
            }

            // Check for valid day of the week
            if (!DayOfWeekIndex.ContainsKey(dowStart) || !DayOfWeekIndex.ContainsKey(dowEnd))
            {
                throw new ArgumentException($"Invalid day of the week '{dow}'. Allowed values are: {string.Join(", ", DayOfWeekIndex.Keys)}");
            }

            var timeParts = t.Split('-');
            if (timeParts.Length != 2) continue;

            var tStart = timeParts[0];
            var tEnd = timeParts[1];

            // Parse start time
            var tsArray = tStart.Split(':').Select(int.Parse).ToArray();
            var timeStart = tsArray.Length == 2
                ? new TimeSpan(tsArray[0], tsArray[1], 0)
                : new TimeSpan(tsArray[0], 0, 0);

            // Parse end time
            var teArray = tEnd.Split(':').Select(int.Parse).ToArray();
            var timeEnd = teArray.Length == 2
                ? new TimeSpan(teArray[0], teArray[1], 0)
                : new TimeSpan(teArray[0], 0, 0);

            if (timeStart > timeEnd)
            {
                throw new ArgumentException($"Invalid time range. Start time is greater than end time: {t}");
            }

            // Check the day and the time
            if (DayOfWeekIndex[dowStart] <= checkDay && checkDay <= DayOfWeekIndex[dowEnd]
                && timeStart <= checkHour && checkHour <= timeEnd)
            {
                timeMatch = true;
            }
        }

        return timeMatch;
    }

    /// <summary>
    /// Parse a time limit string in the format "2/5m" or "1/3h" 
    /// (two in five minutes, one in three hours).
    /// </summary>
    /// <param name="limit">A time limit string</param>
    /// <returns>Tuple of (count, timespan)</returns>
    public static (int Count, TimeSpan TimeSpan) ParseTimeLimit(string limit)
    {
        // Strip and replace blanks
        limit = limit.Trim().Replace(" ", "");
        
        var timeSpecifier = char.ToLowerInvariant(limit[^1]);
        if (timeSpecifier is not ('m' or 's' or 'h'))
        {
            throw new ArgumentException("Invalid time specifier");
        }

        var timeParts = limit[..^1].Split('/');
        var count = int.Parse(timeParts[0]);
        var timeDelta = int.Parse(timeParts[1]);

        var td = timeSpecifier switch
        {
            's' => TimeSpan.FromSeconds(timeDelta),
            'h' => TimeSpan.FromHours(timeDelta),
            _ => TimeSpan.FromMinutes(timeDelta)
        };

        return (count, td);
    }

    /// <summary>
    /// Parse a date string that can be:
    /// - Offset: +30d, +12h, +10m
    /// - Fixed date: 23.12.2016 23:30, 2016/12/23 11:30pm, 2017-04-27T20:00+0200
    /// </summary>
    /// <param name="dateString">A string containing a date or an offset</param>
    /// <returns>DateTime object</returns>
    public static DateTime? ParseDate(string dateString)
    {
        dateString = dateString.Trim();
        
        if (string.IsNullOrEmpty(dateString))
            return DateTime.Now;

        if (dateString.StartsWith('+'))
        {
            // We are using an offset
            var deltaSpecifier = char.ToLowerInvariant(dateString[^1]);
            if (deltaSpecifier is not ('m' or 'h' or 'd'))
            {
                return DateTime.Now;
            }

            if (!int.TryParse(dateString[1..^1], out var deltaAmount))
                return DateTime.Now;

            var td = deltaSpecifier switch
            {
                'm' => TimeSpan.FromMinutes(deltaAmount),
                'h' => TimeSpan.FromHours(deltaAmount),
                'd' => TimeSpan.FromDays(deltaAmount),
                _ => TimeSpan.Zero
            };

            return DateTime.Now + td;
        }

        // Try to parse as a fixed date
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Parse a timedelta string like "+5d" or "-30m" and return a TimeSpan.
    /// Allowed identifiers: s, m, h, d, y
    /// </summary>
    /// <param name="s">A string like +30m or -5d</param>
    /// <returns>TimeSpan</returns>
    public static TimeSpan ParseTimeDelta(string s)
    {
        var match = TimeDeltaRegex().Match(s);
        if (!match.Success)
        {
            throw new ArgumentException($"Unsupported timedelta: {s}");
        }

        var count = int.Parse(match.Groups[2].Value);
        if (match.Groups[1].Value == "-")
        {
            count = -count;
        }

        var unit = match.Groups[3].Value;
        return unit switch
        {
            "s" => TimeSpan.FromSeconds(count),
            "m" => TimeSpan.FromMinutes(count),
            "h" => TimeSpan.FromHours(count),
            "d" => TimeSpan.FromDays(count),
            "y" => TimeSpan.FromDays(365 * count),
            _ => TimeSpan.Zero
        };
    }

    /// <summary>
    /// Parse a time string like "5d" or "24h" into seconds.
    /// </summary>
    /// <param name="s">Time string like 5d or 24h</param>
    /// <returns>Time in seconds as integer</returns>
    public static int ParseTimeSecInt(object s)
    {
        try
        {
            if (s is string str)
            {
                var td = ParseTimeDelta(str);
                return (int)Math.Abs(td.TotalSeconds);
            }
            return Convert.ToInt32(s);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Parse a string as used in token event handlers:
    /// "New date {now}+5d. Some {other} {tags}"
    /// Returns the modified string and the timedelta.
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <returns>Tuple of (modified string, timedelta)</returns>
    public static (string ModifiedString, TimeSpan TimeDelta) ParseTimeOffsetFromNow(string s)
    {
        var td = TimeSpan.Zero;
        var match1 = TimeOffsetRegex1().Match(s);
        var match2 = TimeOffsetRegex2().Match(s);
        var match = match1.Success ? match1 : match2;

        if (match.Success)
        {
            var s1 = match.Groups[1].Value;
            var s2 = match.Groups[2].Value;
            var s3 = match.Groups[3].Value;
            s = s1 + s3;
            td = ParseTimeDelta(s2);
        }

        return (s, td);
    }

    /// <summary>
    /// Parse legacy time format (DD/MM/YY hh:mm) and return as ISO 8601 string with timezone.
    /// </summary>
    /// <param name="ts">Legacy timestamp string</param>
    /// <param name="returnDate">If true, return DateTime instead of string</param>
    /// <returns>ISO 8601 formatted string or DateTime</returns>
    public static object ParseLegacyTime(string ts, bool returnDate = false)
    {
        // Try to parse the date
        if (!DateTime.TryParse(ts, out var d))
        {
            d = DateTime.Now;
        }

        // If no timezone info, assume local timezone
        if (d.Kind == DateTimeKind.Unspecified)
        {
            d = DateTime.SpecifyKind(d, DateTimeKind.Local);
        }

        if (returnDate)
            return d;

        return d.ToString("yyyy-MM-ddTHH:mm:sszzz");
    }

    /// <summary>
    /// Convert a timezone-aware DateTime to UTC (timezone-naive).
    /// </summary>
    /// <param name="timestamp">DateTime to convert</param>
    /// <returns>UTC DateTime without timezone info</returns>
    public static DateTime ConvertTimestampToUtc(DateTime timestamp)
    {
        return timestamp.ToUniversalTime();
    }

    [GeneratedRegex(@"\s*([+-]?)\s*(\d+)\s*([smhdy])\s*$")]
    private static partial Regex TimeDeltaRegex();

    [GeneratedRegex(@"(^.*{current_time})([+-]\d+[smhd])(.*$)")]
    private static partial Regex TimeOffsetRegex1();

    [GeneratedRegex(@"(^.*{now})([+-]\d+[smhd])(.*$)")]
    private static partial Regex TimeOffsetRegex2();
}
