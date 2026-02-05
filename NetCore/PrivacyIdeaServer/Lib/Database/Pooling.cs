//  2018-08-07   Friedrich Weber <friedrich.weber@netknights.it>
//               Add a shared registry of database connections to
//               properly implement connection pooling
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
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Lib.Database;

/// <summary>
/// This module implements a so-called connection registry which manages
/// database connections used for external SQL databases
/// by the SQL audit module and SQLIdResolver.
/// 
/// There should only be one shared registry per application which is
/// used by all threads. This is necessary to properly implement pooling.
/// 
/// Note: In .NET, Entity Framework Core handles connection pooling automatically,
/// so this is more about managing DbContext instances for different connection strings.
/// </summary>
public interface IConnectionRegistry
{
    /// <summary>
    /// Return the DbContext associated with the key.
    /// </summary>
    /// <param name="key">An arbitrary hashable key (e.g., connection string)</param>
    /// <param name="creator">A function that creates a new DbContext</param>
    /// <returns>A DbContext instance</returns>
    DbContext GetContext(string key, Func<DbContext> creator);
}

/// <summary>
/// A registry which creates a new context for every request.
/// Consequently, contexts are not shared among threads and
/// no pooling is implemented at the application level.
/// (EF Core still handles connection pooling internally)
/// </summary>
public class NullConnectionRegistry : IConnectionRegistry
{
    public DbContext GetContext(string key, Func<DbContext> creator)
    {
        return creator();
    }
}

/// <summary>
/// A registry which holds a dictionary mapping a key to a DbContext factory.
/// 
/// Note: In .NET, we typically don't share DbContext instances across threads
/// as they are not thread-safe. Instead, we register the DbContext in DI
/// and let the framework handle the lifetime.
/// 
/// This implementation provides a way to register multiple connection strings
/// and create contexts as needed.
/// </summary>
public class SharedConnectionRegistry : IConnectionRegistry
{
    private readonly ILogger<SharedConnectionRegistry> _logger;
    private readonly ConcurrentDictionary<string, Func<DbContext>> _contextFactories;
    private readonly object _lock = new();

    public SharedConnectionRegistry(ILogger<SharedConnectionRegistry> logger)
    {
        _logger = logger;
        _contextFactories = new ConcurrentDictionary<string, Func<DbContext>>();
    }

    public DbContext GetContext(string key, Func<DbContext> creator)
    {
        // This method will be called concurrently by multiple threads.
        // We store the factory function, not the context itself,
        // because DbContext is not thread-safe.
        var factory = _contextFactories.GetOrAdd(key, k =>
        {
            _logger.LogInformation("Registering new context factory for key {Key}", k);
            return creator;
        });

        // Create a new context instance using the factory
        return factory();
    }
}

/// <summary>
/// Connection registry manager for the application
/// </summary>
public class ConnectionRegistryManager
{
    private static readonly Lazy<IConnectionRegistry> _instance = new(() =>
    {
        var registryType = Environment.GetEnvironmentVariable("PI_CONNECTION_REGISTRY_CLASS") ?? "null";
        
        return registryType.ToLowerInvariant() switch
        {
            "shared" => new SharedConnectionRegistry(
                LoggerFactory.Create(builder => builder.AddConsole())
                    .CreateLogger<SharedConnectionRegistry>()),
            _ => new NullConnectionRegistry()
        };
    });

    /// <summary>
    /// Get the global connection registry instance
    /// </summary>
    public static IConnectionRegistry Instance => _instance.Value;

    /// <summary>
    /// Shortcut to get a context from the application-global registry
    /// </summary>
    public static DbContext GetContext(string key, Func<DbContext> creator)
    {
        return Instance.GetContext(key, creator);
    }
}
