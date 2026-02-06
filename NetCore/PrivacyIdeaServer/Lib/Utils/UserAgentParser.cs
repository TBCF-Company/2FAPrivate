// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using System.Text.RegularExpressions;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// User agent parsing utilities.
/// </summary>
public static partial class UserAgentParser
{
    [GeneratedRegex(@"^(?<agent>[a-zA-Z0-9_-]+)(/(?<version>\d+[\d.]*))?(\s(?<comment>.*))?")]
    private static partial Regex UserAgentRegex();

    /// <summary>
    /// Extract plugin name and version from a user-agent string.
    /// See https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/User-Agent
    /// </summary>
    /// <param name="userAgent">User-agent string</param>
    /// <returns>Tuple of (agent, version, comment)</returns>
    public static (string Agent, string? Version, string? Comment) GetPluginInfoFromUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return (string.Empty, null, null);

        var match = UserAgentRegex().Match(userAgent);
        if (match.Success)
        {
            return (
                match.Groups["agent"].Value,
                match.Groups["version"].Success ? match.Groups["version"].Value : null,
                match.Groups["comment"].Success ? match.Groups["comment"].Value : null
            );
        }

        return (string.Empty, null, null);
    }

    /// <summary>
    /// Extract computer name from user agent string.
    /// Searches for entries like "ComputerName/Laptop-3324231".
    /// </summary>
    /// <param name="userAgent">User agent string</param>
    /// <param name="customKeys">Custom keys to search for (optional)</param>
    /// <returns>Computer name or null</returns>
    public static string? GetComputerNameFromUserAgent(string? userAgent, List<string>? customKeys = null)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        var keys = new List<string> { "ComputerName", "Hostname", "MachineName", "Windows", "Linux", "Mac" };
        
        if (customKeys != null)
        {
            foreach (var key in customKeys.Where(k => !keys.Contains(k)))
            {
                keys.Add(key);
            }
        }

        foreach (var key in keys)
        {
            if (userAgent.Contains(key))
            {
                try
                {
                    var parts = userAgent.Split(key + "/");
                    if (parts.Length > 1)
                    {
                        var name = parts[1].Split(' ')[0];
                        return name;
                    }
                }
                catch
                {
                    // Continue to next key
                }
            }
        }

        return null;
    }
}
