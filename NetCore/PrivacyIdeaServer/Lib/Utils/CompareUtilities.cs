// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-FileCopyrightText: 2019 Friedrich Weber <friedrich.weber@netknights.it>
// SPDX-FileCopyrightText: 2025 Jelina Unger <jelina.unger@netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Exception for comparison errors.
/// </summary>
public class CompareException : Exception
{
    public CompareException(string message) : base(message) { }
}

/// <summary>
/// Primary comparator names.
/// </summary>
public static class PrimaryComparators
{
    public new const string Equals = "equals";
    public const string NotEquals = "!equals";
    public const string Contains = "contains";
    public const string NotContains = "!contains";
    public const string Matches = "matches";
    public const string NotMatches = "!matches";
    public const string In = "in";
    public const string NotIn = "!in";
    public const string Smaller = "<";
    public const string Bigger = ">";
    public const string DateBefore = "date_before";
    public const string DateAfter = "date_after";
    public const string DateWithinLast = "date_within_last";
    public const string DateNotWithinLast = "!date_within_last";
    public const string StringContains = "string_contains";
    public const string StringNotContains = "!string_contains";

    public static List<string> GetAllComparators() => new()
    {
        Equals, NotEquals, Contains, NotContains, Matches, NotMatches,
        In, NotIn, Smaller, Bigger, DateBefore, DateAfter,
        DateWithinLast, DateNotWithinLast, StringContains, StringNotContains
    };

    public static List<string> GetAllIntComparators() => new()
    {
        Equals, NotEquals, Smaller, Bigger
    };
}

/// <summary>
/// Comparison utilities for comparing values according to different operators.
/// </summary>
public static partial class CompareUtilities
{
    private static readonly Dictionary<string, Func<object?, object?, bool>> Comparators = new()
    {
        [PrimaryComparators.Equals] = CompareEquality,
        ["="] = CompareEquality,
        ["=="] = CompareEquality,
        ["==_any"] = CompareEquality,
        ["=_any"] = CompareEquality,
        
        [PrimaryComparators.NotEquals] = (l, r) => !CompareEquality(l, r),
        ["!="] = (l, r) => !CompareEquality(l, r),
        ["!=_any"] = (l, r) => !CompareEquality(l, r),
        
        [PrimaryComparators.Contains] = CompareContains,
        [PrimaryComparators.NotContains] = (l, r) => !CompareContains(l, r),
        
        [PrimaryComparators.Matches] = CompareMatches,
        [PrimaryComparators.NotMatches] = (l, r) => !CompareMatches(l, r),
        
        [PrimaryComparators.In] = CompareIn,
        [PrimaryComparators.NotIn] = (l, r) => !CompareIn(l, r),
        
        [PrimaryComparators.Smaller] = CompareSmaller,
        ["<_any"] = CompareSmallerAny,
        ["<="] = CompareLessEqual,
        ["=<"] = CompareLessEqual,
        
        [PrimaryComparators.Bigger] = CompareBigger,
        [">_any"] = CompareBiggerAny,
        [">="] = CompareGreaterEqual,
        ["=>"] = CompareGreaterEqual,
        
        [PrimaryComparators.DateBefore] = CompareDateBefore,
        [PrimaryComparators.DateAfter] = CompareDateAfter,
        [PrimaryComparators.DateWithinLast] = CompareDateWithinLast,
        [PrimaryComparators.DateNotWithinLast] = (l, r) => !CompareDateWithinLast(l, r),
        
        [PrimaryComparators.StringContains] = CompareStringContains,
        [PrimaryComparators.StringNotContains] = (l, r) => !CompareStringContains(l, r)
    };

    /// <summary>
    /// Parse a comma-separated string with quoted values support.
    /// </summary>
    public static List<string> ParseCommaSeparatedString(string input)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        bool escaped = false;

        foreach (char c in input)
        {
            if (escaped)
            {
                current.Append(c);
                escaped = false;
            }
            else if (c == '\\')
            {
                escaped = true;
            }
            else if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            result.Add(current.ToString().Trim());

        return result;
    }

    private static int ParseInt(object? value)
    {
        return value switch
        {
            int i => i,
            string s when int.TryParse(s, out var result) => result,
            _ => throw new CompareException($"Cannot convert value '{value}' to integer.")
        };
    }

    private static bool CompareEquality(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        return left.Equals(right);
    }

    private static bool CompareSmaller(object? left, object? right)
    {
        var l = ParseInt(left ?? 0);
        var r = ParseInt(right!);
        return l < r;
    }

