// SPDX-FileCopyrightText: (C) 2010-2014 LSE Leading Security Experts GmbH
// SPDX-FileCopyrightText: (C) 2014-2025 Cornelius Kölbel <cornelius.koelbel@netknights.it>
// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/tokenclass.py
// This is the Token Base class, which is inherited by all token types.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;
using PrivacyIdeaServer.Lib.Crypto;
using PrivacyIdeaServer.Lib.Users;

namespace PrivacyIdeaServer.Lib.Tokens
{
    /// <summary>
    /// Date format for general dates
    /// </summary>
    public static class TokenConstants
    {
        public const string DateFormat = "yyyy-MM-ddTHH:mmzzz";
        public const string AuthDateFormat = "yyyy-MM-dd HH:mm:ss.ffffffzzz";
        public const string FailCounterExceeded = "failcounter_exceeded";
        public const string FailCounterClearTimeout = "failcounter_clear_timeout";
        public const int TwoStepDefaultClientSize = 8;
        public const int TwoStepDefaultDifficulty = 10000;
    }

    /// <summary>
    /// Challenge session states
    /// </summary>
    public static class ChallengeSession
    {
        public const string Enrollment = "enrollment";
        public const string Declined = "challenge_declined";
    }

    /// <summary>
    /// Token kinds (hardware, software, virtual)
    /// </summary>
    public static class TokenKind
    {
        public const string Software = "software";
        public const string Hardware = "hardware";
        public const string Virtual = "virtual";
    }

    /// <summary>
    /// Authentication modes
    /// </summary>
    public static class AuthenticationMode
    {
        public const string Authenticate = "authenticate";
        public const string Challenge = "challenge";
        public const string OutOfBand = "outofband";
    }

    /// <summary>
    /// Client modes for challenge-response
    /// </summary>
    public static class ClientMode
    {
        public const string Interactive = "interactive";
        public const string Poll = "poll";
        public const string U2F = "u2f";
        public const string WebAuthn = "webauthn";
    }

    /// <summary>
    /// Token rollout states
    /// </summary>
    public static class RolloutState
    {
        public const string ClientWait = "clientwait";
        public const string Pending = "pending";
        public const string VerifyPending = "verify";
        public const string Enrolled = "enrolled";
        public const string Broken = "broken";
        public const string Failed = "failed";
        public const string Denied = "denied";
    }

    /// <summary>
    /// Interface for token classes - defines methods that token types must implement
    /// </summary>
    public interface ITokenClass
    {
        /// <summary>
        /// Get the token type string
        /// </summary>
        string? GetClassType();

        /// <summary>
        /// Get class information dictionary
        /// </summary>
        Dictionary<string, object> GetClassInfo(string? key = null, string ret = "all");

        /// <summary>
        /// Get the token type prefix
        /// </summary>
        string GetClassPrefix();

        /// <summary>
        /// Check OTP value
        /// </summary>
        Task<int> CheckOtpAsync(string otpval, int? counter = null, int? window = null, Dictionary<string, object>? options = null);

        /// <summary>
        /// Get OTP value(s)
        /// </summary>
        Task<(int, string, string, string)> GetOtpAsync(string currentTime = "");
    }

    /// <summary>
    /// Base class for all token types. Contains core token functionality including
    /// enrollment, validation, challenge-response, and database operations.
    /// </summary>
    public abstract class TokenClass : ITokenClass
    {
        protected readonly PrivacyIDEAContext _context;
        protected readonly ILogger _logger;
        protected readonly Token _token;

        // Class properties
        public virtual bool UsingPin => true;
        public virtual bool HKeyRequired => false;
        public virtual string[] Mode => new[] { AuthenticationMode.Authenticate, AuthenticationMode.Challenge };
        public virtual string ClientMode => Tokens.ClientMode.Interactive;
        public virtual bool CanVerifyEnrollment => false;
        public virtual string DescKeyGen => "Force the key to be generated on the server.";

        // Instance properties
        public string Type { get; protected set; }
        public Dictionary<string, object> InitDetails { get; protected set; }
        public Dictionary<string, object> AuthDetails { get; protected set; }

        /// <summary>
        /// Create a new token object
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="token">Database token object</param>
        public TokenClass(PrivacyIDEAContext context, ILogger logger, Token token)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _token = token ?? throw new ArgumentNullException(nameof(token));

            Type = token.TokenType ?? "unknown";
            InitDetails = new Dictionary<string, object>();
            AuthDetails = new Dictionary<string, object>();
        }

