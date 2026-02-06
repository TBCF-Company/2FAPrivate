// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// This code is free software: you can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License
// as published by the Free Software Foundation, either
// version 3 of the License, or any later version.

using System.Reflection;

namespace PrivacyIdeaServer.Lib.Utils;

/// <summary>
/// Version information utilities.
/// </summary>
public static class VersionInfo
{
    /// <summary>
    /// Get the privacyIDEA version number.
    /// </summary>
    /// <returns>Version number string</returns>
    public static string GetVersionNumber()
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version?.ToString() ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    /// <summary>
    /// Get the version string displayed in WebUI and self-service portal.
    /// </summary>
    /// <returns>Formatted version string</returns>
    public static string GetVersion()
    {
        var version = GetVersionNumber();
        return $"privacyIDEA {version}";
    }
}
