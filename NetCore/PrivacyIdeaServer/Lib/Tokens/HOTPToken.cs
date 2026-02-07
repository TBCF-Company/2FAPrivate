// SPDX-FileCopyrightText: (C) 2010-2014 LSE Leading Security Experts GmbH
// SPDX-FileCopyrightText: (C) 2014-2025 Cornelius Kölbel <cornelius.koelbel@netknights.it>
// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/tokens/hotptoken.py
// HOTP Token implementation based on RFC 4226

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Lib.Tokens
{
    /// <summary>
    /// HOTP (HMAC-based One-Time Password) token implementation.
    /// Implements RFC 4226 counter-based OTP tokens.
    /// </summary>
    public class HOTPToken : TokenClass
    {
        private new readonly ILogger<HOTPToken>? _logger;
        
        /// <summary>
        /// The HOTP token can verify enrollment
        /// </summary>
        public override bool CanVerifyEnrollment => true;
        
        /// <summary>
        /// Offset for the previous OTP (1 for HOTP)
        /// </summary>
        protected virtual int PreviousOtpOffset => 1;

        /// <summary>
        /// Initializes a new instance of the HOTPToken class.
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="dbToken">The database token entity</param>
        public HOTPToken(PrivacyIDEAContext context, ILogger logger, Token dbToken) 
            : base(context, logger, dbToken)
        {
            _logger = logger as ILogger<HOTPToken>;
            Type = "hotp";
        }

        /// <summary>
        /// Gets the token type string
        /// </summary>
        public override string? GetClassType() => "hotp";

        /// <summary>
        /// Gets the token serial prefix
        /// </summary>
        public override string GetClassPrefix() => "OATH";

        /// <summary>
        /// Gets the token class info for UI and enrollment
        /// </summary>
        public override Dictionary<string, object> GetClassInfo(string? key = null, string ret = "all")
        {
            return new Dictionary<string, object>
            {
                ["type"] = "hotp",
                ["title"] = "HMAC Event Token",
                ["description"] = "HOTP: Event-based One Time Passwords (RFC 4226)",
                ["init"] = new Dictionary<string, object>
                {
                    ["page"] = new Dictionary<string, object>
                    {
                        ["html"] = "hotptoken.mako",
                        ["scope"] = "enroll"
                    },
                    ["title"] = new Dictionary<string, object>
                    {
                        ["html"] = "hotptoken.mako",
                        ["scope"] = "enroll.title"
                    }
                },
                ["config"] = new Dictionary<string, object>
                {
                    ["page"] = new Dictionary<string, object>
                    {
                        ["html"] = "hotptoken.mako",
                        ["scope"] = "config"
                    },
                    ["title"] = new Dictionary<string, object>
                    {
                        ["html"] = "hotptoken.mako",
                        ["scope"] = "config.title"
                    }
                },
                ["user"] = new List<string> { "enroll" },
                ["policy"] = new Dictionary<string, object>
                {
                    ["hotp_hashlib"] = new Dictionary<string, object>
                    {
                        ["type"] = "str",
                        ["value"] = new List<string> { "sha1", "sha256", "sha512" },
                        ["desc"] = "Specify the hashing function to be used (SHA1, SHA256 or SHA512)"
                    },
                    ["hotp_otplen"] = new Dictionary<string, object>
                    {
                        ["type"] = "int",
                        ["value"] = new List<int> { 6, 8 },
                        ["desc"] = "Specify the OTP length (6 or 8 digits)"
                    }
                }
            };
        }

        /// <summary>
        /// Updates the token with HOTP-specific parameters
        /// </summary>
        /// <param name="parameters">Token parameters from enrollment</param>
        /// <param name="resetFailCount">Whether to reset the fail counter</param>
        public override async Task UpdateAsync(Dictionary<string, object> parameters, bool resetFailCount = true)
        {
            // Get OTP length (default 6)
            if (parameters.TryGetValue("otplen", out var otpLenObj))
            {
                var otpLen = otpLenObj?.ToString() ?? "6";
                await AddTokenInfoAsync("otplen", otpLen);
            }
            else
            {
                await AddTokenInfoAsync("otplen", "6");
            }

            // Get hash algorithm (default sha1)
            var hashLibObj = parameters.GetValueOrDefault("hashlib", "sha1");
            var hashLib = hashLibObj?.ToString()?.ToLower() ?? "sha1";
            await AddTokenInfoAsync("hashlib", hashLib);

            // Get or generate secret key
            if (parameters.TryGetValue("otpkey", out var otpKeyObj))
            {
                var otpKey = otpKeyObj?.ToString();
                if (!string.IsNullOrEmpty(otpKey))
                {
                    await SetOtpKeyAsync(otpKey);
                }
            }
            else if (parameters.TryGetValue("genkey", out var genKeyObj) && genKeyObj?.ToString() == "1")
            {
                // Generate new key based on hash algorithm
                var keyLength = GetKeyLength(hashLib);
                await SetOtpKeyAsync(GenerateOtpKey(keyLength));
            }

            // Set counter
            if (parameters.TryGetValue("counter", out var counterObj))
            {
                var counterStr = counterObj?.ToString();
                if (int.TryParse(counterStr, out var counter))
                {
                    _token.Count = counter;
                }
            }
            else
            {
                _token.Count = 0;
            }

            await base.UpdateAsync(parameters, resetFailCount);
        }

        /// <summary>
        /// Checks if the provided OTP is valid
        /// </summary>
        /// <param name="otp">The OTP to validate</param>
        /// <param name="counter">Optional counter value</param>
        /// <param name="window">The look-ahead window (default 10)</param>
        /// <returns>Counter if valid, -1 if invalid</returns>
        public async Task<int> CheckOtpAsync(string otp, int? counter = null, int window = 10)
        {
            if (string.IsNullOrEmpty(otp))
            {
                return -1;
            }

            var otpKey = await GetOtpKeyAsync();
            if (string.IsNullOrEmpty(otpKey))
            {
                _logger?.LogError("Token {Serial} has no OTP key", _token.Serial);
                return -1;
            }

            var otpLen = await GetOtpLengthAsync();
            var hashMode = await GetHashModeAsync();
            var currentCounter = counter ?? _token.Count;

            try
            {
                // Decode the secret key (base32)
                var keyBytes = Base32Encoding.ToBytes(otpKey);
                
                // Create HOTP instance
                var hotp = new Hotp(keyBytes, mode: hashMode);

                // Check OTP in window
                for (int i = 0; i <= window; i++)
                {
                    var testCounter = currentCounter + i;
                    var computedOtp = hotp.ComputeHOTP(testCounter);
                    var computedOtpStr = computedOtp.ToString().PadLeft(otpLen, '0');

                    if (computedOtpStr == otp)
                    {
                        // Update counter
                        _token.Count = testCounter + 1;
                        await _context.SaveChangesAsync();
                        
                        _logger?.LogInformation("Valid OTP for token {Serial} at counter {Counter}", 
                            _token.Serial, testCounter);
                        
                        return testCounter;
                    }
                }

                _logger?.LogWarning("Invalid OTP for token {Serial}, checked window of {Window}", 
                    _token.Serial, window);
                return -1;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking OTP for token {Serial}", _token.Serial);
                return -1;
            }
        }

        /// <summary>
        /// Authenticates with the token
        /// </summary>
        /// <param name="passw">The password/OTP to check</param>
        /// <param name="user">Optional user information</param>
        /// <param name="options">Authentication options</param>
        /// <returns>Tuple of (counter, pin_match, otp_match, reply)</returns>
        public async Task<(int counter, bool pinMatch, bool otpMatch, Dictionary<string, object>? reply)> 
            AuthenticateWithOtpAsync(string passw, object? user = null, Dictionary<string, object>? options = null)
        {
            var reply = new Dictionary<string, object>();
            var pinMatch = false;
            var otpMatch = false;
            int counter = -1;

            // Split PIN and OTP
            var pin = await GetPinAsync();
            var otpLen = await GetOtpLengthAsync();
            
            string otp;
            if (!string.IsNullOrEmpty(pin))
            {
                // PIN is set, check it
                if (passw.Length >= pin.Length && passw.StartsWith(pin))
                {
                    pinMatch = true;
                    otp = passw.Substring(pin.Length);
                }
                else
                {
                    _logger?.LogWarning("PIN mismatch for token {Serial}", _token.Serial);
                    return (counter, pinMatch, otpMatch, reply);
                }
            }
            else
            {
                // No PIN set
                pinMatch = true;
                otp = passw;
            }

            // Check if OTP has correct length
            if (otp.Length != otpLen)
            {
                _logger?.LogWarning("OTP length mismatch for token {Serial}: expected {Expected}, got {Actual}", 
                    _token.Serial, otpLen, otp.Length);
                return (counter, pinMatch, otpMatch, reply);
            }

            // Get authentication window from options or use default
            var window = 10;
            if (options?.TryGetValue("window", out var windowObj) == true && windowObj is int w)
            {
                window = w;
            }

            // Check the OTP
            counter = await CheckOtpAsync(otp, null, window);
            otpMatch = counter >= 0;

            if (otpMatch)
            {
                reply["message"] = "matching 1 tokens";
            }

            return (counter, pinMatch, otpMatch, reply);
        }

        /// <summary>
        /// Gets the OTP length from token info
        /// </summary>
        private async Task<int> GetOtpLengthAsync()
        {
            var otpLenStr = (await GetTokenInfoAsync("otplen"))?.ToString();
            return int.TryParse(otpLenStr, out var len) ? len : 6;
        }

        /// <summary>
        /// Gets the hash mode for OTP generation
        /// </summary>
        private async Task<OtpHashMode> GetHashModeAsync()
        {
            var hashLib = (await GetTokenInfoAsync("hashlib"))?.ToString() ?? "sha1";
            return hashLib.ToLower() switch
            {
                "sha256" => OtpHashMode.Sha256,
                "sha512" => OtpHashMode.Sha512,
                _ => OtpHashMode.Sha1
            };
        }

        /// <summary>
        /// Gets the key length for a hash algorithm
        /// </summary>
        private int GetKeyLength(string hashLib)
        {
            return hashLib.ToLower() switch
            {
                "sha256" => 32,
                "sha512" => 64,
                _ => 20 // sha1
            };
        }

        /// <summary>
        /// Generates a random OTP key
        /// </summary>
        protected string GenerateOtpKey(int length = 20)
        {
            var key = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return Base32Encoding.ToString(key);
        }

        /// <summary>
        /// Gets the provisioning URI for QR code generation (otpauth://)
        /// </summary>
        /// <param name="user">Username</param>
        /// <param name="issuer">Issuer name</param>
        /// <returns>OTP Auth URI</returns>
        public async Task<string> GetOtpAuthUriAsync(string user, string? issuer = null)
        {
            var secret = await GetOtpKeyAsync();
            var counter = _token.Count;
            var otpLen = await GetOtpLengthAsync();
            var hashLib = (await GetTokenInfoAsync("hashlib"))?.ToString() ?? "sha1";

            var uri = $"otpauth://hotp/{Uri.EscapeDataString(user)}?secret={secret}&counter={counter}&digits={otpLen}&algorithm={hashLib.ToUpper()}";
            
            if (!string.IsNullOrEmpty(issuer))
            {
                uri += $"&issuer={Uri.EscapeDataString(issuer)}";
            }

            return uri;
        }

        /// <summary>
        /// Gets the OTP key from token info
        /// </summary>
        protected async Task<string?> GetOtpKeyAsync()
        {
            return (await GetTokenInfoAsync("otpkey"))?.ToString();
        }

        /// <summary>
        /// Sets the OTP key in token info
        /// </summary>
        public override async Task SetOtpKeyAsync(string otpKey, bool resetFailCount = true)
        {
            await AddTokenInfoAsync("otpkey", otpKey, "password");
            if (resetFailCount)
            {
                _token.FailCount = 0;
            }
            await SaveAsync();
        }

        /// <summary>
        /// Gets the PIN for this token (empty if no PIN is set)
        /// NOTE: PIN functionality is not yet fully implemented. The Token model
        /// stores PIN in encrypted form (UserPin) or as a hash (PinHash), but
        /// decryption/validation logic needs to be implemented in a future update.
        /// Currently returns empty string, meaning all tokens operate without PIN.
        /// </summary>
        protected async Task<string> GetPinAsync()
        {
            // For now, return empty string as PIN is stored in UserPin (encrypted) or PinHash
            // TODO: Implement proper PIN decryption/validation
            await Task.CompletedTask;
            return string.Empty;
        }
    }
}
