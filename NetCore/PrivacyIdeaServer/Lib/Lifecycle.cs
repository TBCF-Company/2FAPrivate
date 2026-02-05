//  2018-08-08   Friedrich Weber <friedrich.weber@netknights.it>
//               Add a lifecycle module that allows to
//               register functions to be called after the request
//               has been handled.
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

namespace PrivacyIdeaServer.Lib;

/// <summary>
/// Lifecycle management for request-scoped cleanup functions
/// </summary>
public static class Lifecycle
{
    /// <summary>
    /// Register a function to be called after the request has ended 
    /// (this includes cases in which an error has been thrown)
    /// </summary>
    /// <param name="action">An action that takes no arguments</param>
    public static void RegisterFinalizer(Action action)
    {
        var store = Framework.GetRequestLocalStore();
        
        if (!store.ContainsKey("call_on_teardown"))
        {
            store["call_on_teardown"] = new List<Action>();
        }

        if (store["call_on_teardown"] is List<Action> finalizers)
        {
            finalizers.Add(action);
        }
    }

    /// <summary>
    /// Call all finalizers that have been registered with the current request.
    /// Exceptions will be caught and logged.
    /// </summary>
    /// <param name="logger">Optional logger for warnings</param>
    public static void CallFinalizers(ILogger? logger = null)
    {
        var store = Framework.GetRequestLocalStore();
        
        if (store.TryGetValue("call_on_teardown", out var value) && value is List<Action> finalizers)
        {
            foreach (var func in finalizers)
            {
                try
                {
                    func();
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Caught exception in finalizer: {Message}", ex.Message);
                }
            }
            
            finalizers.Clear();
        }
    }
}