        #region Abstract and Virtual Interface Methods

        /// <summary>
        /// Get the token type string. Override in derived classes.
        /// </summary>
        public virtual string? GetClassType() => null;

        /// <summary>
        /// Get class information dictionary. Override in derived classes.
        /// </summary>
        public virtual Dictionary<string, object> GetClassInfo(string? key = null, string ret = "all")
        {
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Get the token type prefix. Override in derived classes.
        /// </summary>
        public virtual string GetClassPrefix() => "UNK";

        /// <summary>
        /// Check if this token class uses out-of-band authentication
        /// </summary>
        public virtual bool IsOutOfBand() => Mode.Contains(AuthenticationMode.OutOfBand);

        #endregion

        #region Token Type and Serial Methods

        /// <summary>
        /// Set the token type in this object and in the database
        /// </summary>
        public async Task SetTypeAsync(string tokenType)
        {
            Type = tokenType;
            _token.TokenType = tokenType;
            await SaveAsync();
        }

        /// <summary>
        /// Get the token type
        /// </summary>
        public string? GetTokenType() => _token.TokenType;

        /// <summary>
        /// Get the serial number of the token
        /// </summary>
        public string GetSerial() => _token.Serial;

        #endregion

        #region OTP Methods

        /// <summary>
        /// Check the OTP value AFTER the upper level did the checkPIN.
        /// In the base class we do not know how to calculate the OTP value, so we return -1.
        /// In case of success, derived classes should return >= 0, the counter.
        /// </summary>
        /// <param name="otpval">The OTP value</param>
        /// <param name="counter">The counter for counter-based OTP values</param>
        /// <param name="window">A counter window</param>
        /// <param name="options">Additional token-specific options</param>
        /// <returns>Counter of the matching OTP value, or -1 on failure</returns>
        public virtual async Task<int> CheckOtpAsync(string otpval, int? counter = null, int? window = null, Dictionary<string, object>? options = null)
        {
            await Task.CompletedTask;
            
            if (!counter.HasValue)
                counter = _token.Count;
            if (!window.HasValue)
                window = _token.CountWindow;

            return -1;
        }

        /// <summary>
        /// The default token does not support getting the OTP value.
        /// Returns a tuple of four values; a negative value indicates failure.
        /// </summary>
        /// <returns>Tuple of (result, pin, otpval, combined)</returns>
        public virtual async Task<(int, string, string, string)> GetOtpAsync(string currentTime = "")
        {
            await Task.CompletedTask;
            return (-2, "0", "0", "0");
        }

        /// <summary>
        /// Get multiple future OTP values of a token
        /// </summary>
        public virtual async Task<(bool, string, Dictionary<string, string>)> GetMultiOtpAsync(
            int count = 0, long epochStart = 0, long epochEnd = 0, DateTime? curTime = null, long? timestamp = null)
        {
            await Task.CompletedTask;
            return (false, "get_multi_otp not implemented for this tokentype", new Dictionary<string, string>());
        }

        /// <summary>
        /// Set the OTP key
        /// </summary>
        public virtual async Task SetOtpKeyAsync(string otpKey, bool resetFailCount = true)
        {
            // Implementation would call Token.SetOtpKey equivalent
            // For now, placeholder
            await SaveAsync();
        }

        /// <summary>
        /// Set the OTP length
        /// </summary>
        public async Task SetOtpLenAsync(int otpLen)
        {
            _token.OtpLen = otpLen;
            await SaveAsync();
        }

        /// <summary>
        /// Get the OTP length
        /// </summary>
        public int GetOtpLen() => _token.OtpLen;

        /// <summary>
        /// Set the OTP counter
        /// </summary>
        public async Task SetOtpCountAsync(int otpCount)
        {
            _token.Count = otpCount;
            await SaveAsync();
        }

        #endregion

        #region PIN Methods

        /// <summary>
        /// Set the PIN of a token. Usually the PIN is stored in a hashed way.
        /// </summary>
        /// <param name="pin">The PIN to be set for the token</param>
        /// <param name="encrypt">If true, the PIN is stored encrypted and can be retrieved</param>
        public virtual async Task SetPinAsync(string pin, bool encrypt = false)
        {
            bool storeHashed = !encrypt;
            // Token.SetPin equivalent would be called here
            // Implementation depends on Token model methods
            await SaveAsync();
        }

        /// <summary>
        /// Get the PIN hash and seed
        /// </summary>
        public (string?, string?) GetPinHashSeed()
        {
            return (_token.PinHash, _token.PinSeed);
        }

        /// <summary>
        /// Set the PIN hash and seed
        /// </summary>
        public async Task SetPinHashSeedAsync(string? pinHash, string? seed)
        {
            _token.PinHash = pinHash;
            _token.PinSeed = seed;
            await SaveAsync();
        }

        /// <summary>
        /// Check if the provided PIN matches the token PIN
        /// </summary>
        public virtual async Task<bool> CheckPinAsync(string pin, UserIdentity? user = null, Dictionary<string, object>? options = null)
        {
            // Base implementation - override in derived classes for policy-based PIN checking
            await Task.CompletedTask;
            
            if (string.IsNullOrEmpty(_token.PinHash))
                return true;

            // TODO: Implement proper PIN hash verification
            return false;
        }

        #endregion

        #region Token State Methods

        /// <summary>
        /// Check if the token is active
        /// </summary>
        public bool IsActive() => _token.Active;

        /// <summary>
        /// Enable or disable the token
        /// </summary>
        public async Task EnableAsync(bool enable = true)
        {
            _token.Active = enable;
            await SaveAsync();
        }

        /// <summary>
        /// Revoke the token - sets revoked, locked, and inactive
        /// </summary>
        public virtual async Task RevokeAsync()
        {
            _token.Revoked = true;
            _token.Locked = true;
            _token.Active = false;
            await SaveAsync();
        }

        /// <summary>
        /// Check if the token is revoked
        /// </summary>
        public bool IsRevoked() => _token.Revoked;

        /// <summary>
        /// Check if the token is locked
        /// </summary>
        public bool IsLocked() => _token.Locked;

        /// <summary>
        /// Get the rollout state
        /// </summary>
        public string? RolloutState => _token.RolloutState;

        #endregion

        #region Fail Counter Methods

        /// <summary>
        /// Reset the fail counter
        /// </summary>
        public async Task ResetAsync()
        {
            if (_token.FailCount > 0)
            {
                await SetFailCountAsync(0);
                await SaveAsync();
            }
        }

        /// <summary>
        /// Get the fail count
        /// </summary>
        public int GetFailCount() => _token.FailCount;

        /// <summary>
        /// Set the fail counter in the database
        /// </summary>
        public async Task SetFailCountAsync(int failCount)
        {
            _token.FailCount = failCount;
            if (failCount == 0)
            {
                await DeleteTokenInfoAsync(TokenConstants.FailCounterExceeded);
            }
        }

        /// <summary>
        /// Increment the fail counter
        /// </summary>
        public async Task<int> IncFailCountAsync()
        {
            if (_token.FailCount < _token.MaxFail)
            {
                _token.FailCount++;
                if (_token.FailCount == _token.MaxFail)
                {
                    await AddTokenInfoAsync(
                        TokenConstants.FailCounterExceeded,
                        DateTimeOffset.Now.ToString(TokenConstants.DateFormat));
                }
            }

            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token fail counter update failed");
                throw new TokenAdminError("Token Fail Counter update failed", ErrorCodes.TOKENADMIN);
            }

            return _token.FailCount;
        }

