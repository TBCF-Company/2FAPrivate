// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using Microsoft.Extensions.Logging;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Configuration and parameter parsing utilities.
/// </summary>
public static class ConfigurationParser
{
    /// <summary>
    /// Parse parameters when creating resolvers or CA connectors.
    /// Checks if parameters correspond to the class definition.
    /// </summary>
    /// <param name="params">Input parameters from REST API</param>
    /// <param name="excludeParams">Parameters to exclude (e.g., "resolver", "type")</param>
    /// <param name="configDescription">Description of allowed configuration</param>
    /// <param name="module">Identifier like "resolver" or "CA connector"</param>
    /// <param name="type">The type of the resolver or CA connector</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>Tuple of (data, types, description)</returns>
    public static (Dictionary<string, object> Data, Dictionary<string, string> Types, Dictionary<string, string> Descriptions)
        GetDataFromParams(
            Dictionary<string, object> @params,
            List<string> excludeParams,
            Dictionary<string, string> configDescription,
            string module,
            string type,
            ILogger? logger = null)
    {
        var types = new Dictionary<string, string>();
        var desc = new Dictionary<string, string>();
        var data = new Dictionary<string, object>();

        foreach (var kvp in @params)
        {
            var k = kvp.Key;

            if (excludeParams.Contains(k))
                continue;

            if (k.StartsWith("type."))
            {
                var key = k["type.".Length..];
                types[key] = kvp.Value?.ToString() ?? string.Empty;
            }
            else if (k.StartsWith("desc."))
            {
                var key = k["desc.".Length..];
                desc[key] = kvp.Value?.ToString() ?? string.Empty;
            }
            else
            {
                data[k] = kvp.Value;
                if (configDescription.TryGetValue(k, out var typeDesc))
                {
                    types[k] = typeDesc;
                }
                else
                {
                    logger?.LogWarning("The passed key '{Key}' is not a parameter for the {Module} type '{Type}'", k, module, type);
                }
            }
        }

        // Check that there is no type or desc without the data itself
        bool missing = false;
        foreach (var t in types.Keys)
        {
            if (!data.ContainsKey(t))
                missing = true;
        }

        foreach (var t in desc.Keys)
        {
            if (!data.ContainsKey(t))
                missing = true;
        }

        if (missing)
        {
            throw new ArgumentException($"Type or description without necessary data! {@params}");
        }

        return (data, types, desc);
    }

    /// <summary>
    /// Parse a string formatted like ":key1: valueA valueB :key2: valueC" into a dictionary.
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <param name="splitChar">The character used for splitting (default ':')</param>
    /// <returns>Dictionary with keys and lists of values</returns>
    public static Dictionary<string, List<string>> ParseStringToDict(string s, char splitChar = ':')
    {
        // Create a list like ["key1", "valueA valueB", "key2", "valueC"]
        var packedList = s.Trim()
            .Split(splitChar)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        var keys = new List<string>();
        var valueStrings = new List<string>();

        for (int i = 0; i < packedList.Count; i++)
        {
            if (i % 2 == 0)
                keys.Add(packedList[i]);
            else
                valueStrings.Add(packedList[i]);
        }

        // Create a list of values: [['valueA', 'valueB'], ['valueC']]
        var values = valueStrings
            .Select(v => v.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList())
            .ToList();

        var result = new Dictionary<string, List<string>>();
        for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
        {
            result[keys[i]] = values[i];
        }

        return result;
    }

    /// <summary>
    /// Check if a value represents true.
    /// Returns true if the value is 1, "1", true, "True", "true", or "TRUE".
    /// </summary>
    /// <param name="value">String or integer value</param>
    /// <returns>Boolean</returns>
    public static bool IsTrue(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            int i => i == 1,
            string s => s is "1" or "True" or "true" or "TRUE",
            _ => false
        };
    }
}
