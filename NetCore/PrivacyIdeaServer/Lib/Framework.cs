//  2018-11-15   Friedrich Weber <friedrich.weber@netknights.it>
//               Add a framework module to reduce the coupling to flask
//  Converted to C# .NET Core 8 - 2026-02-05
//
// This code is free software; you can redistribute it and/or
// modify it under the terms of the GNU AFFERO GENERAL PUBLIC LICENSE
// License as published by the Free Software Foundation; either
// version 3 of the License, or any later version.
//
// This code is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU AFFERO GENERAL PUBLIC LICENSE for more details.
//
// You should have received a copy of the GNU Affero General Public
// License along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;

namespace PrivacyIdeaServer.Lib;

/// <summary>
/// Framework utilities to manage application-level and request-level storage.
/// Reduces coupling to specific web framework.
/// </summary>
public static class Framework
{
    // Application-level store (shared among all requests/threads)
    private static readonly ConcurrentDictionary<string, object> _appLocalStore = new();

    // Thread-local store for request-specific data
    private static readonly AsyncLocal<Dictionary<string, object>> _requestLocalStore = new();

    /// <summary>
    /// Get a dictionary which is local to the current application,
    /// but shared among all threads.
    /// </summary>
    /// <returns>A thread-safe dictionary</returns>
    public static ConcurrentDictionary<string, object> GetAppLocalStore()
    {
        return _appLocalStore;
    }

    /// <summary>
    /// Get a dictionary which is local to the current request. Thus, it
    /// is not shared among threads.
    /// </summary>
    /// <returns>A request-local dictionary</returns>
    public static Dictionary<string, object> GetRequestLocalStore()
    {
        if (_requestLocalStore.Value == null)
        {
            _requestLocalStore.Value = new Dictionary<string, object>();
        }
        return _requestLocalStore.Value;
    }

    /// <summary>
    /// Get a specific configuration option from the app local store.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if key not found</param>
    /// <returns>Configuration value or default</returns>
    public static T? GetAppConfigValue<T>(string key, T? defaultValue = default)
    {
        if (_appLocalStore.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Set a configuration option in the app local store.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    public static void SetAppConfigValue(string key, object value)
    {
        _appLocalStore[key] = value;
    }

    /// <summary>
    /// Clear the request-local store (typically called at the end of a request).
    /// </summary>
    public static void ClearRequestLocalStore()
    {
        _requestLocalStore.Value?.Clear();
    }
}