        /// <summary>
        /// Get the maximum fail count
        /// </summary>
        public int GetMaxFailCount() => _token.MaxFail;

        /// <summary>
        /// Set the maximum fail count
        /// </summary>
        public async Task SetMaxFailAsync(int maxFail)
        {
            _token.MaxFail = maxFail;
            await SaveAsync();
        }

        #endregion

        #region Counter Window Methods

        /// <summary>
        /// Set the counter window
        /// </summary>
        public async Task SetCountWindowAsync(int countWindow)
        {
            _token.CountWindow = countWindow;
            await SaveAsync();
        }

        /// <summary>
        /// Get the counter window
        /// </summary>
        public int GetCountWindow() => _token.CountWindow;

        /// <summary>
        /// Set the sync window
        /// </summary>
        public async Task SetSyncWindowAsync(int syncWindow)
        {
            _token.SyncWindow = syncWindow;
            await SaveAsync();
        }

        /// <summary>
        /// Get the sync window
        /// </summary>
        public int GetSyncWindow() => _token.SyncWindow;

        #endregion

        #region Token Info Methods

        /// <summary>
        /// Add a key-value pair to the token info
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        /// <param name="valueType">If "password", the value will be encrypted</param>
        /// <param name="commitDbSession">Whether to commit immediately</param>
        public async Task AddTokenInfoAsync(string key, string value, string? valueType = null, bool commitDbSession = true)
        {
            if (valueType == "password")
            {
                // TODO: Implement password encryption
                // value = EncryptPassword(value);
            }

            var tokenInfo = await _context.TokenInfos
                .FirstOrDefaultAsync(ti => ti.TokenId == _token.Id && ti.Key == key);

            if (tokenInfo == null)
            {
                tokenInfo = new TokenInfo(_token.Id, key, value, valueType ?? "string");
                await _context.TokenInfos.AddAsync(tokenInfo);
            }
            else
            {
                tokenInfo.Value = value;
                tokenInfo.Type = valueType;
            }

            if (commitDbSession)
            {
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Add multiple token info entries from a dictionary
        /// </summary>
        public async Task AddTokenInfoDictAsync(Dictionary<string, string> info)
        {
            foreach (var kvp in info.Where(x => !x.Key.EndsWith(".type")))
            {
                var valueType = info.ContainsKey($"{kvp.Key}.type") ? info[$"{kvp.Key}.type"] : null;
                await AddTokenInfoAsync(kvp.Key, kvp.Value, valueType, false);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Get token info - either a single key or the complete dictionary
        /// </summary>
        /// <param name="key">The key to retrieve (null for all)</param>
        /// <param name="defaultValue">Default value if key doesn't exist</param>
        /// <param name="decrypted">Whether to decrypt password values</param>
        public async Task<object?> GetTokenInfoAsync(string? key = null, object? defaultValue = null, bool decrypted = false)
        {
            var tokenInfoList = await _context.TokenInfos
                .Where(ti => ti.TokenId == _token.Id)
                .ToListAsync();

            var tokenInfo = tokenInfoList.ToDictionary(ti => ti.Key, ti => ti.Value ?? string.Empty);

            if (key != null)
            {
                if (!tokenInfo.ContainsKey(key))
                    return defaultValue;

                var value = tokenInfo[key];
                var keyType = tokenInfo.ContainsKey(key + ".type") ? tokenInfo[key + ".type"] : null;

                if (keyType == "password")
                {
                    // TODO: Implement password decryption
                    // value = DecryptPassword(value);
                }

                return value;
            }
            else if (decrypted)
            {
                // Decrypt all password values
                var result = new Dictionary<string, string>();
                foreach (var kvp in tokenInfo.Where(x => !x.Key.EndsWith(".type")))
                {
                    var valueType = tokenInfo.ContainsKey(kvp.Key + ".type") ? tokenInfo[kvp.Key + ".type"] : null;
                    result[kvp.Key] = valueType == "password" ? kvp.Value : kvp.Value; // TODO: decrypt
                }
                return result;
            }

            return tokenInfo;
        }

        /// <summary>
        /// Delete token info for a given key or all info if key is null
        /// </summary>
        public async Task DeleteTokenInfoAsync(string? key = null)
        {
            var query = _context.TokenInfos.Where(ti => ti.TokenId == _token.Id);

            if (key != null)
            {
                query = query.Where(ti => ti.Key == key);
            }

            _context.TokenInfos.RemoveRange(query);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region User and Owner Methods

        /// <summary>
        /// Get the first owner of the token
        /// </summary>
        public async Task<UserIdentity?> GetUserAsync()
        {
            var tokenOwner = await _context.TokenOwners
                .Include(to => to.Realm)
                .FirstOrDefaultAsync(to => to.TokenId == _token.Id);

            if (tokenOwner == null)
                return null;

            // TODO: Create UserIdentity from tokenOwner
            // This requires UserIdentity constructor access
            return null;
        }

        /// <summary>
        /// Get all owners of the token
        /// </summary>
        public async Task<List<UserIdentity>> GetOwnersAsync()
        {
            var tokenOwners = await _context.TokenOwners
                .Include(to => to.Realm)
                .Where(to => to.TokenId == _token.Id)
                .ToListAsync();

            var users = new List<UserIdentity>();
            // TODO: Convert tokenOwners to UserIdentity objects
            return users;
        }

        /// <summary>
        /// Get all owner realms
        /// </summary>
        public async Task<HashSet<string>> GetOwnerRealmsAsync()
        {
            var realmsList = await _context.TokenOwners
                .Include(to => to.Realm)
                .Where(to => to.TokenId == _token.Id && to.Realm != null)
                .Select(to => to.Realm!.Name)
                .ToListAsync();

            return realmsList.ToHashSet();
        }

        /// <summary>
        /// Get the user ID of the first owner
        /// </summary>
        public async Task<string> GetUserIdAsync()
        {
            var tokenOwner = await _context.TokenOwners
                .FirstOrDefaultAsync(to => to.TokenId == _token.Id);

            return tokenOwner?.UserId ?? string.Empty;
        }

        /// <summary>
        /// Remove the user (owner) of a token
        /// </summary>
        public async Task<UserIdentity?> RemoveUserAsync()
        {
            var user = await GetUserAsync();
            if (user != null)
            {
                var tokenOwner = await _context.TokenOwners
                    .FirstOrDefaultAsync(to => to.TokenId == _token.Id);

                if (tokenOwner != null)
                {
                    _context.TokenOwners.Remove(tokenOwner);
                    await _context.SaveChangesAsync();
                }
            }
            return user;
        }

        #endregion

        #region Init Details Methods

        /// <summary>
        /// Add information to the volatile init details dictionary
        /// </summary>
        public Dictionary<string, object> AddInitDetails(string key, object value)
        {
            InitDetails[key] = value;
            return InitDetails;
        }

        /// <summary>
        /// Set the init details dictionary
        /// </summary>
        public Dictionary<string, object> SetInitDetails(Dictionary<string, object> details)
        {
            if (details == null)
                throw new ArgumentException("Details must be a dictionary");

            InitDetails = details;
            return InitDetails;
        }

        /// <summary>
        /// Get the init details
        /// </summary>
        public Dictionary<string, object> GetInitDetails() => InitDetails;

        /// <summary>
        /// Get initialization details to complete token initialization.
        /// This method is called from the API after the token is enrolled.
        /// </summary>
        /// <param name="params">Request params during token creation</param>
        /// <param name="user">The user, token owner</param>
        /// <returns>Additional descriptions</returns>
        public virtual async Task<Dictionary<string, object>> GetInitDetailAsync(
            Dictionary<string, object>? @params = null, UserIdentity? user = null)
        {
            await Task.CompletedTask;

            var responseDetail = new Dictionary<string, object>(InitDetails)
            {
                ["serial"] = GetSerial()
            };

            if (InitDetails.ContainsKey("otpkey"))
            {
                var otpKey = InitDetails["otpkey"]?.ToString();
                if (!string.IsNullOrEmpty(otpKey))
                {
                    responseDetail["otpkey"] = new Dictionary<string, object>
                    {
                        ["description"] = "OTP seed",
                        ["value"] = $"seed://{otpKey}",
                        // TODO: Add image generation
                        // ["img"] = CreateImg(otpKey)
                    };
                }
            }

            return responseDetail;
        }

        #endregion

        #region Update Method

        /// <summary>
        /// Update the token object with parameters
        /// </summary>
        /// <param name="param">Dictionary with parameters like keysize, description, genkey, otpkey, pin</param>
        /// <param name="resetFailCount">Whether to reset fail count when setting OTP key</param>
        public virtual async Task UpdateAsync(Dictionary<string, object> param, bool resetFailCount = true)
        {
            // Description
            if (param.ContainsKey("description") && param["description"] != null)
            {
                _token.Description = param["description"].ToString();
            }

            // Key size
            var keySize = 20;
            if (param.ContainsKey("keysize") && param["keysize"] != null)
            {
                keySize = Convert.ToInt32(param["keysize"]);
            }

            // OTP key processing
            string? otpKey = param.ContainsKey("otpkey") ? param["otpkey"]?.ToString() : null;
            var genKey = param.ContainsKey("genkey") && IsTrue(param["genkey"]);
            var twoStepInit = param.ContainsKey("2stepinit") && IsTrue(param["2stepinit"]);
            var verify = param.ContainsKey("verify") ? param["verify"]?.ToString() : null;
            var otpKeyFormat = param.ContainsKey("otpkeyformat") ? param["otpkeyformat"]?.ToString() : null;

            if (otpKey != null && otpKeyFormat != null)
            {
                // Decode OTP key
                otpKey = DecodeOtpKey(otpKey, otpKeyFormat);
            }

            var rollover = param.ContainsKey("rollover") && IsTrue(param["rollover"]);

            if (twoStepInit)
            {
                if (rollover)
                {
                    _token.RolloutState = null;
                }

                if (_token.RolloutState == Tokens.RolloutState.ClientWait)
                {
                    throw new ParameterError("2stepinit is only to be used in the first initialization step.");
                }

                genKey = true;
                _token.Active = false;
            }

            if (genKey && otpKey != null)
            {
                throw new ParameterError("You may either specify genkey or otpkey, but not both!", ErrorCodes.PARAMETER);
            }

            if (otpKey == null && genKey && string.IsNullOrEmpty(verify))
            {
                otpKey = GenerateOtpKey(keySize);
            }

            if (otpKey == null && HKeyRequired && string.IsNullOrEmpty(verify))
            {
                throw new ParameterError("OTP key is required for this token type");
            }

            if (otpKey != null)
            {
                if (_token.RolloutState == Tokens.RolloutState.ClientWait)
                {
                    // Generate symmetric key from server and client components
                    var serverComponent = ""; // TODO: Get from token
                    var clientComponent = otpKey;
                    otpKey = GenerateSymmetricKey(serverComponent, clientComponent, param);
                    _token.RolloutState = string.Empty;
                    _token.Active = true;
                }

                AddInitDetails("otpkey", otpKey);
                await SetOtpKeyAsync(otpKey, resetFailCount);
            }

            if (twoStepInit)
            {
                _token.RolloutState = Tokens.RolloutState.ClientWait;
            }

            // PIN
            if (param.ContainsKey("pin") && param["pin"] != null)
            {
                var pin = param["pin"].ToString() ?? string.Empty;
                var encryptPin = param.ContainsKey("encryptpin") && IsTrue(param["encryptpin"]);
                await SetPinAsync(pin, encryptPin);
            }

            await SaveAsync();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if a value is true (supports various string representations)
        /// </summary>
        protected bool IsTrue(object? value)
        {
            if (value == null) return false;
            if (value is bool b) return b;
            if (value is int i) return i != 0;

            var str = value.ToString()?.ToLowerInvariant();
            return str == "true" || str == "1" || str == "yes" || str == "on";
        }

        /// <summary>
        /// Generate an OTP key
        /// </summary>
        protected virtual string GenerateOtpKey(int keySize)
        {
            var bytes = new byte[keySize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToHexString(bytes);
        }

        /// <summary>
        /// Decode OTP key from various formats
        /// </summary>
        protected virtual string DecodeOtpKey(string otpKey, string format)
        {
            // TODO: Implement format decoding (base32, hex, etc.)
            return otpKey;
        }

        /// <summary>
        /// Generate symmetric key from server and client components
        /// </summary>
        protected virtual string GenerateSymmetricKey(string serverComponent, string clientComponent, Dictionary<string, object> param)
        {
            // TODO: Implement symmetric key generation
            return serverComponent + clientComponent;
        }

        /// <summary>
        /// Save the token to the database
        /// </summary>
        protected async Task SaveAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save token {Serial}", _token.Serial);
                throw;
            }
        }

        #endregion

        #region Challenge-Response Methods (Virtual)

        /// <summary>
        /// Check if this is a challenge request
        /// </summary>
        public virtual async Task<bool> IsChallengeRequestAsync(string passw, UserIdentity? user = null, Dictionary<string, object>? options = null)
        {
            var pinMatch = await CheckPinAsync(passw, user, options);
            return pinMatch;
        }

        /// <summary>
        /// Check if this is a challenge response
        /// </summary>
        public virtual async Task<bool> IsChallengeResponseAsync(string passw, UserIdentity? user = null, Dictionary<string, object>? options = null)
        {
            await Task.CompletedTask;
            options ??= new Dictionary<string, object>();
            return options.ContainsKey("state") || options.ContainsKey("transactionid");
        }

        /// <summary>
        /// Check if the token is fit for challenge
        /// </summary>
        public virtual async Task<bool> IsFitForChallengeAsync(List<string> messages, Dictionary<string, object>? options = null)
        {
            await Task.CompletedTask;
            return true;
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Check if this token should be used for authentication
        /// </summary>
        public virtual bool UseForAuthentication(Dictionary<string, object>? options = null)
        {
            return true;
        }

        /// <summary>
        /// Check if the token is orphaned (owner doesn't exist in user store)
        /// </summary>
        public virtual async Task<bool> IsOrphanedAsync(bool orphanedOnError = true)
        {
            var user = await GetUserAsync();
            if (user == null)
                return false;

            try
            {
                // TODO: Verify user still exists in resolver
                return false;
            }
            catch (Exception)
            {
                return orphanedOnError;
            }
        }

        #endregion
    }
}