    private static bool CompareSmallerAny(object? left, object? right)
    {
        if (left is IComparable lc && right is IComparable rc)
            return lc.CompareTo(rc) < 0;
        throw new CompareException("Values must be comparable");
    }

    private static bool CompareLessEqual(object? left, object? right)
    {
        var l = ParseInt(left ?? 0);
        var r = ParseInt(right!);
        return l <= r;
    }

    private static bool CompareBigger(object? left, object? right)
    {
        var l = ParseInt(left ?? 0);
        var r = ParseInt(right!);
        return l > r;
    }

    private static bool CompareBiggerAny(object? left, object? right)
    {
        if (left is IComparable lc && right is IComparable rc)
            return lc.CompareTo(rc) > 0;
        throw new CompareException("Values must be comparable");
    }

    private static bool CompareGreaterEqual(object? left, object? right)
    {
        var l = ParseInt(left ?? 0);
        var r = ParseInt(right!);
        return l >= r;
    }

    private static bool CompareContains(object? left, object? right)
    {
        if (left is System.Collections.IEnumerable enumerable and not string)
        {
            return enumerable.Cast<object>().Contains(right);
        }
        throw new CompareException($"Left value must be a list, not {left?.GetType().Name ?? "null"}");
    }

    private static bool CompareMatches(object? left, object? right)
    {
        try
        {
            var leftStr = left?.ToString() ?? string.Empty;
            var rightStr = right?.ToString() ?? string.Empty;

            // Check for regex modes like (?i)
            var match = Regex.Match(rightStr, @"^(\(\?[a-zA-Z]+\))(.+)$");
            string pattern;
            if (match.Success && match.Groups.Count == 3)
            {
                pattern = match.Groups[1].Value + "^" + match.Groups[2].Value + "$";
            }
            else
            {
                pattern = "^" + rightStr + "$";
            }

            return Regex.IsMatch(leftStr, pattern);
        }
        catch (Exception ex)
        {
            throw new CompareException($"Error during matching: {ex.Message}");
        }
    }

    private static bool CompareIn(object? left, object? right)
    {
        var leftStr = left?.ToString() ?? string.Empty;
        var rightStr = right?.ToString() ?? string.Empty;
        var values = ParseCommaSeparatedString(rightStr);
        return values.Contains(leftStr);
    }

