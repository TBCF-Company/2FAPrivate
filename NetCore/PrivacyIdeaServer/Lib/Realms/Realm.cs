// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/realm.py
// Library functions to create, modify and delete realms in the database

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Lib;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Lib.Realms
{
    /// <summary>
    /// Realm configuration dictionary structure
    /// </summary>
    public class RealmConfig
    {
        public int Id { get; set; }
        public string? Option { get; set; }
        public bool Default { get; set; }
        public List<ResolverInfo> Resolvers { get; set; } = new List<ResolverInfo>();
    }

    /// <summary>
    /// Resolver information within a realm
    /// </summary>
    public class ResolverInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int? Priority { get; set; }
        public string? Node { get; set; }
    }

    /// <summary>
    /// Result of realm creation/update
    /// </summary>
    public class SetRealmResult
    {
        public List<string> Added { get; set; } = new List<string>();
        public List<string> Failed { get; set; } = new List<string>();
    }

    /// <summary>
    /// Realm management service
    /// Equivalent to Python's realm.py module functions
    /// </summary>
    public class RealmService
    {
        private readonly PrivacyIDEAContext _context;
        private readonly ILogger<RealmService> _logger;
        private readonly ConfigService _configService;

        public RealmService(
            PrivacyIDEAContext context,
            ILogger<RealmService> logger,
            ConfigService configService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        /// <summary>
        /// Get all defined realms or a specific realm
        /// Equivalent to Python's get_realms function
        /// </summary>
        /// <param name="realmName">Optional realm name to filter</param>
        /// <returns>Dictionary of realm configurations</returns>
        public async Task<Dictionary<string, RealmConfig>> GetRealmsAsync(string? realmName = null)
        {
            var configObject = await _configService.GetConfigObjectAsync();
            var realms = configObject.Realms;

            if (!string.IsNullOrEmpty(realmName))
            {
                if (realms.ContainsKey(realmName))
                {
                    return new Dictionary<string, RealmConfig> { { realmName, realms[realmName] } };
                }
                else
                {
                    return new Dictionary<string, RealmConfig>();
                }
            }

            return realms;
        }

        /// <summary>
        /// Get a single realm configuration
        /// Equivalent to Python's get_realm function
        /// </summary>
        /// <param name="realmName">Realm name</param>
        /// <returns>Realm configuration or empty dictionary</returns>
        public async Task<RealmConfig> GetRealmAsync(string realmName)
        {
            var realms = await GetRealmsAsync(realmName);
            return realms.GetValueOrDefault(realmName) ?? new RealmConfig();
        }

        /// <summary>
        /// Get the realm ID for a realm name
        /// Equivalent to Python's get_realm_id function
        /// </summary>
        /// <param name="realmName">The name of the realm</param>
        /// <returns>The ID of the realm, or null if not found</returns>
        public async Task<int?> GetRealmIdAsync(string realmName)
        {
            var configObject = await _configService.GetConfigObjectAsync();
            return configObject.Realms.GetValueOrDefault(realmName)?.Id;
        }

        /// <summary>
        /// Check if a realm exists
        /// Equivalent to Python's realm_is_defined function
        /// </summary>
        /// <param name="realm">The realm to verify</param>
        /// <returns>True if realm exists</returns>
        public async Task<bool> RealmIsDefinedAsync(string realm)
        {
            var realms = await GetRealmsAsync();
            return realms.ContainsKey(realm.ToLower());
        }

        /// <summary>
        /// Set the default realm attribute
        /// Equivalent to Python's set_default_realm function
        /// </summary>
        /// <param name="defaultRealm">The default realm name, or null to clear</param>
        /// <returns>DB ID of the realm set as default</returns>
        public async Task<int> SetDefaultRealmAsync(string? defaultRealm = null)
        {
            int res = 0;

            // Find existing default realm
            var existingDefault = await _context.Realms
                .FirstOrDefaultAsync(r => r.Default);

            if (existingDefault != null)
            {
                existingDefault.Default = false;
                res = existingDefault.Id;
            }

            if (!string.IsNullOrEmpty(defaultRealm))
            {
                // Set the new realm as default
                var realm = await _context.Realms
                    .FirstOrDefaultAsync(r => r.Name == defaultRealm);

                if (realm == null)
                {
                    throw new InvalidOperationException($"Realm {defaultRealm} not found");
                }

                realm.Default = true;
                res = realm.Id;
            }

            await _configService.SaveConfigTimestampAsync();
            await _context.SaveChangesAsync();

            return res;
        }

        /// <summary>
        /// Get the default realm
        /// Equivalent to Python's get_default_realm function
        /// </summary>
        /// <returns>The default realm name</returns>
        public async Task<string> GetDefaultRealmAsync()
        {
            var configObject = await _configService.GetConfigObjectAsync();
            return configObject.DefaultRealm;
        }

        /// <summary>
        /// Delete a realm from the database
        /// Equivalent to Python's delete_realm function
        /// </summary>
        /// <param name="realmName">The realm to delete</param>
        /// <returns>The realm ID</returns>
        public async Task<int> DeleteRealmAsync(string realmName)
        {
            // Check if there are still users assigned to tokens or containers
            // TODO: Implement token and container checks when those services are available
            // For now, we'll just delete the realm

            var defaultRealm = await GetDefaultRealmAsync();
            bool hadDefaultRealmBefore = !string.IsNullOrEmpty(defaultRealm);

            var realm = await _context.Realms
                .Include(r => r.TokenList)
                .Include(r => r.ResolverList)
                .FirstOrDefaultAsync(r => r.Name == realmName);

            if (realm == null)
            {
                throw new InvalidOperationException($"Realm {realmName} not found");
            }

            int realmId = realm.Id;

            // Delete relationships
            _context.TokenRealms.RemoveRange(
                _context.TokenRealms.Where(tr => tr.RealmId == realmId));
            _context.ResolverRealms.RemoveRange(
                _context.ResolverRealms.Where(rr => rr.RealmId == realmId));

            // Delete realm
            _context.Realms.Remove(realm);
            await _configService.SaveConfigTimestampAsync();
            await _context.SaveChangesAsync();

            // If there was a default realm before and there's only one realm left, set it as default
            if (hadDefaultRealmBefore)
            {
                defaultRealm = await GetDefaultRealmAsync();
                if (string.IsNullOrEmpty(defaultRealm))
                {
                    var realms = await GetRealmsAsync();
                    if (realms.Count == 1)
                    {
                        var remainingRealm = realms.Keys.First();
                        await SetDefaultRealmAsync(remainingRealm);
                    }
                }
            }

            return realmId;
        }

        /// <summary>
        /// Create or update a realm with resolvers
        /// Equivalent to Python's set_realm function
        /// </summary>
        /// <param name="realm">Name of an existing or new realm</param>
        /// <param name="resolvers">List of resolver configurations</param>
        /// <returns>Tuple of (added resolvers, failed resolvers)</returns>
        public async Task<SetRealmResult> SetRealmAsync(
            string realm,
            List<ResolverInfo>? resolvers = null)
        {
            resolvers ??= new List<ResolverInfo>();
            var result = new SetRealmResult();
            bool realmCreated = false;

            realm = realm.ToLower().Trim().Replace(" ", "-");
            
            // Validate realm name
            SanityNameCheck(realm, @"^[A-Za-z0-9_\-\.]+$");

            // Create new realm if it doesn't exist
            var dbRealm = await _context.Realms
                .Include(r => r.ResolverList)
                .FirstOrDefaultAsync(r => r.Name == realm);

            if (dbRealm == null)
            {
                dbRealm = new Realm(realm);
                _context.Realms.Add(dbRealm);
                await _context.SaveChangesAsync(); // Save to get the ID
                realmCreated = true;
            }

            if (!realmCreated)
            {
                // Delete old resolvers if we're updating the realm
                _context.ResolverRealms.RemoveRange(
                    _context.ResolverRealms.Where(rr => rr.RealmId == dbRealm.Id));
            }

            // Assign the resolvers
            foreach (var reso in resolvers)
            {
                var resoName = reso.Name.Trim();
                var dbResolver = await _context.Resolvers
                    .FirstOrDefaultAsync(r => r.Name == resoName);

                if (dbResolver != null)
                {
                    try
                    {
                        var newResolverRealm = new ResolverRealm(
                            resolverId: dbResolver.Id,
                            realmId: dbRealm.Id,
                            nodeUuid: reso.Node ?? string.Empty,
                            priority: reso.Priority);

                        _context.ResolverRealms.Add(newResolverRealm);
                        result.Added.Add(resoName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not add resolver {ResolverName} to realm {RealmName}", 
                            resoName, realm);
                        result.Failed.Add(resoName);
                    }
                }
                else
                {
                    _logger.LogDebug("Could not find resolver {ResolverName} in database", resoName);
                    result.Failed.Add(resoName);
                }
            }

            // If this is the first realm, make it the default
            var realmCount = await _context.Realms.CountAsync();
            if (realmCount == 1)
            {
                dbRealm.Default = true;
            }

            await _configService.SaveConfigTimestampAsync();
            await _context.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// Export realm configuration
        /// Equivalent to Python's export_realms function
        /// </summary>
        /// <param name="name">Optional realm name to export</param>
        /// <returns>Realm configuration dictionary</returns>
        public async Task<Dictionary<string, RealmConfig>> ExportRealmsAsync(string? name = null)
        {
            return await GetRealmsAsync(name);
        }

        /// <summary>
        /// Import realm configurations
        /// Equivalent to Python's import_realms function
        /// </summary>
        /// <param name="data">Realm configuration data</param>
        /// <param name="name">Optional realm name to import (imports only this one)</param>
        public async Task ImportRealmsAsync(Dictionary<string, RealmConfig> data, string? name = null)
        {
            _logger.LogDebug("Import realm config: {Data}", data);

            foreach (var (realmName, realmConfig) in data)
            {
                if (!string.IsNullOrEmpty(name) && name != realmName)
                {
                    continue;
                }

                var result = await SetRealmAsync(realmName, realmConfig.Resolvers);

                if (realmConfig.Default)
                {
                    await SetDefaultRealmAsync(realmName);
                }

                _logger.LogInformation(
                    "realm: {RealmName,-15} resolver added: {Added} failed: {Failed}",
                    realmName, result.Added, result.Failed);
            }
        }

        /// <summary>
        /// Check sanity of a name (realm, resolver, etc.)
        /// Equivalent to Python's sanity_name_check function
        /// </summary>
        /// <param name="name">The name to check</param>
        /// <param name="pattern">Regex pattern to match</param>
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
    }

    /// <summary>
    /// Config service interface (simplified for realm service)
    /// </summary>
    public class ConfigService
    {
        private readonly PrivacyIDEAContext _context;

        public ConfigService(PrivacyIDEAContext context)
        {
            _context = context;
        }

        public async Task<ConfigObject> GetConfigObjectAsync()
        {
            // Load realms with their resolvers
            var realms = await _context.Realms
                .Include(r => r.ResolverList)
                    .ThenInclude(rr => rr.Resolver)
                .ToListAsync();

            var configObject = new ConfigObject();

            foreach (var realm in realms)
            {
                var realmConfig = new RealmConfig
                {
                    Id = realm.Id,
                    Default = realm.Default,
                    Resolvers = realm.ResolverList.Select(rr => new ResolverInfo
                    {
                        Name = rr.Resolver?.Name ?? string.Empty,
                        Type = rr.Resolver?.ResolverType ?? string.Empty,
                        Priority = rr.Priority,
                        Node = rr.NodeUuid
                    }).ToList()
                };

                configObject.Realms[realm.Name] = realmConfig;
            }

            // Set default realm
            var defaultRealm = realms.FirstOrDefault(r => r.Default);
            configObject.DefaultRealm = defaultRealm?.Name ?? string.Empty;

            return configObject;
        }

        public async Task SaveConfigTimestampAsync()
        {
            var timestamp = await _context.Configs
                .FirstOrDefaultAsync(c => c.Key == ConfigConstants.PrivacyIdeaTimestamp);

            if (timestamp == null)
            {
                timestamp = new Config(ConfigConstants.PrivacyIdeaTimestamp, DateTime.UtcNow.ToString("O"));
                _context.Configs.Add(timestamp);
            }
            else
            {
                timestamp.Value = DateTime.UtcNow.ToString("O");
            }

            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Configuration object structure
    /// </summary>
    public class ConfigObject
    {
        public Dictionary<string, RealmConfig> Realms { get; set; } = new Dictionary<string, RealmConfig>();
        public string DefaultRealm { get; set; } = string.Empty;
    }
}
