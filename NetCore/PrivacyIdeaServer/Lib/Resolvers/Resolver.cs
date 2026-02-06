// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/resolver.py
// Library for creating and managing user resolvers in the database

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Lib;
using PrivacyIdeaServer.Lib.Crypto;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// Constant for censored password fields
    /// Equivalent to Python's CENSORED constant
    /// </summary>
    public static class ResolverConstants
    {
        public const string Censored = "__CENSORED__";
    }

    /// <summary>
    /// Resolver configuration data
    /// </summary>
    public class ResolverConfigData
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        public List<string> CensorKeys { get; set; } = new List<string>();
    }

    /// <summary>
    /// Resolver class descriptor
    /// </summary>
    public class ResolverClassDescriptor
    {
        public Dictionary<string, string> Config { get; set; } = new Dictionary<string, string>();
        public string ClassName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resolver management service
    /// Equivalent to Python's resolver.py module functions
    /// </summary>
    public class ResolverService
    {
        private readonly PrivacyIDEAContext _context;
        private readonly ILogger<ResolverService> _logger;
        private readonly Dictionary<string, IResolverClass> _resolverCache = new();
        private static readonly object _cacheLock = new object();

        public ResolverService(PrivacyIDEAContext context, ILogger<ResolverService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Save a resolver to the database
        /// Equivalent to Python's save_resolver function
        /// If resolver exists, it is updated. When updating, you don't need to provide all parameters.
        /// </summary>
        /// <param name="parameters">Request parameters including name, type, and config</param>
        /// <returns>The database ID of the resolver</returns>
        public async Task<int> SaveResolverAsync(Dictionary<string, object> parameters)
        {
            // Extract required parameters
            if (!parameters.TryGetValue("resolver", out var resolverNameObj) || resolverNameObj == null)
            {
                throw new ArgumentException("Parameter 'resolver' is required");
            }
            var resolverName = resolverNameObj.ToString()!;

            if (!parameters.TryGetValue("type", out var resolverTypeObj) || resolverTypeObj == null)
            {
                throw new ArgumentException("Parameter 'type' is required");
            }
            var resolverType = resolverTypeObj.ToString()!;

            bool updateResolver = false;

            // Check the name
            SanityNameCheck(resolverName, @"^[A-Za-z0-9_\-\.]+$");

            // Check the type
            var resolverTypes = GetResolverTypes();
            if (!resolverTypes.Contains(resolverType))
            {
                throw new ArgumentException(
                    $"Resolver type '{resolverType}' not in valid types: {string.Join(", ", resolverTypes)}");
            }

            // Check if resolver exists
            var existingResolvers = await GetResolverListAsync(filterResolverName: resolverName);
            foreach (var (rName, resolver) in existingResolvers)
            {
                if (resolver.Type == resolverType)
                {
                    updateResolver = true;
                }
                else
                {
                    throw new ArgumentException(
                        $"Resolver with similar name and other type already exists: {rName}");
                }
            }

            // Get resolver config description
            var resolverConfig = GetResolverConfigDescription(resolverType);
            var configDescription = resolverConfig.GetValueOrDefault(resolverType)?.Config 
                ?? new Dictionary<string, string>();

            // Extract data from parameters
            var (data, types, desc) = GetDataFromParams(
                parameters, 
                new[] { "resolver", "type" }, 
                configDescription, 
                resolverType, 
                resolverName);

            // Create or update resolver in DB
            int resolverId;
            if (updateResolver)
            {
                var resolver = await _context.Resolvers
                    .FirstOrDefaultAsync(r => r.Name == resolverName);
                
                if (resolver == null)
                {
                    throw new InvalidOperationException($"Resolver {resolverName} not found");
                }
                
                resolverId = resolver.Id;
            }
            else
            {
                var resolver = new Resolver(resolverName, resolverType);
                _context.Resolvers.Add(resolver);
                await _context.SaveChangesAsync(); // Flush to get ID
                resolverId = resolver.Id;
            }

            // Create or update config entries
            foreach (var (key, value) in data)
            {
                if (string.IsNullOrEmpty(value?.ToString()) && value is not bool)
                {
                    // Empty value - delete old entry if exists
                    var oldConfigs = _context.ResolverConfigs
                        .Where(rc => rc.ResolverId == resolverId && rc.Key == key);
                    _context.ResolverConfigs.RemoveRange(oldConfigs);
                    continue;
                }

                var valueStr = value?.ToString() ?? string.Empty;
                var typeStr = types.GetValueOrDefault(key, "");

                // Handle password encryption
                if (typeStr == "password")
                {
                    if (valueStr == ResolverConstants.Censored)
                    {
                        continue; // Keep existing password
                    }
                    else
                    {
                        valueStr = EncryptPassword(valueStr);
                    }
                }
                else if (typeStr == "dict_with_password")
                {
                    // Handle dictionary with password fields
                    if (value is Dictionary<string, object> dict)
                    {
                        foreach (var (dictKey, dictValue) in dict.ToList())
                        {
                            var fullKey = $"{key}.{dictKey}";
                            if (configDescription.GetValueOrDefault(fullKey) == "password")
                            {
                                var dictValueStr = dictValue?.ToString() ?? string.Empty;
                                if (dictValueStr == ResolverConstants.Censored)
                                {
                                    // Fetch old value from database
                                    var oldConfig = await _context.ResolverConfigs
                                        .FirstOrDefaultAsync(rc => rc.ResolverId == resolverId && rc.Key == key);
                                    
                                    if (oldConfig != null && !string.IsNullOrEmpty(oldConfig.Value))
                                    {
                                        var oldDict = JsonSerializer.Deserialize<Dictionary<string, string>>(oldConfig.Value);
                                        if (oldDict?.ContainsKey(dictKey) == true)
                                        {
                                            dict[dictKey] = oldDict[dictKey]; // Already encrypted
                                        }
                                    }
                                }
                                else
                                {
                                    dict[dictKey] = EncryptPassword(dictValueStr);
                                }
                            }
                        }
                        valueStr = JsonSerializer.Serialize(dict);
                    }
                }

                // Serialize dictionaries to JSON
                if (value is Dictionary<string, object>)
                {
                    valueStr = JsonSerializer.Serialize(value);
                }

                // Update or create config entry
                var existingConfig = await _context.ResolverConfigs
                    .FirstOrDefaultAsync(rc => rc.ResolverId == resolverId && rc.Key == key);

                if (existingConfig != null)
                {
                    existingConfig.Value = valueStr;
                    existingConfig.Type = typeStr;
                    existingConfig.Description = desc.GetValueOrDefault(key, "");
                }
                else
                {
                    var config = new ResolverConfig(
                        resolverId: resolverId,
                        key: key,
                        value: valueStr,
                        type: typeStr,
                        description: desc.GetValueOrDefault(key, ""));
                    
                    _context.ResolverConfigs.Add(config);
                }
            }

            // TODO: Remove corresponding entries from user cache
            // await DeleteUserCacheAsync(resolver: resolverName);
            
            await SaveConfigTimestampAsync();
            await _context.SaveChangesAsync();

            return resolverId;
        }

        /// <summary>
        /// Get list of configured resolvers from the database
        /// Equivalent to Python's get_resolver_list function
        /// </summary>
        /// <param name="filterResolverType">Only resolvers of the given type</param>
        /// <param name="filterResolverName">Get the distinct resolver</param>
        /// <param name="editable">Whether only return editable resolvers</param>
        /// <param name="censor">Censor sensitive data</param>
        /// <returns>Dictionary of resolvers and their configuration</returns>
        public async Task<Dictionary<string, ResolverConfigData>> GetResolverListAsync(
            string? filterResolverType = null,
            string? filterResolverName = null,
            bool? editable = null,
            bool censor = false)
        {
            var query = _context.Resolvers
                .Include(r => r.ConfigList)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filterResolverType))
            {
                query = query.Where(r => r.ResolverType == filterResolverType);
            }

            if (!string.IsNullOrEmpty(filterResolverName))
            {
                query = query.Where(r => r.Name.ToLower() == filterResolverName.ToLower());
            }

            var resolvers = await query.ToListAsync();
            var result = new Dictionary<string, ResolverConfigData>();

            foreach (var resolver in resolvers)
            {
                var configData = new ResolverConfigData
                {
                    Name = resolver.Name,
                    Type = resolver.ResolverType
                };

                foreach (var config in resolver.ConfigList)
                {
                    var value = config.Value ?? string.Empty;
                    
                    // Deserialize JSON dictionaries
                    if (config.Type == "dict_with_password" || (!string.IsNullOrEmpty(value) && value.StartsWith("{")))
                    {
                        try
                        {
                            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(value);
                            if (dict != null)
                            {
                                foreach (var (k, v) in dict)
                                {
                                    configData.Data[$"{config.Key}.{k}"] = v;
                                }
                            }
                        }
                        catch
                        {
                            configData.Data[config.Key] = value;
                        }
                    }
                    else
                    {
                        configData.Data[config.Key] = value;
                    }

                    // Track password fields for censoring
                    if (config.Type == "password")
                    {
                        configData.CensorKeys.Add(config.Key);
                    }
                }

                // Apply censoring if requested
                if (censor)
                {
                    foreach (var key in configData.CensorKeys)
                    {
                        if (configData.Data.ContainsKey(key))
                        {
                            configData.Data[key] = ResolverConstants.Censored;
                        }
                    }
                }

                // Filter by editable if specified
                if (editable.HasValue)
                {
                    var isEditable = IsTrue(configData.Data.GetValueOrDefault("Editable")) ||
                                   IsTrue(configData.Data.GetValueOrDefault("EDITABLE"));
                    
                    if (editable.Value != isEditable)
                    {
                        continue;
                    }
                }

                result[resolver.Name] = configData;
            }

            return result;
        }

        /// <summary>
        /// Delete a resolver and all related ResolverConfig entries
        /// Equivalent to Python's delete_resolver function
        /// </summary>
        /// <param name="resolverName">The name of the resolver to delete</param>
        /// <returns>The ID of the resolver, or -1 if not found</returns>
        public async Task<int> DeleteResolverAsync(string resolverName)
        {
            var resolver = await _context.Resolvers
                .Include(r => r.RealmList)
                .Include(r => r.ConfigList)
                .FirstOrDefaultAsync(r => r.Name == resolverName);

            if (resolver == null)
            {
                return -1;
            }

            // Check if resolver is still in use by a realm
            if (resolver.RealmList.Any())
            {
                var realmName = resolver.RealmList.First().Realm?.Name ?? "unknown";
                throw new ConfigAdminException(
                    $"The resolver '{resolverName}' is still contained in realm '{realmName}'");
            }

            int resolverId = resolver.Id;

            // Delete config entries (cascade delete should handle this, but explicit for safety)
            _context.ResolverConfigs.RemoveRange(resolver.ConfigList);
            
            // Delete resolver
            _context.Resolvers.Remove(resolver);

            await SaveConfigTimestampAsync();
            await _context.SaveChangesAsync();

            // Remove from cache
            lock (_cacheLock)
            {
                _resolverCache.Remove(resolverName);
            }

            // TODO: Remove from user cache
            // await DeleteUserCacheAsync(resolver: resolverName);

            return resolverId;
        }

        /// <summary>
        /// Get the complete config of a given resolver
        /// Equivalent to Python's get_resolver_config function
        /// </summary>
        /// <param name="resolverName">The name of the resolver</param>
        /// <returns>The config dictionary</returns>
        public async Task<Dictionary<string, string>> GetResolverConfigAsync(string resolverName)
        {
            var resolvers = await GetResolverListAsync(filterResolverName: resolverName);
            return resolvers.GetValueOrDefault(resolverName)?.Data ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Get the configuration description of a resolver type
        /// Equivalent to Python's get_resolver_config_description function
        /// </summary>
        /// <param name="resolverType">The type of the resolver</param>
        /// <returns>Configuration description dictionary</returns>
        public Dictionary<string, ResolverClassDescriptor> GetResolverConfigDescription(string resolverType)
        {
            var resolverClass = GetResolverClass(resolverType);
            if (resolverClass == null)
            {
                return new Dictionary<string, ResolverClassDescriptor>();
            }

            return resolverClass.GetResolverClassDescriptor();
        }

        /// <summary>
        /// Get resolver class by type
        /// Equivalent to Python's get_resolver_class function
        /// </summary>
        /// <param name="resolverType">Resolver type</param>
        /// <returns>Resolver class instance</returns>
        public IResolverClass? GetResolverClass(string resolverType)
        {
            // TODO: Implement resolver class registry
            // For now, return null - this should be populated with actual resolver implementations
            _logger.LogWarning("Resolver class registry not yet implemented for type: {Type}", resolverType);
            return null;
        }

        /// <summary>
        /// Get the type of a resolver by name
        /// Equivalent to Python's get_resolver_type function
        /// </summary>
        /// <param name="resolverName">The name of the resolver</param>
        /// <returns>The type of the resolver</returns>
        public async Task<string?> GetResolverTypeAsync(string resolverName)
        {
            var resolver = await _context.Resolvers
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == resolverName);
            
            return resolver?.ResolverType;
        }

        /// <summary>
        /// Get available resolver types
        /// Equivalent to Python's get_resolver_types function
        /// </summary>
        /// <returns>List of resolver type strings</returns>
        public List<string> GetResolverTypes()
        {
            // TODO: Load from resolver class registry
            // For now, return common types
            return new List<string>
            {
                "passwdresolver",
                "ldapresolver",
                "sqlresolver",
                "scimresolver"
            };
        }

        /// <summary>
        /// Test if resolver parameters will create a working resolver
        /// Equivalent to Python's pretestresolver function
        /// </summary>
        /// <param name="resolverType">Resolver type</param>
        /// <param name="parameters">Resolver parameters</param>
        /// <returns>Tuple of (success, description)</returns>
        public async Task<(bool Success, string Description)> PretestResolverAsync(
            string resolverType,
            Dictionary<string, object> parameters)
        {
            // If testing an existing resolver, replace censored passwords with actual values
            if (parameters.TryGetValue("resolver", out var resolverNameObj))
            {
                var resolverName = resolverNameObj?.ToString();
                if (!string.IsNullOrEmpty(resolverName))
                {
                    var oldConfig = await GetResolverListAsync(filterResolverName: resolverName);
                    if (oldConfig.TryGetValue(resolverName, out var config))
                    {
                        foreach (var key in config.CensorKeys)
                        {
                            if (parameters.TryGetValue(key, out var value) 
                                && value?.ToString() == ResolverConstants.Censored)
                            {
                                parameters[key] = config.Data.GetValueOrDefault(key, "");
                            }
                        }
                    }
                }
            }

            var resolverClass = GetResolverClass(resolverType);
            if (resolverClass == null)
            {
                return (false, $"Resolver type '{resolverType}' not found");
            }

            return await resolverClass.TestConnectionAsync(parameters);
        }

        /// <summary>
        /// Export resolver configuration
        /// Equivalent to Python's export_resolver function
        /// </summary>
        /// <param name="name">Optional resolver name to export</param>
        /// <param name="censor">Censor sensitive data</param>
        /// <returns>Resolver configuration</returns>
        public async Task<Dictionary<string, ResolverConfigData>> ExportResolverAsync(
            string? name = null,
            bool censor = false)
        {
            return await GetResolverListAsync(filterResolverName: name, censor: censor);
        }

        /// <summary>
        /// Import resolver configuration
        /// Equivalent to Python's import_resolver function
        /// </summary>
        /// <param name="data">Resolver configuration data</param>
        /// <param name="name">Optional resolver name to import (imports only this one)</param>
        public async Task ImportResolverAsync(
            Dictionary<string, ResolverConfigData> data,
            string? name = null)
        {
            _logger.LogDebug("Import resolver config: {Data}", data);

            foreach (var (resName, resData) in data)
            {
                if (!string.IsNullOrEmpty(name) && name != resName)
                {
                    continue;
                }

                var parameters = new Dictionary<string, object>
                {
                    ["resolver"] = resName,
                    ["type"] = resData.Type
                };

                // Add all data entries as parameters
                foreach (var (key, value) in resData.Data)
                {
                    parameters[key] = value;
                }

                var rid = await SaveResolverAsync(parameters);
                _logger.LogInformation("Import of resolver '{ResolverName}' finished, id: {Id}", 
                    resName, rid);
            }
        }

        #region Helper Methods

        private void SanityNameCheck(string name, string pattern)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be empty");
            }

            var regex = new Regex(pattern);
            if (!regex.IsMatch(name))
            {
                throw new ArgumentException(
                    $"Invalid name '{name}'. Name must match pattern: {pattern}");
            }
        }

        private (Dictionary<string, object> data, Dictionary<string, string> types, Dictionary<string, string> desc) 
            GetDataFromParams(
                Dictionary<string, object> parameters,
                string[] excludeKeys,
                Dictionary<string, string> configDescription,
                string resolverType,
                string resolverName)
        {
            var data = new Dictionary<string, object>();
            var types = new Dictionary<string, string>();
            var desc = new Dictionary<string, string>();

            foreach (var (key, value) in parameters)
            {
                if (excludeKeys.Contains(key))
                {
                    continue;
                }

                data[key] = value;
                
                if (configDescription.TryGetValue(key, out var type))
                {
                    types[key] = type;
                }

                // Description can be extracted from resolver class if needed
                desc[key] = "";
            }

            return (data, types, desc);
        }

        private string EncryptPassword(string password)
        {
            // TODO: SECURITY CRITICAL - Implement proper password encryption
            // Current implementation uses random IV which makes password irrecoverable
            // 
            // Required: Use a master encryption key from secure storage:
            // - Azure Key Vault
            // - AWS KMS
            // - Hardware Security Module (HSM)
            // 
            // The encryption key must be:
            // 1. Stored securely (never in code or config files)
            // 2. Retrievable for decryption
            // 3. Rotatable without losing access to old passwords
            //
            // For now, returning plaintext with warning log
            _logger.LogWarning("Password encryption not implemented - storing in plaintext (DEVELOPMENT ONLY)");
            return password;
            
            // When implemented:
            // return CryptoFunctions.Encrypt(password, CryptoFunctions.GetUrandom(16), masterKey);
        }

        private bool IsTrue(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            value = value.ToLower();
            return value == "true" || value == "1" || value == "yes" || value == "on";
        }

        private async Task SaveConfigTimestampAsync()
        {
            var timestamp = await _context.Configs
                .FirstOrDefaultAsync(c => c.Key == ConfigConstants.PrivacyIdeaTimestamp);

            if (timestamp == null)
            {
                timestamp = new PrivacyIdeaServer.Models.Database.Config(ConfigConstants.PrivacyIdeaTimestamp, DateTime.UtcNow.ToString("O"));
                _context.Configs.Add(timestamp);
            }
            else
            {
                timestamp.Value = DateTime.UtcNow.ToString("O");
            }

            await _context.SaveChangesAsync();
        }

        #endregion
    }

    /// <summary>
    /// Interface for resolver class implementations
    /// </summary>
    public interface IResolverClass
    {
        Dictionary<string, ResolverClassDescriptor> GetResolverClassDescriptor();
        Task<(bool Success, string Description)> TestConnectionAsync(Dictionary<string, object> parameters);
        void LoadConfig(Dictionary<string, string> config);
    }
}