    private static DateTime GetDateTime(object? value)
    {
        return value switch
        {
            DateTime dt => dt,
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt) => dt,
            string s when s.Length >= 10 => DateTime.Parse(s, CultureInfo.InvariantCulture),
            _ => throw new CompareException($"Invalid date format: {value}. Expected ISO format.")
        };
    }

    private static bool CompareDateBefore(object? left, object? right)
    {
        var leftDt = GetDateTime(left);
        var rightDt = GetDateTime(right);

        if ((leftDt.Kind == DateTimeKind.Unspecified) != (rightDt.Kind == DateTimeKind.Unspecified))
        {
            throw new CompareException("Cannot compare timezone-naive and timezone-aware datetimes.");
        }

        return leftDt < rightDt;
    }

    private static bool CompareDateAfter(object? left, object? right)
    {
        var leftDt = GetDateTime(left);
        var rightDt = GetDateTime(right);

        if ((leftDt.Kind == DateTimeKind.Unspecified) != (rightDt.Kind == DateTimeKind.Unspecified))
        {
            throw new CompareException("Cannot compare timezone-naive and timezone-aware datetimes.");
        }

        return leftDt > rightDt;
    }

    private static bool CompareDateWithinLast(object? left, object? right)
    {
        var dateToCheck = GetDateTime(left);
        if (dateToCheck.Kind == DateTimeKind.Unspecified)
        {
            dateToCheck = DateTime.SpecifyKind(dateToCheck, DateTimeKind.Utc);
        }

        var rightStr = right?.ToString() ?? string.Empty;
        TimeSpan conditionTimeDelta;
        try
        {
            conditionTimeDelta = TimeUtilities.ParseTimeDelta(rightStr);
        }
        catch (Exception ex)
        {
            throw new CompareException(ex.Message);
        }

        var now = DateTime.UtcNow;
        var trueTimeDelta = now - dateToCheck;

        return trueTimeDelta < conditionTimeDelta;
    }

    private static bool CompareStringContains(object? left, object? right)
    {
        if (left is not string text || right is not string substring)
        {
            throw new CompareException($"Expected strings, got {left?.GetType().Name ?? "null"}");
        }

        return text.Contains(substring, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Compare two values according to a comparator.
    /// </summary>
    public static bool CompareValues(object? left, string comparatorName, object? right)
    {
        if (!Comparators.TryGetValue(comparatorName, out var comparator))
        {
            throw new CompareException($"Invalid comparator: {comparatorName}");
        }

        return comparator(left, right);
    }

    /// <summary>
    /// Compare time value against a condition (e.g., "5d", "2h").
    /// </summary>
    public static bool CompareTime(string condition, object timeValue, ILogger? logger = null)
    {
        try
        {
            return CompareValues(timeValue, PrimaryComparators.DateWithinLast, condition);
        }
        catch (CompareException ex)
        {
            logger?.LogDebug("Error during time comparison for condition '{Condition}' and value '{Value}': {Error}",
                condition, timeValue, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Parse a condition string into its components.
    /// </summary>
    public static Condition? ParseCondition(string conditionStr, string? dataType = null)
    {
        conditionStr = conditionStr.Trim();
        if (string.IsNullOrEmpty(conditionStr))
            return null;

        var comparators = dataType == "int"
            ? PrimaryComparators.GetAllIntComparators()
            : PrimaryComparators.GetAllComparators();

        var allComparators = new List<string>();
        allComparators.AddRange(comparators.Select(c => $"'{c}'"));
        allComparators.AddRange(new[] { "==", "!=", "<=", "=<", ">=", "=>", "<", ">", "=" });
        allComparators.AddRange(allComparators.Select(c => c.Replace("'", "")).ToList());

        foreach (var comp in allComparators)
        {
            var parts = conditionStr.Split(new[] { comp }, 2, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                var left = parts[0].Trim();
                var right = parts[1].Trim();
                var comparator = comp.Trim('\'');
                return new Condition(left, comparator, right);
            }
        }

        // No comparator found, assume equals
        return new Condition(string.Empty, PrimaryComparators.Equals, conditionStr.Trim());
    }

    /// <summary>
    /// Compare integers based on a condition string.
    /// </summary>
    public static bool CompareInts(string condition, object value, ILogger? logger = null)
    {
        var parsedCondition = ParseCondition(condition, "int");
        if (parsedCondition == null)
            return false;

        try
        {
            var intValue = Convert.ToInt32(value);
            var conditionValue = Convert.ToInt32(parsedCondition.RightValue);

            return CompareValues(intValue, parsedCondition.Comparator, conditionValue);
        }
        catch (Exception ex)
        {
            logger?.LogDebug("Cannot convert values to integers: {Error}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Compare using a generic key method.
    /// </summary>
    public static bool CompareGeneric(string condition, Func<string, object?> keyMethod, string warning, ILogger? logger = null)
    {
        var parsedCondition = ParseCondition(condition);
        if (parsedCondition == null)
            return false;

        var key = parsedCondition.LeftValue;
        var comparator = parsedCondition.Comparator;
        var rightValue = parsedCondition.RightValue;

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(comparator))
        {
            logger?.LogWarning(warning, condition);
            return false;
        }

        var leftValue = keyMethod(key);
        if (leftValue == null)
        {
            logger?.LogDebug("Key {Key} not found", key);
            return false;
        }

        // Adjust comparator for generic types
        if (comparator is "<" or ">")
        {
            comparator += "_any";
        }

        // Type conversion
        try
        {
            // Try integer conversion
            if (int.TryParse(leftValue.ToString(), out var leftInt) &&
                int.TryParse(rightValue, out var rightInt))
            {
                leftValue = leftInt;
                rightValue = rightInt.ToString();
            }
            // Try datetime conversion
            else if (DateTime.TryParse(leftValue.ToString(), out var leftDate) &&
                     DateTime.TryParse(rightValue, out var rightDate))
            {
                leftValue = leftDate;
                rightValue = rightDate.ToString();
            }
        }
        catch (Exception ex)
        {
            logger?.LogDebug("Type conversion failed: {Error}", ex.Message);
        }

        try
        {
            var result = CompareValues(leftValue, comparator, rightValue);
            logger?.LogDebug("Comparing {Key} {Comparator} {Right} with result {Result}",
                key, comparator, rightValue, result);
            return result;
        }
        catch (CompareException ex)
        {
            logger?.LogDebug("Error during comparison: {Error}", ex.Message);
            return false;
        }
    }
}

/// <summary>
/// Represents a parsed condition.
/// </summary>
/// <param name="LeftValue">Left operand</param>
/// <param name="Comparator">Comparison operator</param>
/// <param name="RightValue">Right operand</param>
public record Condition(string LeftValue, string Comparator, string RightValue);
