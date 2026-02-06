// SPDX-FileCopyrightText: (C) 2014-2025 Cornelius Kölbel <cornelius@privacyidea.org>
// SPDX-FileCopyrightText: (C) 2010-2014 LSE Leading Security Experts GmbH
// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/user.py
// User management library functions
// Dependencies: resolver and realm libraries
// No dependencies on token functions or web services

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;
using PrivacyIdeaServer.Lib.Realms;
using PrivacyIdeaServer.Lib.Resolvers;

namespace PrivacyIdeaServer.Lib.Users
{
    /// <summary>
    /// Represents a user identity with login, realm, and resolver information.
    /// A user is typically identified as "login@realm".
    /// </summary>
    public class UserIdentity
    {
        private readonly ILogger<UserIdentity> _logger;
        private readonly PrivacyIDEAContext _context;
        private readonly RealmService _realmService;
        private readonly Dictionary<string, bool> _passwordVerificationCache = new();

        public string LoginName { get; private set; } = string.Empty;
        public string DisplayLoginName { get; private set; } = string.Empty;
        public string RealmName { get; private set; } = string.Empty;
        public string ResolverName { get; private set; } = string.Empty;
        public string? UserId { get; private set; }
        public string? ResolverTypeName { get; private set; }
        public int? RealmDatabaseId { get; private set; }

        /// <summary>
        /// Creates a new user identity instance
        /// </summary>
        public UserIdentity(
            PrivacyIDEAContext context,
            ILogger<UserIdentity> logger,
            RealmService realmService,
            string login = "",
            string realm = "",
            string resolver = "",
            string? uid = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _realmService = realmService ?? throw new ArgumentNullException(nameof(realmService));

            LoginName = login?.Trim() ?? string.Empty;
            DisplayLoginName = LoginName;
            RealmName = (realm ?? string.Empty).ToLowerInvariant();
            ResolverName = (resolver == "**") ? string.Empty : (resolver ?? string.Empty);
            UserId = uid;
            RealmDatabaseId = null;

            if (string.IsNullOrEmpty(LoginName) && string.IsNullOrEmpty(ResolverName) && uid != null)
            {
                throw new UserError("Cannot instantiate user from uid without specifying resolver");
            }
        }

        /// <summary>
        /// Initializes user data by querying the user store
        /// </summary>
        public async Task InitializeFromStoreAsync(IUserIdResolver? resolverInstance = null)
        {
            if (string.IsNullOrEmpty(ResolverName))
            {
                await DiscoverResolverAsync();
            }

            if (!string.IsNullOrEmpty(ResolverName))
            {
                var resolver = resolverInstance ?? await GetResolverInstanceAsync(ResolverName);
                if (resolver == null)
                {
                    throw new UserError($"Resolver '{ResolverName}' does not exist");
                }

                if (string.IsNullOrEmpty(UserId))
                {
                    UserId = await resolver.GetUserIdAsync(LoginName);
                }

                if (string.IsNullOrEmpty(LoginName) && !string.IsNullOrEmpty(UserId))
                {
                    DisplayLoginName = LoginName = await resolver.GetUsernameAsync(UserId);
                }

                if (resolver.HasMultipleLoginNames && !string.IsNullOrEmpty(UserId))
                {
                    LoginName = await resolver.GetUsernameAsync(UserId);
                }

                ResolverTypeName = resolver.GetResolverType();
                RealmDatabaseId = await _realmService.GetRealmIdAsync(RealmName);
            }
        }

        /// <summary>
        /// Checks if the user identity is empty (no login and no realm)
        /// </summary>
        public bool HasNoIdentity()
        {
            return string.IsNullOrEmpty(LoginName) && string.IsNullOrEmpty(RealmName);
        }

        /// <summary>
        /// Validates that the user exists in the configured stores
        /// </summary>
        public bool ValidateExistence()
        {
            return !string.IsNullOrEmpty(UserId) && RealmDatabaseId.HasValue;
        }

        /// <summary>
        /// Retrieves detailed user information from the resolver
        /// </summary>
        public async Task<Dictionary<string, object>> FetchUserDetailsAsync()
        {
            if (HasNoIdentity() || !ValidateExistence())
            {
                return new Dictionary<string, object>();
            }

            if (string.IsNullOrEmpty(UserId))
            {
                return new Dictionary<string, object>();
            }

            var resolver = await GetResolverInstanceAsync(ResolverName);
            if (resolver == null)
            {
                return new Dictionary<string, object>();
            }

            var userDetails = await resolver.GetUserInfoAsync(UserId);
            
            var customAttrs = await FetchCustomAttributesAsync();
            foreach (var attr in customAttrs)
            {
                userDetails[attr.Key] = attr.Value;
            }

            return userDetails;
        }

