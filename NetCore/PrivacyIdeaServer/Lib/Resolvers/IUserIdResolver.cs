// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 NetKnights GmbH

using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// Interface for UserID Resolvers.
    /// A UserID Resolver is responsible for resolving login names to unique user identifiers.
    /// </summary>
    public interface IUserIdResolver
    {
        /// <summary>
        /// Supported search fields and their types
        /// </summary>
        Dictionary<string, int> Fields { get; }

        /// <summary>
        /// Name of the resolver
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Unique identifier for the resolver
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Indicates if the resolver can be configured as editable
        /// </summary>
        bool Updateable { get; }

        /// <summary>
        /// Indicates if this instance of the resolver is configured as editable
        /// </summary>
        bool Editable { get; }

        /// <summary>
        /// Indicates if this resolver has multiple loginname attributes
        /// </summary>
        bool HasMultipleLoginNames { get; }

        /// <summary>
        /// Hook to close down the resolver after one request
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// Get the resolver type
        /// </summary>
        string GetResolverType();

        /// <summary>
        /// Get the resolver class type
        /// </summary>
        string GetResolverClassType();

        /// <summary>
        /// Get the resolver class descriptor
        /// </summary>
        Dictionary<string, object> GetResolverClassDescriptor();

        /// <summary>
        /// Resolve a login name to a user ID
        /// </summary>
        /// <param name="loginName">The login name of the user</param>
        /// <returns>The ID of the user, or empty string if not found</returns>
        Task<string> GetUserIdAsync(string loginName);

        /// <summary>
        /// Get the username/loginname for a given user ID
        /// </summary>
        /// <param name="userId">The user ID in this resolver</param>
        /// <returns>The username</returns>
        Task<string> GetUsernameAsync(string userId);

        /// <summary>
        /// Get user information for a given user ID
        /// </summary>
        /// <param name="userId">ID of the user in the resolver</param>
        /// <returns>Dictionary of user attributes, or empty if not found</returns>
        Task<Dictionary<string, object>> GetUserInfoAsync(string userId);

        /// <summary>
        /// Find user objects that match the search criteria
        /// </summary>
        /// <param name="searchDict">Dictionary with key-value pairs for searching</param>
        /// <returns>List of user dictionaries</returns>
        Task<List<Dictionary<string, object>>> GetUserListAsync(Dictionary<string, string>? searchDict = null);

        /// <summary>
        /// Get the resolver identifier
        /// </summary>
        string GetResolverId();

        /// <summary>
        /// Load configuration into the resolver
        /// </summary>
        /// <param name="config">Configuration dictionary</param>
        Task LoadConfigAsync(Dictionary<string, object> config);

        /// <summary>
        /// Check if a password is valid for a given user ID
        /// </summary>
        /// <param name="uid">The user ID</param>
        /// <param name="password">The password to check (usually cleartext)</param>
        /// <returns>True if password matches, false otherwise</returns>
        Task<bool> CheckPassAsync(string uid, string password);

        /// <summary>
        /// Add a new user to the resolver
        /// </summary>
        /// <param name="attributes">User attributes</param>
        /// <returns>The new user ID, or null if not supported</returns>
        Task<string?> AddUserAsync(Dictionary<string, object>? attributes = null);

        /// <summary>
        /// Delete a user from the resolver
        /// </summary>
        /// <param name="uid">The user ID to delete</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteUserAsync(string uid);

        /// <summary>
        /// Update an existing user
        /// </summary>
        /// <param name="uid">The user ID to update</param>
        /// <param name="attributes">Attributes to update</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateUserAsync(string uid, Dictionary<string, object>? attributes = null);

        /// <summary>
        /// Test if the resolver configuration is valid
        /// </summary>
        /// <param name="param">Configuration parameters to test</param>
        /// <returns>Tuple of success flag and description</returns>
        Task<(bool Success, string Description)> TestConnectionAsync(Dictionary<string, object> param);

        /// <summary>
        /// Get the current configuration of the resolver
        /// </summary>
        Dictionary<string, object> GetConfig();
    }
}
