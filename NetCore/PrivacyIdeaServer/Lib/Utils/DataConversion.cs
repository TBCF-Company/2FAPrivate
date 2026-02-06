// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// General data conversion and formatting utilities.
/// </summary>
public static class DataConversion
{
    /// <summary>
    /// Convert an integer string to hexadecimal representation.
    /// Used for converting integer serial numbers of certificates to hex.
    /// </summary>
    /// <param name="serial">An integer string</param>
    /// <returns>Hex formatted string</returns>
    public static string IntToHex(string serial)
    {
        var serialInt = long.Parse(serial);
        var serialHex = serialInt.ToString("X");
        
        // Ensure even number of characters
        if (serialHex.Length % 2 != 0)
        {
            serialHex = "0" + serialHex;
        }
        
        return serialHex;
    }

    /// <summary>
    /// Parse an integer from string, supporting both base10 and base16.
    /// </summary>
    /// <param name="s">String to parse</param>
    /// <param name="defaultValue">Default value if parsing fails</param>
    /// <returns>Parsed integer or default</returns>
    public static int ParseInt(string? s, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(s))
            return defaultValue;

        // Try base 10
        if (int.TryParse(s, out var result))
            return result;

        // Try base 16
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(s[2..], System.Globalization.NumberStyles.HexNumber, null, out result))
                return result;
        }
        else if (int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out result))
        {
            return result;
        }

        return defaultValue;
    }

    /// <summary>
    /// Convert input to a list. Handles lists, sets, and single values.
    /// </summary>
    /// <typeparam name="T">Type of elements</typeparam>
    /// <param name="input">Input value</param>
    /// <returns>List of elements</returns>
    public static List<T> ToList<T>(object? input)
    {
        return input switch
        {
            null => new List<T>(),
            List<T> list => list,
            IEnumerable<T> enumerable => enumerable.ToList(),
            T single => new List<T> { single },
            _ => new List<T>()
        };
    }

    /// <summary>
    /// Truncate a comma-separated list to a maximum length.
    /// Shortens longest entries and marks them with '+'.
    /// </summary>
    /// <param name="data">Comma-separated list</param>
    /// <param name="maxLen">Maximum length</param>
    /// <returns>Shortened string</returns>
    public static string TruncateCommaList(string data, int maxLen)
    {
        var parts = data.Split(',').ToList();

        // Early exit if too many entries
        if (parts.Count >= maxLen)
        {
            var result = string.Join(",", parts)[..maxLen];
            return result[..^1] + "+";
        }

        while (string.Join(",", parts).Length > maxLen)
        {
            var newParts = new List<string>();
            var longest = parts.MaxBy(p => p.Length) ?? string.Empty;

            foreach (var part in parts)
            {
                if (part == longest && part.Length > 2)
                {
                    newParts.Add(part[..^2] + "+");
                }
                else
                {
                    newParts.Add(part);
                }
            }

            parts = newParts;
        }

        return string.Join(",", parts);
    }
}
