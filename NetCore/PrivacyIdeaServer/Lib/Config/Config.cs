// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/config.py
// The config module takes care about storing server configuration in the Config database table.
// It provides functions to retrieve (get) and set configuration.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Lib.Crypto;
using PrivacyIdeaServer.Lib.Utils;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Lib.Config
{
    /// <summary>
    /// System configuration constants
    /// Equivalent to Python's SYSCONF class
    /// </summary>
    public static class SYSCONF
    {
        public const string OVERRIDECLIENT = "OverrideAuthorizationClient";
        public const string PREPENDPIN = "PrependPin";
        public const string SPLITATSIGN = "splitAtSign";
        public const string INCFAILCOUNTER = "IncFailCountOnFalsePin";
        public const string RETURNSAML = "ReturnSamlAttributes";
        public const string RETURNSAMLONFAIL = "ReturnSamlAttributesOnFail";
        public const string RESET_FAILCOUNTER_ON_PIN_ONLY = "ResetFailcounterOnPIN";
    }

    /// <summary>
    /// Configuration keys used in the application
    /// Equivalent to Python's ConfigKey class
    /// </summary>
    public static class ConfigKey
    {
        public const string ENABLE_CSP = "PI_ENABLE_CSP";
        public const string SESSION_COOKIE_SECURE = "PI_SESSION_COOKIE_SECURE";
        public const string FORCE_HTTPS = "PI_FORCE_HTTPS";
        public const string SECRET_KEY = "SECRET_KEY";
        public const string PEPPER = "PI_PEPPER";
        public const string ENCFILE = "PI_ENCFILE";

        public const string SQLALCHEMY_DATABASE_URI = "SQLALCHEMY_DATABASE_URI";
        public const string SQLALCHEMY_ENGINE_OPTIONS = "SQLALCHEMY_ENGINE_OPTIONS";
        public const string DEV_DATABASE_URL = "DEV_DATABASE_URL";
        public const string TEST_DATABASE_URL = "TEST_DATABASE_URL";
        public const string DB_DRIVER = "PI_DB_DRIVER";
        public const string DB_USER = "PI_DB_USER";
        public const string DB_PASSWORD = "PI_DB_PASSWORD";
        public const string DB_HOST = "PI_DB_HOST";
        public const string DB_PORT = "PI_DB_PORT";
        public const string DB_NAME = "PI_DB_NAME";
        public const string DB_EXTRA_PARAMS = "PI_DB_EXTRA_PARAMS";

        public const string AUDIT_SQL_URI = "PI_AUDIT_SQL_URI";
        public const string AUDIT_SQL_OPTIONS = "PI_AUDIT_SQL_OPTIONS";
        public const string AUDIT_POOL_SIZE = "PI_AUDIT_POOL_SIZE";
        public const string AUDIT_POOL_RECYCLE = "PI_AUDIT_POOL_RECYCLE";
        public const string AUDIT_KEY_PRIVATE = "PI_AUDIT_KEY_PRIVATE";
        public const string AUDIT_KEY_PUBLIC = "PI_AUDIT_KEY_PUBLIC";
        public const string AUDIT_MODULE = "PI_AUDIT_MODULE";
        public const string AUDIT_SERVERNAME = "PI_AUDIT_SERVERNAME";
        public const string AUDIT_SQL_TRUNCATE = "PI_AUDIT_SQL_TRUNCATE";
        public const string AUDIT_NO_SIGN = "PI_AUDIT_NO_SIGN";
        public const string AUDIT_NO_PRIVATE_KEY_CHECK = "PI_AUDIT_NO_PRIVATE_KEY_CHECK";
        public const string AUDIT_SQL_COLUMN_LENGTH = "PI_AUDIT_SQL_COLUMN_LENGTH";
        public const string CHECK_OLD_SIGNATURES = "PI_CHECK_OLD_SIGNATURES";

        public const string LOGLEVEL = "PI_LOGLEVEL";
        public const string LOGCONFIG = "PI_LOGCONFIG";
        public const string LOGFILE = "PI_LOGFILE";

        public const string NODE = "PI_NODE";
        public const string NODE_UUID = "PI_NODE_UUID";
        public const string UUID_FILE = "PI_UUID_FILE";

        public const string VERBOSE = "VERBOSE";
        public const string SUPERUSER_REALM = "SUPERUSER_REALM";
        public const string APP_READY = "APP_READY";
        public const string HSM_INITIALIZE = "PI_INITIALIZE_HSM";
        public const string CONFIG_NAME = "PI_CONFIG_NAME";
        public const string STATIC_FOLDER = "PI_STATIC_FOLDER";
        public const string TEMPLATE_FOLDER = "PI_TEMPLATE_FOLDER";
        public const string NO_RESPONSE_SIGN = "PI_NO_RESPONSE_SIGN";
        public const string RESPONSE_NO_PRIVATE_KEY_CHECK = "PI_RESPONSE_NO_PRIVATE_KEY_CHECK";
    }

    /// <summary>
    /// Default configuration values
    /// Equivalent to Python's DefaultConfigValues class
    /// </summary>
    public static class DefaultConfigValues
    {
        public const string UUID_FILE = "/etc/privacyidea/uuid.txt";
        public const string NODE_NAME = "localnode";
        public const string STATIC_FOLDER = "static/";
        public const string TEMPLATE_FOLDER = "static/templates/";
        public const string CFG_PATH = "/etc/privacyidea/pi.cfg";
        public const string LOGFILE_PATH = "/etc/privacyidea/privacyidea.log";
        public const string LOGGING_CFG = "/etc/privacyidea/logging.cfg";
    }

    /// <summary>
    /// A shared config class object is shared between threads and is supposed
    /// to store the current configuration with resolvers, realms, policies
    /// and event handler definitions along with the timestamp of the configuration.
    /// Equivalent to Python's SharedConfigClass
    /// </summary>
    public class SharedConfigClass
    {
        private readonly SemaphoreSlim _configLock = new(1, 1);
        private readonly PrivacyIDEAContext _context;
        private readonly ILogger<SharedConfigClass> _logger;

        public Dictionary<string, ConfigValue> Config { get; private set; } = new();
        public Dictionary<string, ResolverDefinition> Resolver { get; private set; } = new();
        public Dictionary<string, RealmDefinition> Realm { get; private set; } = new();
        public string? DefaultRealm { get; private set; }
        public List<Dictionary<string, object>> Policies { get; private set; } = new();
        public List<Dictionary<string, object>> Events { get; private set; } = new();
        public List<CAConnectorDefinition> CAConnectors { get; private set; } = new();
        public DateTime? Timestamp { get; private set; }

        public SharedConfigClass(PrivacyIDEAContext context, ILogger<SharedConfigClass> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Read the timestamp from the database. If the timestamp is newer than
        /// the internal timestamp, then read the complete data
        /// </summary>
        public async Task ReloadFromDbAsync()
        {
            var reloadIntervalSeconds = Framework.GetAppConfigValue("PI_CHECK_RELOAD_CONFIG", 0);
            if (Timestamp == null || Timestamp.Value.AddSeconds(reloadIntervalSeconds) < DateTime.UtcNow)
            {
                var dbTs = await _context.Configs
                    .Where(c => c.Key == ConfigConstants.PrivacyIdeaTimestamp)
                    .FirstOrDefaultAsync();

                if (ShouldReloadDb(Timestamp, dbTs))
                {
                    _logger.LogDebug("Reloading shared config from database");

                    var config = new Dictionary<string, ConfigValue>();
                    var resolverConfig = new Dictionary<string, ResolverDefinition>();
                    var realmConfig = new Dictionary<string, RealmDefinition>();
                    string? defaultRealm = null;
                    var policies = new List<Dictionary<string, object>>();
                    var events = new List<Dictionary<string, object>>();
                    var caConnectors = new List<CAConnectorDefinition>();

                    // Load system configuration
                    var sysConfigs = await _context.Configs.ToListAsync();
                    foreach (var sysConf in sysConfigs)
                    {
                        config[sysConf.Key] = new ConfigValue
                        {
                            Value = sysConf.Value ?? string.Empty,
                            Type = sysConf.Type ?? string.Empty,
                            Description = sysConf.Description ?? string.Empty
                        };
                    }

                    // Load resolver configuration
                    var resolvers = await _context.Resolvers
                        .Include(r => r.ConfigList)
                        .ToListAsync();

                    foreach (var resolver in resolvers)
                    {
                        var resolverDef = new ResolverDefinition
                        {
                            Type = resolver.ResolverType ?? string.Empty,
                            ResolverName = resolver.Name ?? string.Empty,
                            CensorKeys = new List<string>(),
                            Data = new Dictionary<string, object>()
                        };

                        foreach (var rconf in resolver.ConfigList ?? new List<ResolverConfig>())
                        {
                            object value = rconf.Value ?? string.Empty;

                            if (rconf.Type == "password")
                            {
                                value = DecryptPassword(rconf.Value ?? string.Empty);
                                resolverDef.CensorKeys.Add(rconf.Key ?? string.Empty);
                            }
                            else if (rconf.Type == "dict")
                            {
                                try
                                {
                                    value = JsonSerializer.Deserialize<Dictionary<string, object>>(rconf.Value ?? "{}") 
                                            ?? new Dictionary<string, object>();
                                }
                                catch (JsonException ex)
                                {
                                    _logger.LogDebug(ex, "Could not load dict {Key} ({Value}) from resolver config as JSON", 
                                        rconf.Key, rconf.Value);
                                    value = rconf.Value ?? string.Empty;
                                }
                            }
                            else if (rconf.Type == "dict_with_password")
                            {
                                try
                                {
                                    var dictValue = JsonSerializer.Deserialize<Dictionary<string, object>>(rconf.Value ?? "{}") 
                                                    ?? new Dictionary<string, object>();
                                    // Decrypt password values in the dictionary
                                    foreach (var kvp in dictValue.ToList())
                                    {
                                        // This would need class descriptor config to determine which fields are passwords
                                        // For now, we'll keep it as-is
                                    }
                                    value = dictValue;
                                }
                                catch (JsonException ex)
                                {
                                    _logger.LogDebug(ex, "Could not load dict_with_password {Key} ({Value}) from resolver config as JSON", 
                                        rconf.Key, rconf.Value);
                                    value = rconf.Value ?? string.Empty;
                                }
                            }

                            resolverDef.Data[rconf.Key ?? string.Empty] = value;
                        }

                        resolverConfig[resolver.Name ?? string.Empty] = resolverDef;
                    }

                    // Load realm configuration
                    var realms = await _context.Realms
                        .Include(r => r.ResolverList)
                        .ThenInclude(rr => rr.Resolver)
                        .ToListAsync();

                    foreach (var realm in realms)
                    {
                        if (realm.Default)
                        {
                            defaultRealm = realm.Name;
                        }

                        var realmDef = new RealmDefinition
                        {
                            Id = realm.Id,
                            Default = realm.Default,
                            Resolvers = new List<RealmResolverEntry>()
                        };

                        foreach (var rr in realm.ResolverList ?? new List<ResolverRealm>())
                        {
                            realmDef.Resolvers.Add(new RealmResolverEntry
                            {
                                Priority = rr.Priority.GetValueOrDefault(0),
                                Name = rr.Resolver?.Name ?? string.Empty,
                                Type = rr.Resolver?.ResolverType ?? string.Empty,
                                Node = rr.NodeUuid ?? string.Empty
                            });
                        }

                        realmConfig[realm.Name ?? string.Empty] = realmDef;
                    }

                    // Load all policies
                    var policiesDb = await _context.Policies
                        .Include(p => p.Conditions)
                        .Include(p => p.Descriptions)
                        .ToListAsync();

                    foreach (var pol in policiesDb)
                    {
                        // Convert policy to dictionary representation
                        var policyDict = new Dictionary<string, object>
                        {
                            ["id"] = pol.Id,
                            ["name"] = pol.Name ?? string.Empty,
                            ["active"] = pol.Active,
                            ["scope"] = pol.Scope ?? string.Empty,
                            ["action"] = pol.Action ?? string.Empty,
                            ["realm"] = pol.Realm ?? string.Empty,
                            ["resolver"] = pol.Resolver ?? string.Empty,
                            ["user"] = pol.User ?? string.Empty,
                            ["client"] = pol.Client ?? string.Empty,
                            ["time"] = pol.Time ?? string.Empty,
                            ["priority"] = pol.Priority
                        };
                        policies.Add(policyDict);
                    }

                    // Load all event handlers
                    var eventHandlers = await _context.EventHandlers
                        .Include(e => e.Options)
                        .Include(e => e.Conditions)
                        .OrderBy(e => e.Position ?? 0)
                        .ToListAsync();

                    foreach (var evt in eventHandlers)
                    {
                        var eventDict = new Dictionary<string, object>
                        {
                            ["id"] = evt.Id,
                            ["name"] = evt.Name ?? string.Empty,
                            ["event"] = evt.Event ?? string.Empty,
                            ["handlermodule"] = evt.HandlerModule ?? string.Empty,
                            ["action"] = evt.Action ?? string.Empty,
                            ["active"] = evt.Active,
                            ["ordering"] = evt.Position ?? 0
                        };
                        events.Add(eventDict);
                    }

                    // Load all CA connectors
                    var caConnectorsDb = await _context.CAConnectors.ToListAsync();

                    foreach (var ca in caConnectorsDb)
                    {
                        // Load configs for this CA connector
                        var configs = await _context.CAConnectorConfigs
                            .Where(c => c.CAConnectorId == ca.Id)
                            .ToListAsync();

                        var caConfig = new Dictionary<string, string>();
                        foreach (var conf in configs)
                        {
                            caConfig[conf.Key ?? string.Empty] = conf.Value ?? string.Empty;
                        }

                        caConnectors.Add(new CAConnectorDefinition
                        {
                            ConnectorName = ca.Name ?? string.Empty,
                            Type = ca.ConnectorType ?? string.Empty,
                            Data = caConfig,
                            Templates = new List<string>() // TODO: Load templates if available
                        });
                    }

                    // Update all configuration atomically
                    var timestamp = DateTime.UtcNow;
                    await _configLock.WaitAsync();
                    try
                    {
                        Config = config;
                        Resolver = resolverConfig;
                        Realm = realmConfig;
                        DefaultRealm = defaultRealm;
                        Policies = policies;
                        Events = events;
                        CAConnectors = caConnectors;
                        Timestamp = timestamp;
                    }
                    finally
                    {
                        _configLock.Release();
                    }
                }
            }
        }

        /// <summary>
        /// Clone the current configuration to a local config object
        /// </summary>
        public async Task<LocalConfigClass> CloneAsync()
        {
            await _configLock.WaitAsync();
            try
            {
                return new LocalConfigClass(
                    new Dictionary<string, ConfigValue>(Config),
                    new Dictionary<string, ResolverDefinition>(Resolver),
                    new Dictionary<string, RealmDefinition>(Realm),
                    DefaultRealm,
                    new List<Dictionary<string, object>>(Policies),
                    new List<Dictionary<string, object>>(Events),
                    new List<CAConnectorDefinition>(CAConnectors),
                    Timestamp
                );
            }
            finally
            {
                _configLock.Release();
            }
        }

        /// <summary>
        /// Check if the current configuration state is outdated, reload it if needed
        /// and return a LocalConfigClass object containing the current configuration state
        /// </summary>
        public async Task<LocalConfigClass> ReloadAndCloneAsync()
        {
            await ReloadFromDbAsync();
            return await CloneAsync();
        }

        private bool ShouldReloadDb(DateTime? currentTimestamp, PrivacyIdeaServer.Models.Database.Config? dbTimestamp)
        {
            if (currentTimestamp == null) return true;
            if (dbTimestamp == null) return false;
            
            // Compare timestamps - reload if DB is newer
            if (DateTime.TryParse(dbTimestamp.Value, out var dbTime))
            {
                return dbTime > currentTimestamp.Value;
            }
            
            return false;
        }

        private string DecryptPassword(string encryptedPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedPassword))
                    return string.Empty;

                // Use crypto functions to decrypt
                return CryptoFunctions.DecryptPin(encryptedPassword);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt password");
                return "FAILED_TO_DECRYPT_PASSWORD";
            }
        }
    }

    /// <summary>
    /// The Config_Object will contain all database configuration of system
    /// config, resolvers, realms, policies and event handler definitions.
    /// It will be cloned from the shared config object at the beginning of the
    /// request and is supposed to stay alive and unchanged during the request.
    /// Equivalent to Python's LocalConfigClass
    /// </summary>
    public class LocalConfigClass
    {
        public Dictionary<string, ConfigValue> Config { get; }
        public Dictionary<string, ResolverDefinition> Resolver { get; }
        public Dictionary<string, RealmDefinition> Realm { get; }
        public string? DefaultRealm { get; }
        public List<Dictionary<string, object>> Policies { get; }
        public List<Dictionary<string, object>> Events { get; }
        public List<CAConnectorDefinition> CAConnectors { get; }
        public DateTime? Timestamp { get; }

        public LocalConfigClass(
            Dictionary<string, ConfigValue> config,
            Dictionary<string, ResolverDefinition> resolver,
            Dictionary<string, RealmDefinition> realm,
            string? defaultRealm,
            List<Dictionary<string, object>> policies,
            List<Dictionary<string, object>> events,
            List<CAConnectorDefinition> caConnectors,
            DateTime? timestamp)
        {
            Config = config;
            Resolver = resolver;
            Realm = realm;
            DefaultRealm = defaultRealm;
            Policies = policies;
            Events = events;
            CAConnectors = caConnectors;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Get configuration value(s)
        /// </summary>
        /// <param name="key">A key to retrieve</param>
        /// <param name="defaultValue">The default value if it does not exist in the database</param>
        /// <param name="role">The role which wants to retrieve the system config (admin or public)</param>
        /// <param name="returnBool">If a boolean value should be returned</param>
        /// <returns>If key is null, a dictionary is returned. Otherwise a string/bool is returned.</returns>
        public object GetConfig(string? key = null, object? defaultValue = null, string role = "admin", bool returnBool = false)
        {
            var defaultTrueKeys = new[]
            {
                SYSCONF.PREPENDPIN,
                SYSCONF.SPLITATSIGN,
                SYSCONF.INCFAILCOUNTER,
                SYSCONF.RETURNSAML
            };

            var rConfig = new Dictionary<string, object>();

            // Reduce the dictionary to only public keys if role is not admin
            var reducedConfig = new Dictionary<string, ConfigValue>();
            foreach (var kvp in Config)
            {
                if (role == "admin" || kvp.Value.Type == "public" || kvp.Key.EndsWith(".identifier"))
                {
                    reducedConfig[kvp.Key] = kvp.Value;
                }
            }

            if (reducedConfig.Count == 0 && role == "admin")
            {
                reducedConfig = Config;
            }

            foreach (var kvp in reducedConfig)
            {
                if (kvp.Value.Type == "password")
                {
                    // Decrypt the password
                    try
                    {
                        rConfig[kvp.Key] = CryptoFunctions.DecryptPin(kvp.Value.Value ?? string.Empty);
                    }
                    catch
                    {
                        rConfig[kvp.Key] = "FAILED_TO_DECRYPT_PASSWORD";
                    }
                }
                else
                {
                    rConfig[kvp.Key] = kvp.Value.Value ?? string.Empty;
                }
            }

            // Set default true keys
            foreach (var tKey in defaultTrueKeys)
            {
                if (!rConfig.ContainsKey(tKey))
                {
                    rConfig[tKey] = "True";
                }
            }

            object result;
            if (key != null)
            {
                // Return a single key
                result = rConfig.ContainsKey(key) ? rConfig[key] : (defaultValue ?? string.Empty);
            }
            else
            {
                result = rConfig;
            }

            if (returnBool)
            {
                if (result is bool boolValue)
                {
                    return boolValue;
                }
                if (result is int intValue)
                {
                    return intValue > 0;
                }
                if (result is string strValue)
                {
                    return ConfigurationParser.IsTrue(strValue.ToLower());
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Configuration management service
    /// Provides functions to retrieve and set configuration
    /// </summary>
    public class ConfigManager
    {
        private readonly PrivacyIDEAContext _context;
        private readonly ILogger<ConfigManager> _logger;
        private static SharedConfigClass? _sharedConfigObject;
        private static readonly SemaphoreSlim _sharedConfigLock = new(1, 1);

        public ConfigManager(PrivacyIDEAContext context, ILogger<ConfigManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get the application-wide SharedConfigClass object, which is created on demand
        /// </summary>
        public async Task<SharedConfigClass> GetSharedConfigObjectAsync()
        {
            if (_sharedConfigObject == null)
            {
                await _sharedConfigLock.WaitAsync();
                try
                {
                    if (_sharedConfigObject == null)
                    {
                        _logger.LogDebug("Creating new shared config object");
                        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                        var sharedLogger = loggerFactory.CreateLogger<SharedConfigClass>();
                        _sharedConfigObject = new SharedConfigClass(_context, sharedLogger);
                    }
                }
                finally
                {
                    _sharedConfigLock.Release();
                }
            }

            return _sharedConfigObject;
        }

        /// <summary>
        /// Invalidate the shared config object to force reload
        /// </summary>
        public static void InvalidateConfigObject()
        {
            _sharedConfigObject = null;
        }

        /// <summary>
        /// Get configuration object (request-local)
        /// </summary>
        public async Task<LocalConfigClass> GetConfigObjectAsync()
        {
            var store = Framework.GetRequestLocalStore();
            if (!store.ContainsKey("config_object"))
            {
                _logger.LogDebug("Cloning request-local config from shared config object");
                var sharedConfig = await GetSharedConfigObjectAsync();
                store["config_object"] = await sharedConfig.ReloadAndCloneAsync();
            }

            return (LocalConfigClass)store["config_object"];
        }

        /// <summary>
        /// Get a configuration value from the database
        /// </summary>
        /// <param name="key">A key to retrieve</param>
        /// <param name="defaultValue">The default value if it does not exist</param>
        /// <param name="role">The role (admin or public)</param>
        /// <param name="returnBool">Whether to return a boolean value</param>
        /// <returns>Configuration value or dictionary of all values</returns>
        public async Task<object> GetFromConfigAsync(string? key = null, object? defaultValue = null, 
            string role = "admin", bool returnBool = false)
        {
            var configObject = await GetConfigObjectAsync();
            return configObject.GetConfig(key: key, defaultValue: defaultValue, role: role, returnBool: returnBool);
        }

        /// <summary>
        /// Set a config value and write it to the Config database table
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value</param>
        /// <param name="typ">Type (password, public, etc.)</param>
        /// <param name="desc">Description</param>
        /// <returns>"insert" or "update"</returns>
        public async Task<string> SetPrivacyIdeaConfigAsync(string key, string value, string typ = "", string desc = "")
        {
            if (string.IsNullOrEmpty(typ))
            {
                // Check if this is a token-specific config
                // TODO: Implement token class lookup for setting type
                _logger.LogDebug("Type not specified, using default");
            }

            if (typ == "password")
            {
                // Store value in encrypted way
                value = CryptoFunctions.EncryptPin(value);
            }

            var piConfig = await _context.Configs
                .Where(c => c.Key == key)
                .FirstOrDefaultAsync();

            string result;
            if (piConfig != null)
            {
                // Update existing value
                piConfig.Value = value;
                if (!string.IsNullOrEmpty(typ))
                {
                    piConfig.Type = typ;
                }
                if (!string.IsNullOrEmpty(desc))
                {
                    piConfig.Description = desc;
                }
                await SaveConfigTimestampAsync();
                await _context.SaveChangesAsync();
                result = "update";
            }
            else
            {
                // Insert new value
                var newConfig = new PrivacyIdeaServer.Models.Database.Config(key, value, typ, desc);
                _context.Configs.Add(newConfig);
                await SaveConfigTimestampAsync();
                await _context.SaveChangesAsync();
                result = "insert";
            }

            return result;
        }

        /// <summary>
        /// Delete a config entry
        /// </summary>
        /// <param name="key">Configuration key to delete</param>
        /// <returns>True if deleted, false otherwise</returns>
        public async Task<bool> DeletePrivacyIdeaConfigAsync(string key)
        {
            var config = await _context.Configs
                .Where(c => c.Key == key)
                .FirstOrDefaultAsync();

            if (config != null)
            {
                _context.Configs.Remove(config);
                await _context.SaveChangesAsync();
                await SaveConfigTimestampAsync();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return if the Failcounter should be increased if only tokens
        /// with a false PIN were identified
        /// </summary>
        public async Task<bool> GetIncFailCountOnFalsePinAsync()
        {
            var result = await GetFromConfigAsync(
                key: "IncFailCountOnFalsePin",
                defaultValue: false,
                returnBool: true);

            return result is bool b && b;
        }

        /// <summary>
        /// Get the status of the "PrependPin" Config
        /// </summary>
        public async Task<bool> GetPrependPinAsync()
        {
            var result = await GetFromConfigAsync(
                key: "PrependPin",
                defaultValue: false,
                returnBool: true);

            return result is bool b && b;
        }

        /// <summary>
        /// Set the status of the "PrependPin" Config
        /// </summary>
        public async Task SetPrependPinAsync(bool prepend = true)
        {
            await SetPrivacyIdeaConfigAsync("PrependPin", prepend.ToString());
        }

        /// <summary>
        /// Return if SAML attributes should be returned
        /// </summary>
        public async Task<bool> ReturnSamlAttributesAsync()
        {
            var result = await GetFromConfigAsync(
                key: SYSCONF.RETURNSAML,
                defaultValue: false,
                returnBool: true);

            return result is bool b && b;
        }

        /// <summary>
        /// Return if SAML attributes should be returned on authentication failure
        /// </summary>
        public async Task<bool> ReturnSamlAttributesOnFailAsync()
        {
            var result = await GetFromConfigAsync(
                key: SYSCONF.RETURNSAMLONFAIL,
                defaultValue: false,
                returnBool: true);

            return result is bool b && b;
        }

        /// <summary>
        /// Get the node name of the privacyIDEA node as found in the configuration
        /// </summary>
        /// <param name="defaultName">Default node name if not configured</param>
        /// <returns>The distinct node name</returns>
        public string GetPrivacyIdeaNode(string defaultName = DefaultConfigValues.NODE_NAME)
        {
            var nodeName = Framework.GetAppConfigValue(
                ConfigKey.NODE,
                Framework.GetAppConfigValue(ConfigKey.AUDIT_SERVERNAME, defaultName));

            return nodeName ?? defaultName;
        }

        /// <summary>
        /// Get the list of nodes known to privacyIDEA
        /// </summary>
        /// <returns>List of nodes with name and uuid</returns>
        public async Task<List<NodeInfo>> GetPrivacyIdeaNodesAsync()
        {
            var nodes = await _context.NodeNames.ToListAsync();

            return nodes.Select(node => new NodeInfo
            {
                Uuid = node.Id ?? string.Empty,
                Name = node.Name ?? string.Empty
            }).ToList();
        }

        /// <summary>
        /// Get the list of node names known to privacyIDEA
        /// </summary>
        /// <returns>List of node names as strings</returns>
        public async Task<List<string>> GetPrivacyIdeaNodeNamesAsync()
        {
            var nodes = await GetPrivacyIdeaNodesAsync();
            return nodes.Select(n => n.Name).ToList();
        }

        /// <summary>
        /// Check if a node with the given UUID exists in the database
        /// </summary>
        /// <param name="nodeUuid">The UUID of the node</param>
        /// <returns>True if the node exists, false otherwise</returns>
        public async Task<bool> CheckNodeUuidExistsAsync(string nodeUuid)
        {
            return await _context.NodeNames
                .Where(n => n.Id == nodeUuid)
                .AnyAsync();
        }

        /// <summary>
        /// Export the global configuration
        /// </summary>
        /// <param name="name">Optional specific key to export</param>
        /// <returns>Configuration dictionary</returns>
        public async Task<Dictionary<string, ConfigValue>> ExportConfigAsync(string? name = null)
        {
            var configObject = await GetConfigObjectAsync();
            var config = new Dictionary<string, ConfigValue>(configObject.Config);

            if (!string.IsNullOrEmpty(name))
            {
                if (config.ContainsKey(name))
                {
                    return new Dictionary<string, ConfigValue> { [name] = config[name] };
                }
                return new Dictionary<string, ConfigValue>();
            }

            return config;
        }

        /// <summary>
        /// Import given server configuration
        /// </summary>
        /// <param name="data">Configuration data to import</param>
        /// <param name="name">Optional specific key to import</param>
        /// <returns>Dictionary of import results (insert/update)</returns>
        public async Task<Dictionary<string, string>> ImportConfigAsync(Dictionary<string, ConfigValue> data, string? name = null)
        {
            _logger.LogDebug("Import server config: {Count} items", data.Count);

            var results = new Dictionary<string, string>();
            data.Remove(ConfigConstants.PrivacyIdeaTimestamp);

            foreach (var kvp in data)
            {
                if (!string.IsNullOrEmpty(name) && name != kvp.Key)
                {
                    continue;
                }

                var result = await SetPrivacyIdeaConfigAsync(
                    kvp.Key,
                    kvp.Value.Value ?? string.Empty,
                    kvp.Value.Type ?? string.Empty,
                    kvp.Value.Description ?? string.Empty);

                results[kvp.Key] = result;
            }

            var inserted = results.Where(r => r.Value == "insert").Select(r => r.Key);
            var updated = results.Where(r => r.Value == "update").Select(r => r.Key);

            _logger.LogInformation("Added configuration: {ConfigKeys}", string.Join(", ", inserted));
            _logger.LogInformation("Updated configuration: {ConfigKeys}", string.Join(", ", updated));

            return results;
        }

        /// <summary>
        /// Save the configuration timestamp to the database
        /// </summary>
        private async Task SaveConfigTimestampAsync()
        {
            var timestamp = DateTime.UtcNow.ToString("O");
            var tsConfig = await _context.Configs
                .Where(c => c.Key == ConfigConstants.PrivacyIdeaTimestamp)
                .FirstOrDefaultAsync();

            if (tsConfig != null)
            {
                tsConfig.Value = timestamp;
            }
            else
            {
                _context.Configs.Add(new PrivacyIdeaServer.Models.Database.Config(
                    ConfigConstants.PrivacyIdeaTimestamp,
                    timestamp,
                    "internal",
                    "Configuration timestamp"));
            }

            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Configuration value container
    /// </summary>
    public class ConfigValue
    {
        public string? Value { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Resolver definition
    /// </summary>
    public class ResolverDefinition
    {
        public string Type { get; set; } = string.Empty;
        public string ResolverName { get; set; } = string.Empty;
        public List<string> CensorKeys { get; set; } = new();
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Realm definition
    /// </summary>
    public class RealmDefinition
    {
        public int Id { get; set; }
        public bool Default { get; set; }
        public List<RealmResolverEntry> Resolvers { get; set; } = new();
    }

    /// <summary>
    /// Realm resolver entry
    /// </summary>
    public class RealmResolverEntry
    {
        public int Priority { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Node { get; set; } = string.Empty;
    }

    /// <summary>
    /// CA Connector definition
    /// </summary>
    public class CAConnectorDefinition
    {
        public string ConnectorName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, string> Data { get; set; } = new();
        public List<string> Templates { get; set; } = new();
    }

    /// <summary>
    /// Node information
    /// </summary>
    public class NodeInfo
    {
        public string Uuid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
