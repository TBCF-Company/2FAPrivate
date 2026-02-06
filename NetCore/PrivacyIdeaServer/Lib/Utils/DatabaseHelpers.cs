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
/// Database helper utilities for caching and connection strings.
/// </summary>
public static partial class DatabaseHelpers
{
    /// <summary>
    /// Check if the configuration database should be reloaded.
    /// Compares cache timestamp with database timestamp.
    /// </summary>
    /// <param name="timestamp">Cache timestamp</param>
    /// <param name="dbTimestamp">Database timestamp value</param>
    /// <returns>True if reload is needed</returns>
    public static bool ReloadDb(DateTime? timestamp, string? dbTimestamp)
    {
        var internalTimestamp = string.Empty;
        if (timestamp.HasValue)
        {
            internalTimestamp = new DateTimeOffset(timestamp.Value).ToUnixTimeSeconds().ToString();
        }

        bool reload = false;

        // Reason to reload
        if (dbTimestamp != null && dbTimestamp.StartsWith("2016-"))
        {
            // Old timestamp in the database
            reload = true;
        }

        if (!timestamp.HasValue || dbTimestamp == null)
        {
            // Values not initialized
            reload = true;
        }

        if (dbTimestamp != null && string.Compare(dbTimestamp, internalTimestamp, StringComparison.Ordinal) >= 0)
        {
            // DB contents is newer
            reload = true;
        }

        return reload;
    }

    /// <summary>
    /// Censor a database connection string by hiding the password.
    /// Replaces password with "***".
    /// </summary>
    /// <param name="connectString">Database connection string</param>
    /// <returns>Censored connection string</returns>
    public static string CensorConnectString(string connectString)
    {
        try
        {
            // Pattern to match password in connection strings
            var censored = PasswordRegex().Replace(connectString, "$1=***");
            return censored;
        }
        catch
        {
            return "<error when censoring connect string>";
        }
    }

    [GeneratedRegex(@"(password|pwd)\s*=\s*([^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordRegex();
}