        /// <summary>
        /// Stores a custom attribute for this user
        /// </summary>
        public async Task<int> StoreCustomAttributeAsync(string attributeKey, string attributeValue, string? attributeType = null)
        {
            var existingAttr = await _context.CustomUserAttributes
                .FirstOrDefaultAsync(a =>
                    a.Username == UserId &&
                    a.Resolver == ResolverName &&
                    a.Key == attributeKey);

            if (existingAttr != null)
            {
                existingAttr.Value = attributeValue;
                await _context.SaveChangesAsync();
                return existingAttr.Id;
            }
            else
            {
                var newAttr = new CustomUserAttribute
                {
                    Username = UserId ?? string.Empty,
                    Resolver = ResolverName,
                    Key = attributeKey,
                    Value = attributeValue
                };

                _context.CustomUserAttributes.Add(newAttr);
                await _context.SaveChangesAsync();
                return newAttr.Id;
            }
        }

        /// <summary>
        /// Fetches custom attributes for this user
        /// </summary>
        public async Task<Dictionary<string, object>> FetchCustomAttributesAsync()
        {
            if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(ResolverName))
            {
                return new Dictionary<string, object>();
            }

            var attributes = await _context.CustomUserAttributes
                .Where(a => a.Username == UserId && a.Resolver == ResolverName)
                .ToListAsync();

            return attributes.ToDictionary(a => a.Key, a => (object)(a.Value ?? string.Empty));
        }

        /// <summary>
        /// Removes a custom attribute (or all if key is null)
        /// </summary>
        public async Task<int> RemoveCustomAttributeAsync(string? attributeKey = null)
        {
            var query = _context.CustomUserAttributes
                .Where(a => a.Username == UserId && a.Resolver == ResolverName);

            if (!string.IsNullOrEmpty(attributeKey))
            {
                query = query.Where(a => a.Key == attributeKey);
            }

            var itemsToRemove = await query.ToListAsync();
            _context.CustomUserAttributes.RemoveRange(itemsToRemove);
            await _context.SaveChangesAsync();

            return itemsToRemove.Count;
        }

        /// <summary>
        /// Retrieves phone number(s) for the user
        /// </summary>
        public async Task<object> GetPhoneNumberAsync(string phoneCategory = "phone", int? indexPosition = null)
        {
            var userDetails = await FetchUserDetailsAsync();
            
            if (!userDetails.ContainsKey(phoneCategory))
            {
                _logger.LogWarning("User {User} has no phone of category {Category}", this, phoneCategory);
                return string.Empty;
            }

            var phoneData = userDetails[phoneCategory];

            if (phoneData is List<object> phoneList && indexPosition.HasValue)
            {
                if (indexPosition.Value < phoneList.Count)
                {
                    return phoneList[indexPosition.Value];
                }
                else
                {
                    _logger.LogWarning("User {User} does not have phone at index {Index}", this, indexPosition);
                    return string.Empty;
                }
            }

            return phoneData ?? string.Empty;
        }

