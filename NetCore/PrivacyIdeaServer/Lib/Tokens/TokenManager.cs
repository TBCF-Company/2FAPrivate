// SPDX-FileCopyrightText: (C) 2014 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// privacyIDEA is a fork of LinOTP
// Converted from Python to C# from privacyidea/lib/token.py
// This module contains all top level token functions.
// This is the middleware/glue between the HTTP API and the database

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Lib.Config;
using PrivacyIdeaServer.Lib.Crypto;
using PrivacyIdeaServer.Lib.Realms;
using PrivacyIdeaServer.Lib.Resolvers;
using PrivacyIdeaServer.Lib.Users;
using PrivacyIdeaServer.Lib.Utils;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Lib.Tokens
{
    /// <summary>
    /// Constants for token management
    /// </summary>
    public static class TokenManagerConstants
    {
        public const string Encoding = "utf-8";
        public const string PiTokenSerialRandom = "PI_TOKEN_SERIAL_RANDOM";
        public const string B32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    }

    /// <summary>
    /// Result of token import operation
    /// </summary>
    public class TokenImportResult
    {
        public List<string> SuccessfulTokens { get; set; } = new();
        public List<string> UpdatedTokens { get; set; } = new();
        public List<string> FailedTokens { get; set; } = new();
    }

    /// <summary>
    /// Result of token export operation
    /// </summary>
    public class TokenExportResult
    {
        public List<string> SuccessfulTokens { get; set; } = new();
        public List<string> FailedTokens { get; set; } = new();
    }

    /// <summary>
    /// Token query parameters for advanced filtering
    /// </summary>
    public class TokenQueryParameters
    {
        public string? TokenType { get; set; }
        public List<string>? TokenTypeList { get; set; }
        public string? Realm { get; set; }
        public bool? Assigned { get; set; }
        public UserIdentity? User { get; set; }
        public string? SerialExact { get; set; }
        public string? SerialWildcard { get; set; }
        public List<string>? SerialList { get; set; }
        public bool? Active { get; set; }
        public string? Resolver { get; set; }
        public string? RolloutState { get; set; }
        public string? Description { get; set; }
        public bool? Revoked { get; set; }
        public bool? Locked { get; set; }
        public string? UserId { get; set; }
        public Dictionary<string, string>? TokenInfo { get; set; }
        public int? MaxFail { get; set; }
        public List<string>? AllowedRealms { get; set; }
        public string? ContainerSerial { get; set; }
        public bool AllNodes { get; set; } = false;
    }

    /// <summary>
    /// Token pagination result
    /// </summary>
    public class TokenPaginationResult
    {
        public List<Dictionary<string, object>> Tokens { get; set; } = new();
        public int Count { get; set; }
        public Dictionary<string, object>? Next { get; set; }
        public Dictionary<string, object>? Prev { get; set; }
        public int Current { get; set; }
    }

    /// <summary>
    /// Token Manager Service - Main interface for token operations
    /// This is the middleware/glue between the HTTP API and the database
    /// Converted from Python's token.py module
    /// </summary>
    public class TokenManager
    {
        private readonly PrivacyIDEAContext _context;
        private readonly ILogger<TokenManager> _logger;
        private readonly ConfigManager _configService;
        private readonly RealmService _realmService;

        public TokenManager(
            PrivacyIDEAContext context,
            ILogger<TokenManager> logger,
            ConfigManager configService,
            RealmService realmService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _realmService = realmService ?? throw new ArgumentNullException(nameof(realmService));
        }

        #region Token Creation and Management

        /// <summary>
        /// Create a token class object from a database token record
        /// Equivalent to Python's create_tokenclass_object
        /// </summary>
        /// <param name="dbToken">Database token object</param>
        /// <returns>Token class instance or null if type not found</returns>
        public TokenClass? CreateTokenClassObject(Token dbToken)
        {
            try
            {
                var tokenType = dbToken.TokenType?.ToLowerInvariant() ?? "hotp";
                
                // TODO: Use a factory pattern to create specific token types
                // For now, this is a placeholder that needs token type implementation
                // In the Python version, get_token_class(tokentype) returns the appropriate class
                
                _logger.LogDebug("Creating token class object for type {TokenType}, serial {Serial}", 
                    tokenType, dbToken.Serial);
                
                // This should be replaced with actual token class factory
                // For now, return null to avoid compilation errors
                // The real implementation would instantiate the appropriate token type class
                _logger.LogWarning("Token class factory not yet implemented for type {TokenType}", tokenType);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create token class object for token {Serial}", dbToken.Serial);
                throw new TokenAdminException($"create_tokenclass_object failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize a token with the given parameters
        /// Equivalent to Python's init_token function
        /// </summary>
        /// <param name="param">Token parameters dictionary</param>
        /// <param name="user">User to assign token to</param>
        /// <param name="tokenRealms">List of realm names</param>
        /// <param name="tokenKind">Token kind (software/hardware/virtual)</param>
        /// <returns>TokenClass object</returns>
        public async Task<TokenClass> InitTokenAsync(
            Dictionary<string, object> param,
            UserIdentity? user = null,
            List<string>? tokenRealms = null,
            string? tokenKind = null)
        {
            _logger.LogInformation("Initializing token with type {TokenType}", 
                param.GetValueOrDefault("type", "unknown"));

            var tokenType = param.GetValueOrDefault("type", "hotp")?.ToString()?.ToLowerInvariant() ?? "hotp";
            var serial = param.GetValueOrDefault("serial")?.ToString();

            Token? dbToken = null;

            if (!string.IsNullOrEmpty(serial))
            {
                // Check if token already exists
                dbToken = await _context.Tokens
                    .Include(t => t.InfoList)
                    .Include(t => t.Owners)
                    .Include(t => t.RealmList)
                    .FirstOrDefaultAsync(t => t.Serial.ToLower() == serial.ToLower());
            }

            if (dbToken == null)
            {
                // Generate serial if not provided
                if (string.IsNullOrEmpty(serial))
                {
                    var prefix = param.GetValueOrDefault("prefix")?.ToString();
                    serial = await GenSerialAsync(tokenType, prefix);
                }

                // Create new token
                dbToken = new Token(serial, tokenType);
                _context.Tokens.Add(dbToken);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created new token with serial {Serial}", serial);
            }
            else
            {
                _logger.LogInformation("Updating existing token {Serial}", serial);
            }

            // Create token class object
            var tokenObj = CreateTokenClassObject(dbToken);
            if (tokenObj == null)
            {
                throw new TokenAdminException($"Could not create token class for type {tokenType}");
            }

            // Update token with parameters
            await tokenObj.UpdateAsync(param);

            // Set token kind if provided
            if (!string.IsNullOrEmpty(tokenKind))
            {
                await AddTokenInfoAsync(serial!, "tokenkind", tokenKind);
            }

            // Assign to user if provided
            if (user != null && !string.IsNullOrEmpty(user.LoginName))
            {
                var pin = param.GetValueOrDefault("pin")?.ToString();
                var encryptPin = param.GetValueOrDefault("encryptpin", false);
                bool encrypt = encryptPin is bool b && b;
                
                await AssignTokenAsync(serial!, user, pin, encrypt);
            }

            // Set realms
            if (tokenRealms != null && tokenRealms.Any())
            {
                await SetRealmsAsync(serial!, tokenRealms);
            }

            await _context.SaveChangesAsync();

            return tokenObj;
        }

        /// <summary>
        /// Generate a unique token serial number
        /// Equivalent to Python's gen_serial function
        /// </summary>
        /// <param name="tokenType">Token type</param>
        /// <param name="prefix">Optional prefix override</param>
        /// <returns>Unique serial number</returns>
        public async Task<string> GenSerialAsync(string tokenType, string? prefix = null)
        {
            var tokenPrefix = prefix;
            
            if (string.IsNullOrEmpty(tokenPrefix))
            {
                // Get prefix from config
                var configPrefix = await _configService.GetFromConfigAsync(
                    $"TokenPrefix.{tokenType}");
                tokenPrefix = configPrefix?.ToString() ?? tokenType.ToUpperInvariant();
            }

            // Check if we should generate completely random serial
            var useRandom = await _configService.GetFromConfigAsync(
                TokenManagerConstants.PiTokenSerialRandom);
            
            if (useRandom?.ToString() == "True" || useRandom?.ToString() == "true")
            {
                return await GenerateRandomSerialAsync();
            }

            // Generate serial with counter
            for (int i = 0; i < 100; i++)
            {
                var rand = GenerateRandomNumber(8);
                var serial = $"{tokenPrefix}{rand:D8}";
                
                if (!await TokenExistsAsync(serial))
                {
                    return serial;
                }
            }

            throw new TokenAdminException("Could not generate unique serial number after 100 attempts");
        }

        /// <summary>
        /// Generate a completely random serial number using cryptographically secure random
        /// </summary>
        private async Task<string> GenerateRandomSerialAsync()
        {
            using var rng = RandomNumberGenerator.Create();
            var alphabet = TokenManagerConstants.B32Alphabet;
            
            for (int i = 0; i < 100; i++)
            {
                var serial = new StringBuilder("PI");
                var randomBytes = new byte[10];
                rng.GetBytes(randomBytes);
                
                for (int j = 0; j < 10; j++)
                {
                    serial.Append(alphabet[randomBytes[j] % alphabet.Length]);
                }
                
                var serialStr = serial.ToString();
                if (!await TokenExistsAsync(serialStr))
                {
                    return serialStr;
                }
            }

            throw new TokenAdminException("Could not generate random serial after 100 attempts");
        }

        /// <summary>
        /// Generate a cryptographically secure random number with specified digits
        /// </summary>
        private int GenerateRandomNumber(int digits)
        {
            using var rng = RandomNumberGenerator.Create();
            var min = (int)Math.Pow(10, digits - 1);
            var max = (int)Math.Pow(10, digits);
            var range = max - min;
            
            var randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            var randomValue = BitConverter.ToUInt32(randomBytes, 0);
            
            return min + (int)(randomValue % range);
        }

        /// <summary>
        /// Remove/delete a token by serial or for a user
        /// Equivalent to Python's remove_token function
        /// </summary>
        /// <param name="serial">Token serial number</param>
        /// <param name="user">User identity</param>
        /// <returns>Number of tokens deleted</returns>
        public async Task<int> RemoveTokenAsync(string? serial = null, UserIdentity? user = null)
        {
            _logger.LogInformation("Removing token(s) - Serial: {Serial}, User: {User}", 
                serial ?? "null", user?.LoginName ?? "null");

            var query = _context.Tokens.AsQueryable();

            if (!string.IsNullOrEmpty(serial))
            {
                query = query.Where(t => t.Serial.ToLower() == serial.ToLower());
            }
            else if (user != null)
            {
                var userId = user.UserId;
                var realmId = user.RealmDatabaseId;
                
                query = query.Where(t => t.Owners.Any(o => 
                    o.UserId == userId && o.RealmId == realmId));
            }
            else
            {
                throw new ParameterException("Either serial or user must be specified");
            }

            var tokens = await query.ToListAsync();
            var count = tokens.Count;

            if (count > 0)
            {
                _context.Tokens.RemoveRange(tokens);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Removed {Count} token(s)", count);
            }

            return count;
        }

        #endregion

        #region Token Retrieval and Queries

        /// <summary>
        /// Create a complex token query with multiple filters
        /// Equivalent to Python's _create_token_query function
        /// </summary>
        private IQueryable<Token> CreateTokenQuery(TokenQueryParameters parameters)
        {
            var query = _context.Tokens
                .Include(t => t.InfoList)
                .Include(t => t.Owners)
                .Include(t => t.RealmList)
                    .ThenInclude(tr => tr.Realm)
                .AsQueryable();

            // Filter by token type
            if (!string.IsNullOrEmpty(parameters.TokenType))
            {
                var tokenType = parameters.TokenType.ToLower();
                if (tokenType.Contains("*"))
                {
                    var pattern = tokenType.Replace("*", "%");
                    query = query.Where(t => EF.Functions.Like(t.TokenType!.ToLower(), pattern));
                }
                else
                {
                    query = query.Where(t => t.TokenType!.ToLower() == tokenType);
                }
            }

            // Filter by token type list
            if (parameters.TokenTypeList != null && parameters.TokenTypeList.Any())
            {
                var types = parameters.TokenTypeList.Select(t => t.ToLower()).ToList();
                query = query.Where(t => types.Contains(t.TokenType!.ToLower()));
            }

            // Filter by serial (exact match)
            if (!string.IsNullOrEmpty(parameters.SerialExact))
            {
                query = query.Where(t => t.Serial.ToLower() == parameters.SerialExact.ToLower());
            }

            // Filter by serial wildcard
            if (!string.IsNullOrEmpty(parameters.SerialWildcard))
            {
                var pattern = parameters.SerialWildcard.ToLower().Replace("*", "%");
                query = query.Where(t => EF.Functions.Like(t.Serial.ToLower(), pattern));
            }

            // Filter by serial list
            if (parameters.SerialList != null && parameters.SerialList.Any())
            {
                var serials = parameters.SerialList.Select(s => s.ToLower()).ToList();
                query = query.Where(t => serials.Contains(t.Serial.ToLower()));
            }

            // Filter by description
            if (!string.IsNullOrEmpty(parameters.Description))
            {
                var desc = parameters.Description.ToLower();
                if (desc.Contains("*"))
                {
                    var pattern = desc.Replace("*", "%");
                    query = query.Where(t => EF.Functions.Like(t.Description!.ToLower(), pattern));
                }
                else
                {
                    query = query.Where(t => t.Description!.ToLower() == desc);
                }
            }

            // Filter by active status
            if (parameters.Active.HasValue)
            {
                query = query.Where(t => t.Active == parameters.Active.Value);
            }

            // Filter by revoked status
            if (parameters.Revoked.HasValue)
            {
                query = query.Where(t => t.Revoked == parameters.Revoked.Value);
            }

            // Filter by locked status
            if (parameters.Locked.HasValue)
            {
                query = query.Where(t => t.Locked == parameters.Locked.Value);
            }

            // Filter by rollout state
            if (!string.IsNullOrEmpty(parameters.RolloutState))
            {
                query = query.Where(t => t.RolloutState == parameters.RolloutState);
            }

            // Filter by maxfail
            if (parameters.MaxFail.HasValue)
            {
                query = query.Where(t => t.FailCount >= t.MaxFail && t.MaxFail == parameters.MaxFail.Value);
            }

            // Filter by assigned status
            if (parameters.Assigned.HasValue)
            {
                if (parameters.Assigned.Value)
                {
                    query = query.Where(t => t.Owners.Any());
                }
                else
                {
                    query = query.Where(t => !t.Owners.Any());
                }
            }

            // Filter by realm
            if (!string.IsNullOrEmpty(parameters.Realm))
            {
                var realm = parameters.Realm.ToLower();
                if (realm.Contains("*"))
                {
                    var pattern = realm.Replace("*", "%");
                    query = query.Where(t => t.RealmList.Any(tr => 
                        EF.Functions.Like(tr.Realm!.Name.ToLower(), pattern)));
                }
                else
                {
                    query = query.Where(t => t.RealmList.Any(tr => 
                        tr.Realm!.Name.ToLower() == realm));
                }
            }

            // Filter by user
            if (parameters.User != null && !string.IsNullOrEmpty(parameters.User.UserId))
            {
                var userId = parameters.User.UserId;
                var realmId = parameters.User.RealmDatabaseId;
                
                query = query.Where(t => t.Owners.Any(o => 
                    o.UserId == userId && o.RealmId == realmId));
            }

            // Filter by resolver
            if (!string.IsNullOrEmpty(parameters.Resolver))
            {
                var resolver = parameters.Resolver;
                query = query.Where(t => t.Owners.Any(o => o.Resolver == resolver));
            }

            // Filter by tokeninfo
            if (parameters.TokenInfo != null && parameters.TokenInfo.Any())
            {
                foreach (var kvp in parameters.TokenInfo)
                {
                    var key = kvp.Key;
                    var value = kvp.Value;
                    query = query.Where(t => t.InfoList.Any(i => 
                        i.Key == key && i.Value == value));
                }
            }

            return query;
        }

        /// <summary>
        /// Get tokens matching the filter criteria
        /// Equivalent to Python's get_tokens function
        /// </summary>
        /// <param name="parameters">Query parameters</param>
        /// <param name="count">If true, return count instead of tokens</param>
        /// <returns>List of token objects or count</returns>
        public async Task<List<TokenClass>> GetTokensAsync(TokenQueryParameters? parameters = null, bool count = false)
        {
            parameters ??= new TokenQueryParameters();
            
            var query = CreateTokenQuery(parameters);

            var dbTokens = await query.ToListAsync();
            
            var tokenObjects = new List<TokenClass>();
            foreach (var dbToken in dbTokens)
            {
                var tokenObj = CreateTokenClassObject(dbToken);
                if (tokenObj != null)
                {
                    tokenObjects.Add(tokenObj);
                }
            }

            _logger.LogDebug("Retrieved {Count} tokens", tokenObjects.Count);
            return tokenObjects;
        }

        /// <summary>
        /// Get a single token with error handling
        /// Equivalent to Python's get_one_token function
        /// </summary>
        /// <param name="parameters">Query parameters</param>
        /// <param name="silentFail">If true, return null instead of throwing</param>
        /// <returns>Single token object or null</returns>
        public async Task<TokenClass?> GetOneTokenAsync(TokenQueryParameters? parameters = null, bool silentFail = false)
        {
            var tokens = await GetTokensAsync(parameters);

            if (tokens.Count == 0)
            {
                if (silentFail)
                {
                    return null;
                }
                throw new ResourceNotFoundException("No token found matching criteria");
            }

            if (tokens.Count > 1)
            {
                if (silentFail)
                {
                    return tokens[0];
                }
                throw new ParameterException($"More than one token found: {tokens.Count}");
            }

            return tokens[0];
        }

        /// <summary>
        /// Get tokens by serial or user
        /// Equivalent to Python's get_tokens_from_serial_or_user
        /// </summary>
        public async Task<List<TokenClass>> GetTokensFromSerialOrUserAsync(
            string? serial = null, 
            UserIdentity? user = null)
        {
            var parameters = new TokenQueryParameters();

            if (!string.IsNullOrEmpty(serial))
            {
                parameters.SerialWildcard = serial;
            }
            else if (user != null)
            {
                parameters.User = user;
            }

            return await GetTokensAsync(parameters);
        }

        /// <summary>
        /// Check if a token exists
        /// Equivalent to Python's token_exist function
        /// </summary>
        public async Task<bool> TokenExistsAsync(string serial)
        {
            return await _context.Tokens.AnyAsync(t => t.Serial.ToLower() == serial.ToLower());
        }

        /// <summary>
        /// Get the token type for a serial
        /// Equivalent to Python's get_token_type function
        /// </summary>
        public async Task<string?> GetTokenTypeAsync(string serial)
        {
            var token = await _context.Tokens
                .Where(t => t.Serial.ToLower() == serial.ToLower())
                .Select(t => t.TokenType)
                .FirstOrDefaultAsync();

            return token?.ToLowerInvariant();
        }

        /// <summary>
        /// Get the number of tokens in a realm
        /// Equivalent to Python's get_num_tokens_in_realm function
        /// </summary>
        public async Task<int> GetNumTokensInRealmAsync(string realm, bool active = true)
        {
            var query = _context.Tokens
                .Where(t => t.RealmList.Any(tr => tr.Realm!.Name.ToLower() == realm.ToLower()));

            if (active)
            {
                query = query.Where(t => t.Active);
            }

            return await query.CountAsync();
        }

        /// <summary>
        /// Get the realms of a token
        /// Equivalent to Python's get_realms_of_token function
        /// </summary>
        public async Task<List<string>> GetRealmsOfTokenAsync(string serial, bool onlyFirstRealm = false)
        {
            var realms = await _context.TokenRealms
                .Where(tr => tr.Token!.Serial.ToLower() == serial.ToLower())
                .Select(tr => tr.Realm!.Name)
                .ToListAsync();

            if (onlyFirstRealm && realms.Any())
            {
                return new List<string> { realms[0] };
            }

            return realms;
        }

        #endregion

        #region Token Assignment and Ownership

        /// <summary>
        /// Assign a token to a user
        /// Equivalent to Python's assign_token function
        /// </summary>
        public async Task<bool> AssignTokenAsync(
            string serial, 
            UserIdentity user, 
            string? pin = null, 
            bool encryptPin = false,
            string? errorMessage = null)
        {
            _logger.LogInformation("Assigning token {Serial} to user {User}", serial, user.LoginName);

            var token = await _context.Tokens
                .Include(t => t.Owners)
                .Include(t => t.RealmList)
                .FirstOrDefaultAsync(t => t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            // Remove existing owners
            _context.TokenOwners.RemoveRange(token.Owners);

            // Add new owner
            var owner = new Models.Database.TokenOwner
            {
                TokenId = token.Id,
                UserId = user.UserId!,
                RealmId = user.RealmDatabaseId!.Value,
                Resolver = user.ResolverName
            };
            _context.TokenOwners.Add(owner);

            // Set PIN if provided
            if (!string.IsNullOrEmpty(pin))
            {
                await SetPinAsync(serial, pin, user, encryptPin);
            }

            // Set realms based on user realm
            if (user.RealmDatabaseId.HasValue)
            {
                var realmName = await _context.Realms
                    .Where(r => r.Id == user.RealmDatabaseId.Value)
                    .Select(r => r.Name)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(realmName))
                {
                    await SetRealmsAsync(serial, new List<string> { realmName });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Unassign a token from a user
        /// Equivalent to Python's unassign_token function
        /// </summary>
        public async Task<bool> UnassignTokenAsync(string serial, UserIdentity? user = null)
        {
            _logger.LogInformation("Unassigning token {Serial}", serial);

            var token = await _context.Tokens
                .Include(t => t.Owners)
                .FirstOrDefaultAsync(t => t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            if (user != null)
            {
                var owner = token.Owners.FirstOrDefault(o => 
                    o.UserId == user.UserId && o.RealmId == user.RealmDatabaseId);
                
                if (owner != null)
                {
                    _context.TokenOwners.Remove(owner);
                }
            }
            else
            {
                // Remove all owners
                _context.TokenOwners.RemoveRange(token.Owners);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Get the owner of a token
        /// Equivalent to Python's get_token_owner function
        /// </summary>
        public async Task<UserIdentity?> GetTokenOwnerAsync(string serial)
        {
            var owner = await _context.TokenOwners
                .Include(o => o.Realm)
                .Where(o => o.Token!.Serial.ToLower() == serial.ToLower())
                .FirstOrDefaultAsync();

            if (owner == null)
            {
                return null;
            }

            // TODO: Create UserIdentity from owner data
            // This requires access to user service/resolver
            return null;
        }

        /// <summary>
        /// Check if a user is the owner of a token
        /// Equivalent to Python's is_token_owner function
        /// </summary>
        public async Task<bool> IsTokenOwnerAsync(string serial, UserIdentity user)
        {
            return await _context.TokenOwners.AnyAsync(o =>
                o.Token!.Serial.ToLower() == serial.ToLower() &&
                o.UserId == user.UserId &&
                o.RealmId == user.RealmDatabaseId);
        }

        #endregion

        #region Token Configuration

        /// <summary>
        /// Set the realms of a token
        /// Equivalent to Python's set_realms function
        /// </summary>
        public async Task<int> SetRealmsAsync(
            string serial, 
            List<string>? realms = null, 
            bool add = false,
            List<string>? allowedRealms = null)
        {
            _logger.LogInformation("Setting realms for token {Serial}: {Realms}", serial, 
                string.Join(", ", realms ?? new List<string>()));

            var token = await _context.Tokens
                .Include(t => t.RealmList)
                .FirstOrDefaultAsync(t => t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            realms ??= new List<string>();

            if (!add)
            {
                // Remove existing realms
                _context.TokenRealms.RemoveRange(token.RealmList);
            }

            // Add new realms
            foreach (var realmName in realms)
            {
                var realmId = await _realmService.GetRealmIdAsync(realmName);
                if (realmId.HasValue)
                {
                    // Check if allowed
                    if (allowedRealms != null && !allowedRealms.Contains(realmName, StringComparer.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Realm {Realm} not in allowed realms list", realmName);
                        continue;
                    }

                    var tokenRealm = new TokenRealm
                    {
                        TokenId = token.Id,
                        RealmId = realmId.Value
                    };
                    _context.TokenRealms.Add(tokenRealm);
                }
            }

            await _context.SaveChangesAsync();
            return realms.Count;
        }

        /// <summary>
        /// Set token defaults
        /// Equivalent to Python's set_defaults function
        /// </summary>
        public async Task<bool> SetDefaultsAsync(string serial)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            // Set default values
            token.OtpLen = 6;
            token.CountWindow = 10;
            token.SyncWindow = 1000;
            token.MaxFail = 10;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Set the PIN for a token
        /// Equivalent to Python's set_pin function
        /// </summary>
        public async Task<bool> SetPinAsync(
            string serial, 
            string pin, 
            UserIdentity? user = null, 
            bool encryptPin = false)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            var tokenObj = CreateTokenClassObject(token);
            if (tokenObj != null)
            {
                await tokenObj.SetPinAsync(pin, encryptPin);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Set the user PIN for a token
        /// Equivalent to Python's set_pin_user function
        /// </summary>
        public async Task<bool> SetPinUserAsync(string serial, string userPin, UserIdentity? user = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            // TODO: Implement user PIN encryption
            // This should use the crypto functions to encrypt the PIN
            _logger.LogWarning("SetPinUserAsync not fully implemented - requires token class methods");

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Set the SO PIN for a token (smartcard)
        /// Equivalent to Python's set_pin_so function
        /// </summary>
        public async Task<bool> SetPinSoAsync(string serial, string soPin, UserIdentity? user = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            // TODO: Implement SO PIN encryption
            // This should use the crypto functions to encrypt the PIN
            _logger.LogWarning("SetPinSoAsync not fully implemented - requires token class methods");

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Set the OTP length for a token
        /// Equivalent to Python's set_otplen function
        /// </summary>
        public async Task<bool> SetOtpLenAsync(string serial, int otpLen = 6, UserIdentity? user = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            token.OtpLen = otpLen;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Set the hash algorithm for a token
        /// Equivalent to Python's set_hashlib function
        /// </summary>
        public async Task<bool> SetHashLibAsync(string serial, string hashLib = "sha1", UserIdentity? user = null)
        {
            // Hash lib is typically stored in tokeninfo
            await AddTokenInfoAsync(serial, "hashlib", hashLib, user: user);
            return true;
        }

        /// <summary>
        /// Set the sync window for a token
        /// Equivalent to Python's set_sync_window function
        /// </summary>
        public async Task<bool> SetSyncWindowAsync(string serial, int syncWindow = 1000, UserIdentity? user = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            token.SyncWindow = syncWindow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Set the count window for a token
        /// Equivalent to Python's set_count_window function
        /// </summary>
        public async Task<bool> SetCountWindowAsync(string serial, int countWindow = 10, UserIdentity? user = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            token.CountWindow = countWindow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Set the description for a token
        /// Equivalent to Python's set_description function
        /// </summary>
        public async Task<bool> SetDescriptionAsync(string serial, string description, UserIdentity? user = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            token.Description = description;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Set the fail counter for a token
        /// Equivalent to Python's set_failcounter function
        /// </summary>
        public async Task<bool> SetFailCounterAsync(string serial, int counter, UserIdentity? user = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            token.FailCount = counter;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Set the max fail count for a token
        /// Equivalent to Python's set_max_failcount function
        /// </summary>
        public async Task<bool> SetMaxFailCountAsync(string serial, int maxFail, UserIdentity? user = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            token.MaxFail = maxFail;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Set authentication counters
        /// Equivalent to Python's set_count_auth function
        /// </summary>
        public async Task<bool> SetCountAuthAsync(
            string serial, 
            int count, 
            UserIdentity? user = null,
            bool max = false, 
            bool success = false)
        {
            var key = max ? "count_auth_max" : (success ? "count_auth_success" : "count_auth");
            
            // Get current value
            var currentValue = await GetTokenInfoAsync(serial, key);
            var currentCount = string.IsNullOrEmpty(currentValue) ? 0 : int.Parse(currentValue);
            
            var newCount = currentCount + count;
            await AddTokenInfoAsync(serial, key, newCount.ToString(), user: user);
            
            return true;
        }

        /// <summary>
        /// Set validity period start
        /// Equivalent to Python's set_validity_period_start function
        /// </summary>
        public async Task<bool> SetValidityPeriodStartAsync(string serial, UserIdentity user, DateTime start)
        {
            var startStr = start.ToString("o");
            await AddTokenInfoAsync(serial, "validity_period_start", startStr, user: user);
            return true;
        }

        /// <summary>
        /// Set validity period end
        /// Equivalent to Python's set_validity_period_end function
        /// </summary>
        public async Task<bool> SetValidityPeriodEndAsync(string serial, UserIdentity user, DateTime end)
        {
            var endStr = end.ToString("o");
            await AddTokenInfoAsync(serial, "validity_period_end", endStr, user: user);
            return true;
        }

        #endregion

        #region Token State Management

        /// <summary>
        /// Enable or disable a token
        /// Equivalent to Python's enable_token function
        /// </summary>
        public async Task<bool> EnableTokenAsync(string serial, bool enable = true, UserIdentity? user = null)
        {
            _logger.LogInformation("{Action} token {Serial}", enable ? "Enabling" : "Disabling", serial);

            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            token.Active = enable;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Check if a token is active
        /// Equivalent to Python's is_token_active function
        /// </summary>
        public async Task<bool> IsTokenActiveAsync(string serial)
        {
            var token = await _context.Tokens
                .Where(t => t.Serial.ToLower() == serial.ToLower())
                .Select(t => new { t.Active })
                .FirstOrDefaultAsync();

            return token?.Active ?? false;
        }

        /// <summary>
        /// Revoke a token
        /// Equivalent to Python's revoke_token function
        /// </summary>
        public async Task<bool> RevokeTokenAsync(string serial, UserIdentity? user = null)
        {
            _logger.LogInformation("Revoking token {Serial}", serial);

            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            token.Revoked = true;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Reset a token (reset fail counter)
        /// Equivalent to Python's reset_token function
        /// </summary>
        public async Task<bool> ResetTokenAsync(string serial, UserIdentity? user = null)
        {
            _logger.LogInformation("Resetting token {Serial}", serial);

            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            token.FailCount = 0;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Resynchronize a token
        /// Equivalent to Python's resync_token function
        /// </summary>
        public async Task<bool> ResyncTokenAsync(
            string serial, 
            string otp1, 
            string otp2,
            Dictionary<string, object>? options = null, 
            UserIdentity? user = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            // TODO: Implement resync logic with token class
            _logger.LogWarning("ResyncTokenAsync not fully implemented - requires token class factory");
            
            await _context.SaveChangesAsync();
            return false;
        }

        #endregion

        #region Token Info Management

        /// <summary>
        /// Get token info value
        /// Equivalent to Python's get_tokeninfo function
        /// </summary>
        public async Task<string?> GetTokenInfoAsync(string serial, string key)
        {
            var info = await _context.TokenInfos
                .Where(ti => ti.Token!.Serial.ToLower() == serial.ToLower() && ti.Key == key)
                .Select(ti => ti.Value)
                .FirstOrDefaultAsync();

            return info;
        }

        /// <summary>
        /// Add or update token info
        /// Equivalent to Python's add_tokeninfo function
        /// </summary>
        public async Task<bool> AddTokenInfoAsync(
            string serial, 
            string key, 
            string? value = null,
            string? valueType = null, 
            UserIdentity? user = null)
        {
            var token = await _context.Tokens
                .Include(t => t.InfoList)
                .FirstOrDefaultAsync(t => t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            var existing = token.InfoList.FirstOrDefault(i => i.Key == key);
            if (existing != null)
            {
                existing.Value = value;
                if (!string.IsNullOrEmpty(valueType))
                {
                    existing.Type = valueType;
                }
            }
            else
            {
                var info = new TokenInfo
                {
                    TokenId = token.Id,
                    Key = key,
                    Value = value,
                    Type = valueType ?? "string"
                };
                _context.TokenInfos.Add(info);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Delete token info key
        /// Equivalent to Python's delete_tokeninfo function
        /// </summary>
        public async Task<bool> DeleteTokenInfoAsync(string serial, string key, UserIdentity? user = null)
        {
            var info = await _context.TokenInfos
                .Where(ti => ti.Token!.Serial.ToLower() == serial.ToLower() && ti.Key == key)
                .FirstOrDefaultAsync();

            if (info != null)
            {
                _context.TokenInfos.Remove(info);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        #endregion

        #region OTP and Authentication

        /// <summary>
        /// Get the current OTP value for a token
        /// Equivalent to Python's get_otp function
        /// </summary>
        public async Task<(string otp, int pin, string otpCount, int passLength)> GetOtpAsync(
            string serial, 
            string? currentTime = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            // TODO: Implement with token class
            _logger.LogWarning("GetOtpAsync not fully implemented - requires token class factory");
            
            return ("", 0, "0", 6);
        }

        /// <summary>
        /// Get multiple future OTP values
        /// Equivalent to Python's get_multi_otp function
        /// </summary>
        public async Task<Dictionary<string, object>> GetMultiOtpAsync(
            string serial,
            int count = 0,
            long epochStart = 0,
            long epochEnd = 0,
            string? currentTime = null,
            long? timestamp = null)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            // TODO: Implement with token class
            _logger.LogWarning("GetMultiOtpAsync not fully implemented - requires token class factory");
            
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Check an OTP value for a token
        /// Equivalent to Python's check_otp function
        /// </summary>
        public async Task<int> CheckOtpAsync(string serial, string otpVal)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serial.ToLower());

            if (token == null)
            {
                throw new ResourceNotFoundException($"Token {serial} not found");
            }

            var tokenObj = CreateTokenClassObject(token);
            if (tokenObj != null)
            {
                var result = await tokenObj.CheckOtpAsync(otpVal);
                await _context.SaveChangesAsync();
                return result;
            }

            return -1;
        }

        /// <summary>
        /// Check password for a specific serial number
        /// Equivalent to Python's check_serial_pass function
        /// </summary>
        public async Task<Dictionary<string, object>> CheckSerialPassAsync(
            string serial, 
            string password,
            Dictionary<string, object>? options = null)
        {
            _logger.LogDebug("Checking password for serial {Serial}", serial);

            options ??= new Dictionary<string, object>();
            
            var tokens = await GetTokensAsync(new TokenQueryParameters { SerialExact = serial });
            
            if (tokens.Count == 0)
            {
                return new Dictionary<string, object>
                {
                    { "result", false },
                    { "details", new Dictionary<string, object> { { "message", "Token not found" } } }
                };
            }

            return await CheckTokenListAsync(tokens, password, null, options);
        }

        /// <summary>
        /// Check password for a user
        /// Equivalent to Python's check_user_pass function
        /// </summary>
        public async Task<Dictionary<string, object>> CheckUserPassAsync(
            UserIdentity user, 
            string password,
            Dictionary<string, object>? options = null)
        {
            _logger.LogDebug("Checking password for user {User}", user.LoginName);

            options ??= new Dictionary<string, object>();
            
            var tokens = await GetTokensAsync(new TokenQueryParameters { User = user });
            
            if (tokens.Count == 0)
            {
                return new Dictionary<string, object>
                {
                    { "result", false },
                    { "details", new Dictionary<string, object> { { "message", "No tokens found for user" } } }
                };
            }

            return await CheckTokenListAsync(tokens, password, user, options);
        }

        /// <summary>
        /// Core authentication logic - check token list against password
        /// Equivalent to Python's check_token_list function
        /// </summary>
        public async Task<Dictionary<string, object>> CheckTokenListAsync(
            List<TokenClass> tokenList,
            string password,
            UserIdentity? user = null,
            Dictionary<string, object>? options = null,
            bool allowResetAllTokens = false)
        {
            _logger.LogDebug("Checking {Count} tokens", tokenList.Count);

            options ??= new Dictionary<string, object>();
            var result = new Dictionary<string, object>
            {
                { "result", false },
                { "details", new Dictionary<string, object>() }
            };

            // TODO: Implement full authentication logic with token class
            // This requires the token class factory to be implemented
            _logger.LogWarning("CheckTokenListAsync not fully implemented - requires token class factory");

            return result;
        }

        #endregion

        #region Token Copy Operations

        /// <summary>
        /// Copy PIN from one token to another
        /// Equivalent to Python's copy_token_pin function
        /// </summary>
        public async Task<bool> CopyTokenPinAsync(string serialFrom, string serialTo)
        {
            var fromToken = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serialFrom.ToLower());
            var toToken = await _context.Tokens.FirstOrDefaultAsync(t => 
                t.Serial.ToLower() == serialTo.ToLower());

            if (fromToken == null || toToken == null)
            {
                throw new ResourceNotFoundException("One or both tokens not found");
            }

            toToken.UserPin = fromToken.UserPin;
            toToken.UserPinIv = fromToken.UserPinIv;
            toToken.SoPin = fromToken.SoPin;
            toToken.SoPinIv = fromToken.SoPinIv;
            toToken.PinHash = fromToken.PinHash;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Copy user assignment from one token to another
        /// Equivalent to Python's copy_token_user function
        /// </summary>
        public async Task<bool> CopyTokenUserAsync(string serialFrom, string serialTo)
        {
            var fromOwners = await _context.TokenOwners
                .Where(o => o.Token!.Serial.ToLower() == serialFrom.ToLower())
                .ToListAsync();

            var toToken = await _context.Tokens
                .Include(t => t.Owners)
                .FirstOrDefaultAsync(t => t.Serial.ToLower() == serialTo.ToLower());

            if (toToken == null)
            {
                throw new ResourceNotFoundException($"Token {serialTo} not found");
            }

            // Remove existing owners
            _context.TokenOwners.RemoveRange(toToken.Owners);

            // Copy owners
            foreach (var owner in fromOwners)
            {
                var newOwner = new Models.Database.TokenOwner
                {
                    TokenId = toToken.Id,
                    UserId = owner.UserId,
                    RealmId = owner.RealmId,
                    Resolver = owner.Resolver
                };
                _context.TokenOwners.Add(newOwner);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Copy realms from one token to another
        /// Equivalent to Python's copy_token_realms function
        /// </summary>
        public async Task<bool> CopyTokenRealmsAsync(string serialFrom, string serialTo)
        {
            var fromRealms = await _context.TokenRealms
                .Where(tr => tr.Token!.Serial.ToLower() == serialFrom.ToLower())
                .ToListAsync();

            var toToken = await _context.Tokens
                .Include(t => t.RealmList)
                .FirstOrDefaultAsync(t => t.Serial.ToLower() == serialTo.ToLower());

            if (toToken == null)
            {
                throw new ResourceNotFoundException($"Token {serialTo} not found");
            }

            // Remove existing realms
            _context.TokenRealms.RemoveRange(toToken.RealmList);

            // Copy realms
            foreach (var realm in fromRealms)
            {
                var newRealm = new TokenRealm
                {
                    TokenId = toToken.Id,
                    RealmId = realm.RealmId
                };
                _context.TokenRealms.Add(newRealm);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Convert token objects to dictionaries for API responses
        /// Equivalent to Python's convert_token_objects_to_dicts function
        /// </summary>
        public async Task<List<Dictionary<string, object>>> ConvertTokenObjectsToDictsAsync(
            List<TokenClass> tokens,
            UserIdentity? user = null,
            string userRole = "user",
            List<string>? allowedRealms = null,
            List<string>? hiddenTokenInfo = null)
        {
            var result = new List<Dictionary<string, object>>();

            // TODO: Implement full conversion with token class methods
            // This requires the token class factory and GetAsDict method
            _logger.LogWarning("ConvertTokenObjectsToDictsAsync not fully implemented - requires token class factory");

            return result;
        }

        #endregion
    }
}