        /// <summary>
        /// Returns all realms this user belongs to
        /// </summary>
        public async Task<List<string>> GetAssociatedRealmsAsync()
        {
            var results = new List<string>();
            var allRealms = await _realmService.GetRealmsAsync();

            if (string.IsNullOrEmpty(RealmName) && string.IsNullOrEmpty(ResolverName))
            {
                var defaultRealm = await _realmService.GetDefaultRealmAsync();
                if (!string.IsNullOrEmpty(defaultRealm))
                {
                    results.Add(defaultRealm.ToLowerInvariant());
                    RealmName = defaultRealm.ToLowerInvariant();
                }
            }
            else if (!string.IsNullOrEmpty(RealmName))
            {
                results.Add(RealmName.ToLowerInvariant());
            }
            else if (!string.IsNullOrEmpty(ResolverName))
            {
                foreach (var realmEntry in allRealms)
                {
                    var realmResolvers = realmEntry.Value.Resolvers;
                    if (realmResolvers.Any(r => r.Name == ResolverName))
                    {
                        results.Add(realmEntry.Key.ToLowerInvariant());
                        _logger.LogDebug("Added realm {Realm} for resolver {Resolver}", realmEntry.Key, ResolverName);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Verifies user password against the user store
        /// </summary>
        public async Task<string?> VerifyPasswordAsync(string password)
        {
            string? authenticatedIdentity = null;
            var passwordHash = ComputePasswordHash(password);

            try
            {
                _logger.LogInformation("User {Login} from realm {Realm} attempting authentication", LoginName, RealmName);

                if (_passwordVerificationCache.TryGetValue(passwordHash, out bool cachedResult))
                {
                    if (cachedResult)
                    {
                        authenticatedIdentity = $"{LoginName}@{RealmName}";
                        _logger.LogDebug("Authenticated {User} from cache", this);
                    }
                    else
                    {
                        _logger.LogInformation("User {User} failed authentication from cache", this);
                    }
                    return authenticatedIdentity;
                }

                var resolvers = await GetApplicableResolversAsync();
                
                if (resolvers.Count == 1)
                {
                    var resolver = await GetResolverInstanceAsync(ResolverName);
                    if (resolver != null && !string.IsNullOrEmpty(UserId))
                    {
                        bool isValid = await resolver.CheckPassAsync(UserId, password);
                        
                        if (isValid)
                        {
                            authenticatedIdentity = $"{LoginName}@{RealmName}";
                            _logger.LogDebug("Successfully authenticated {User}", this);
                            _passwordVerificationCache[passwordHash] = true;
                        }
                        else
                        {
                            _logger.LogInformation("User {User} authentication failed", this);
                            _passwordVerificationCache[passwordHash] = false;
                        }
                    }
                }
                else if (resolvers.Count == 0)
                {
                    _logger.LogError("User {User} exists in no resolver", this);
                }
            }
            catch (UserError ex)
            {
                _logger.LogError(ex, "Error verifying username");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password verification");
            }

            return authenticatedIdentity;
        }

        /// <summary>
        /// Retrieves available search fields from the resolver
        /// </summary>
        public async Task<Dictionary<string, Dictionary<string, int>>> GetAvailableSearchFieldsAsync()
        {
            var searchFields = new Dictionary<string, Dictionary<string, int>>();
            var resolvers = await GetApplicableResolversAsync();

            foreach (var resolverName in resolvers)
            {
                try
                {
                    var resolver = await GetResolverInstanceAsync(resolverName);
                    if (resolver != null)
                    {
                        searchFields[resolverName] = resolver.Fields;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting search fields for resolver {Resolver}", resolverName);
                }
            }

            return searchFields;
        }

        /// <summary>
        /// Updates user information in the resolver
        /// </summary>
        public async Task<bool> ModifyUserInformationAsync(Dictionary<string, object> modifications, string? newPassword = null)
        {
            if (newPassword != null)
            {
                modifications["password"] = newPassword;
            }

            bool operationSucceeded = false;

            try
            {
                _logger.LogInformation("Updating user info for {Login}@{Realm}", LoginName, RealmName);
                
                var resolvers = await GetApplicableResolversAsync();
                
                if (resolvers.Count == 1)
                {
                    var resolver = await GetResolverInstanceAsync(ResolverName);
                    if (resolver == null)
                    {
                        return false;
                    }

                    if (!resolver.Updateable)
                    {
                        _logger.LogWarning("Resolver {Resolver} is not updateable", ResolverName);
                        return false;
                    }

                    if (!string.IsNullOrEmpty(UserId))
                    {
                        operationSucceeded = await resolver.UpdateUserAsync(UserId, modifications);
                        
                        if (operationSucceeded)
                        {
                            await ClearUserCacheEntriesAsync(LoginName, ResolverName);
                            
                            if (modifications.TryGetValue("username", out var newUsername))
                            {
                                LoginName = newUsername.ToString() ?? LoginName;
                            }
                            
                            _logger.LogInformation("Successfully updated user {User}", this);
                        }
                        else
                        {
                            _logger.LogInformation("Failed to update user {User}", this);
                        }
                    }
                }
                else if (resolvers.Count == 0)
                {
                    _logger.LogError("User {User} exists in no resolver", this);
                }
            }
            catch (UserError ex)
            {
                _logger.LogError(ex, "Error updating user");
            }

            return operationSucceeded;
        }

        /// <summary>
        /// Removes the user from the resolver
        /// </summary>
        public async Task<bool> RemoveUserAsync()
        {
            bool operationSucceeded = false;

            try
            {
                _logger.LogInformation("Deleting user {Login}@{Realm}", LoginName, RealmName);
                
                var resolvers = await GetApplicableResolversAsync();
                
                if (resolvers.Count == 1)
                {
                    var resolver = await GetResolverInstanceAsync(ResolverName);
                    if (resolver == null)
                    {
                        return false;
                    }

                    if (!resolver.Updateable)
                    {
                        _logger.LogWarning("Resolver {Resolver} is not updateable", ResolverName);
                        return false;
                    }

                    if (!string.IsNullOrEmpty(UserId))
                    {
                        operationSucceeded = await resolver.DeleteUserAsync(UserId);
                        
                        if (operationSucceeded)
                        {
                            _logger.LogInformation("Successfully deleted user {User}", this);
                            await ClearUserCacheEntriesAsync(LoginName, ResolverName);
                        }
                        else
                        {
                            _logger.LogInformation("Failed to delete user {User}", this);
                        }
                    }
                }
                else if (resolvers.Count == 0)
                {
                    _logger.LogError("User {User} exists in no resolver", this);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
            }

            return operationSucceeded;
        }

        /// <summary>
        /// Exports user data for token reassignment after import
        /// </summary>
        public async Task<Dictionary<string, object>> ExportUserDataAsync()
        {
            var exportData = new Dictionary<string, object>
            {
                ["login"] = LoginName,
                ["realm"] = RealmName,
                ["resolver"] = ResolverName,
                ["uid"] = UserId ?? string.Empty,
                ["custom_attributes"] = await FetchCustomAttributesAsync()
            };

            return exportData;
        }

        public override string ToString()
        {
            if (HasNoIdentity())
            {
                return "<empty user>";
            }

            var resolverPart = string.IsNullOrEmpty(ResolverName) ? string.Empty : $".{ResolverName}";
            return $"<{LoginName}{resolverPart}@{RealmName}>";
        }

        public override bool Equals(object? obj)
        {
            if (obj is not UserIdentity other)
            {
                _logger.LogInformation("Comparing non-user object: {This} != {Other}", this, obj?.GetType());
                return false;
            }

            if (ResolverName != other.ResolverName || RealmName != other.RealmName)
            {
                _logger.LogInformation("Users not in same resolver/realm: {This} != {Other}", this, other);
                return false;
            }

            if (!string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(other.UserId))
            {
                _logger.LogDebug("Comparing by uid: {ThisUid} vs {OtherUid}", UserId, other.UserId);
                return UserId == other.UserId;
            }

            _logger.LogDebug("Comparing by login: {ThisLogin} vs {OtherLogin}", LoginName, other.LoginName);
            return LoginName == other.LoginName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GetType(), LoginName, ResolverName, RealmName);
        }

        // Private helper methods

        private async Task DiscoverResolverAsync()
        {
            var orderedResolvers = await GetOrderedResolverListAsync();
            
            foreach (var resolverName in orderedResolvers)
            {
                if (await TryLocateInResolverAsync(resolverName))
                {
                    break;
                }
            }
        }

        private async Task<bool> TryLocateInResolverAsync(string resolverName)
        {
            var resolver = await GetResolverInstanceAsync(resolverName);
            if (resolver == null)
            {
                _logger.LogInformation("Resolver {Resolver} not found", resolverName);
                return false;
            }

            var foundUserId = await resolver.GetUserIdAsync(LoginName);
            if (!string.IsNullOrEmpty(foundUserId))
            {
                _logger.LogInformation("User {Login} found in resolver {Resolver}", LoginName, resolverName);
                _logger.LogInformation("User id resolved to {UserId}", foundUserId);
                ResolverName = resolverName;
                UserId = foundUserId;
                return true;
            }
            else
            {
                _logger.LogDebug("User {Login} not found in resolver {Resolver}", LoginName, resolverName);
                return false;
            }
        }

        private async Task<List<string>> GetOrderedResolverListAsync()
        {
            var resolverPriorities = new List<(string name, int priority, string? node)>();
            var realmConfig = await _realmService.GetRealmsAsync(RealmName);
            
            if (realmConfig.TryGetValue(RealmName, out var config))
            {
                foreach (var resolver in config.Resolvers)
                {
                    resolverPriorities.Add((
                        resolver.Name,
                        resolver.Priority ?? 1000,
                        resolver.Node
                    ));
                }
            }

            var sortedResolvers = resolverPriorities.OrderBy(r => r.priority).ToList();
            var nodeUuid = Framework.GetAppConfigValue<string>("PI_NODE_UUID");
            
            var filtered = sortedResolvers
                .Where(r => string.IsNullOrEmpty(r.node) || r.node == nodeUuid)
                .Select(r => r.name)
                .Distinct()
                .ToList();

            return filtered;
        }

        private async Task<List<string>> GetApplicableResolversAsync()
        {
            if (!string.IsNullOrEmpty(ResolverName))
            {
                return new List<string> { ResolverName };
            }

            var resolvers = new List<string>();
            var orderedList = await GetOrderedResolverListAsync();
            
            foreach (var resolverName in orderedList)
            {
                if (await TryLocateInResolverAsync(resolverName))
                {
                    break;
                }
            }

            if (!string.IsNullOrEmpty(ResolverName))
            {
                resolvers.Add(ResolverName);
            }

            return resolvers;
        }

        private async Task<IUserIdResolver?> GetResolverInstanceAsync(string resolverName)
        {
            // TODO: Implement resolver factory/registry pattern
            // This should be injected or retrieved from a service
            _logger.LogWarning("Resolver instance retrieval not yet implemented for {Resolver}", resolverName);
            await Task.CompletedTask;
            return null;
        }

        private async Task ClearUserCacheEntriesAsync(string username, string resolver)
        {
            var cacheEntries = await _context.UserCaches
                .Where(uc => uc.Username == username && uc.Resolver == resolver)
                .ToListAsync();

            _context.UserCaches.RemoveRange(cacheEntries);
            await _context.SaveChangesAsync();
        }

        private static string ComputePasswordHash(string password)
        {
            var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Service for user-related operations
    /// </summary>
    public class UserService
    {
        private readonly PrivacyIDEAContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly RealmService _realmService;

        public UserService(
            PrivacyIDEAContext context,
            ILogger<UserService> logger,
            RealmService realmService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _realmService = realmService ?? throw new ArgumentNullException(nameof(realmService));
        }

        /// <summary>
        /// Creates a new user in the specified resolver
        /// </summary>
        public async Task<string?> CreateUserAsync(string resolverName, Dictionary<string, object> userAttributes, string? password = null)
        {
            if (password != null)
            {
                userAttributes["password"] = password;
            }

            // TODO: Get resolver instance and call AddUserAsync
            _logger.LogWarning("User creation not yet fully implemented for resolver {Resolver}", resolverName);
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// Splits username into login and realm parts
        /// Handles formats: user@realm, realm\\user, or user@email@realm
        /// </summary>
        public async Task<(string username, string realm)> SplitUserIdentifierAsync(string fullUsername)
        {
            var trimmedUser = fullUsername.Trim();
            var realmPart = string.Empty;

            var splitAtSignEnabled = true; // TODO: Get from config

            if (splitAtSignEnabled)
            {
                var atParts = trimmedUser.Split('@');
                if (atParts.Length >= 2)
                {
                    var potentialRealm = atParts[^1];
                    if (await _realmService.RealmIsDefinedAsync(potentialRealm))
                    {
                        var lastAtIndex = trimmedUser.LastIndexOf('@');
                        return (trimmedUser[..lastAtIndex], potentialRealm);
                    }
                }
                else
                {
                    var backslashParts = trimmedUser.Split('\\');
                    if (backslashParts.Length >= 2)
                    {
                        var lastBackslashIndex = trimmedUser.LastIndexOf('\\');
                        realmPart = trimmedUser[..lastBackslashIndex];
                        return (trimmedUser[(lastBackslashIndex + 1)..], realmPart);
                    }
                }
            }

            return (trimmedUser, realmPart);
        }

        /// <summary>
        /// Constructs a user from request parameters
        /// </summary>
        public async Task<UserIdentity> GetUserFromParametersAsync(
            Dictionary<string, object> parameters,
            bool isRequired = false)
        {
            var realm = string.Empty;
            var username = GetParameter(parameters, "user", isRequired);

            if (!string.IsNullOrEmpty(username))
            {
                (username, realm) = await SplitUserIdentifierAsync(username);
            }

            if (parameters.TryGetValue("realm", out var realmParam) && realmParam != null)
            {
                realm = realmParam.ToString() ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(username) && string.IsNullOrEmpty(realm))
            {
                realm = await _realmService.GetDefaultRealmAsync() ?? string.Empty;
            }

            var resolverName = GetParameter(parameters, "resolver", false) ?? string.Empty;

            // Create logger through logging factory (assumes we have access via DI)
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var userLogger = loggerFactory.CreateLogger<UserIdentity>();
            var user = new UserIdentity(_context, userLogger, _realmService, username ?? string.Empty, realm, resolverName);
            await user.InitializeFromStoreAsync();

            return user;
        }

        /// <summary>
        /// Queries users from resolvers based on search criteria
        /// </summary>
        public async Task<List<Dictionary<string, object>>> QueryUsersAsync(
            Dictionary<string, object>? searchParams = null,
            UserIdentity? specificUser = null,
            bool includeCustomAttributes = false)
        {
            var results = new List<Dictionary<string, object>>();
            var resolversToSearch = new HashSet<string>();
            var searchCriteria = new Dictionary<string, string> { ["username"] = "*" };

            searchParams ??= new Dictionary<string, object>();

            foreach (var param in searchParams)
            {
                if (param.Key is "realm" or "resolver" or "user" or "username")
                {
                    continue;
                }
                searchCriteria[param.Key] = param.Value?.ToString() ?? string.Empty;
            }

            if (searchParams.TryGetValue("username", out var usernameParam))
            {
                searchCriteria["username"] = usernameParam?.ToString() ?? "*";
            }
            if (searchParams.TryGetValue("user", out var userParam))
            {
                searchCriteria["username"] = userParam?.ToString() ?? "*";
            }

            var paramResolver = GetParameter(searchParams, "resolver", false);
            var paramRealm = GetParameter(searchParams, "realm", false);
            var userResolver = specificUser?.ResolverName;
            var userRealm = specificUser?.RealmName;

            if (!string.IsNullOrEmpty(paramResolver))
            {
                resolversToSearch.Add(paramResolver);
            }
            if (!string.IsNullOrEmpty(userResolver))
            {
                resolversToSearch.Add(userResolver);
            }

            var nodeUuid = Framework.GetAppConfigValue<string>("PI_NODE_UUID");

            foreach (var realmToCheck in new[] { paramRealm, userRealm }.Where(r => !string.IsNullOrEmpty(r)))
            {
                var realmConfig = await _realmService.GetRealmsAsync(realmToCheck);
                if (realmConfig.TryGetValue(realmToCheck!, out var config))
                {
                    foreach (var resolver in config.Resolvers)
                    {
                        if (!string.IsNullOrEmpty(resolver.Name))
                        {
                            if (string.IsNullOrEmpty(resolver.Node) || resolver.Node == nodeUuid)
                            {
                                resolversToSearch.Add(resolver.Name);
                            }
                        }
                    }
                }
            }

            if (resolversToSearch.Count == 0)
            {
                var allRealms = await _realmService.GetRealmsAsync();
                foreach (var realmEntry in allRealms.Values)
                {
                    foreach (var resolver in realmEntry.Resolvers)
                    {
                        if (string.IsNullOrEmpty(resolver.Node) || resolver.Node == nodeUuid)
                        {
                            resolversToSearch.Add(resolver.Name);
                        }
                    }
                }
            }

            foreach (var resolverName in resolversToSearch)
            {
                try
                {
                    _logger.LogDebug("Querying resolver {Resolver}", resolverName);
                    
                    // TODO: Get resolver instance and query users
                    // var resolver = await GetResolverInstance(resolverName);
                    // var userList = await resolver.GetUserListAsync(searchCriteria);
                    
                    // For now, log warning
                    _logger.LogWarning("User list query not yet fully implemented for {Resolver}", resolverName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to query users from resolver {Resolver}", resolverName);
                }
            }

            return results;
        }

        /// <summary>
        /// Gets username for a given user ID and resolver
        /// </summary>
        public async Task<string> GetUsernameAsync(string userId, string resolverName)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return string.Empty;
            }

            // TODO: Check cache first, then query resolver
            _logger.LogWarning("Username lookup not yet fully implemented");
            await Task.CompletedTask;
            return string.Empty;
        }

        /// <summary>
        /// Checks if custom attributes exist in the system
        /// </summary>
        public async Task<bool> HasAnyCustomAttributesAsync()
        {
            return await _context.CustomUserAttributes.AnyAsync();
        }

        private static string? GetParameter(Dictionary<string, object> parameters, string key, bool required)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                return value?.ToString();
            }

            if (required)
            {
                throw new ParameterError($"Required parameter '{key}' is missing");
            }

            return null;
        }
    }
}
