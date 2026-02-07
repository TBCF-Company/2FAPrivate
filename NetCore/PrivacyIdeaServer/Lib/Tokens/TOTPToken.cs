// SPDX-FileCopyrightText: (C) 2015 Cornelius Kölbel <cornelius@privacyidea.org>
// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/tokens/totptoken.py
// TOTP Token implementation based on RFC 6238

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OtpNet;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Lib.Tokens
{
    /// <summary>
    /// TOTP (Time-based One-Time Password) token implementation.
    /// Implements RFC 6238 time-based OTP tokens.
    /// Inherits from HOTPToken and overrides time-specific behavior.
    /// </summary>
    public class TOTPToken : HOTPToken
    {
        private new readonly ILogger<TOTPToken>? _logger;

        /// <summary>
        /// The TOTP counter contains the last used OTP value, not the next one
        /// </summary>
        protected override int PreviousOtpOffset => 0;

        /// <summary>
        /// Initializes a new instance of the TOTPToken class.
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="dbToken">The database token entity</param>
        public TOTPToken(PrivacyIDEAContext context, ILogger logger, Token dbToken)
            : base(context, logger, dbToken)
        {
            _logger = logger as ILogger<TOTPToken>;
            Type = "totp";
        }

        /// <summary>
        /// Gets the token type string
        /// </summary>
        public override string? GetClassType() => "totp";

        /// <summary>
        /// Gets the token serial prefix
        /// </summary>
        public override string GetClassPrefix() => "TOTP";

        /// <summary>
        /// Gets the token class info for UI and enrollment
        /// </summary>
        public override Dictionary<string, object> GetClassInfo(string? key = null, string ret = "all")
        {
            return new Dictionary<string, object>
            {
                ["type"] = "totp",
                ["title"] = "HMAC Time Token",
                ["description"] = "TOTP: Time-based One Time Passwords (RFC 6238)",
                ["init"] = new Dictionary<string, object>
                {
                    ["page"] = new Dictionary<string, object>
                    {
                        ["html"] = "totptoken.mako",
                        ["scope"] = "enroll"
                    },
                    ["title"] = new Dictionary<string, object>
                    {
                        ["html"] = "totptoken.mako",
                        ["scope"] = "enroll.title"
                    }
                },
                ["config"] = new Dictionary<string, object>
                {
                    ["page"] = new Dictionary<string, object>
                    {
                        ["html"] = "totptoken.mako",
                        ["scope"] = "config"
                    },
                    ["title"] = new Dictionary<string, object>
                    {
                        ["html"] = "totptoken.mako",
                        ["scope"] = "config.title"
                    }
                },
                ["user"] = new List<string> { "enroll" },
                ["policy"] = new Dictionary<string, object>
                {
                    ["totp_hashlib"] = new Dictionary<string, object>
                    {
                        ["type"] = "str",
                        ["value"] = new List<string> { "sha1", "sha256", "sha512" },
                        ["desc"] = "Specify the hashing function to be used (SHA1, SHA256 or SHA512)"
                    },
                    ["totp_otplen"] = new Dictionary<string, object>
                    {
                        ["type"] = "int",
                        ["value"] = new List<int> { 6, 8 },
                        ["desc"] = "Specify the OTP length (6 or 8 digits)"
                    },
                    ["totp_timestep"] = new Dictionary<string, object>
                    {
                        ["type"] = "int",
                        ["value"] = new List<int> { 30, 60 },
                        ["desc"] = "Specify the time step in seconds (default 30)"
                    }
                }
            };
        }

        /// <summary>
        /// Updates the token with TOTP-specific parameters
        /// </summary>
        /// <param name="parameters">Token parameters from enrollment</param>
        /// <param name="resetFailCount">Whether to reset the fail counter</param>
        public override async Task UpdateAsync(Dictionary<string, object> parameters, bool resetFailCount = true)
        {
            // Call base HOTP update
            await base.UpdateAsync(parameters, resetFailCount);

            // Get time step (default 30 seconds)
            if (parameters.TryGetValue("timeStep", out var timeStepObj))
            {
                var timeStep = timeStepObj?.ToString() ?? "30";
                await AddTokenInfoAsync("timeStep", timeStep);
            }
            else
            {
                await AddTokenInfoAsync("timeStep", "30");
            }

            // TOTP uses time-based counter, not manual counter
            _token.Count = 0;
        }

        /// <summary>
        /// Checks if the provided OTP is valid for TOTP
        /// </summary>
        /// <param name="otp">The OTP to validate</param>
        /// <param name="counter">Optional Unix timestamp (seconds since epoch)</param>
        /// <param name="window">The time window in steps (default 3 means ±3 time steps)</param>
        /// <returns>Timestamp if valid, -1 if invalid</returns>
        public new async Task<long> CheckOtpAsync(string otp, long? counter = null, int window = 3)
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
            var timeStep = await GetTimeStepAsync();
            
            // Get current time or use provided timestamp
            var currentTime = counter ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            try
            {
                // Decode the secret key (base32)
                var keyBytes = Base32Encoding.ToBytes(otpKey);
                
                // Create TOTP instance
                var totp = new Totp(keyBytes, step: timeStep, mode: hashMode, totpSize: otpLen);

                // Calculate current time step
                var currentStep = currentTime / timeStep;

                // Check OTP in time window
                for (long i = -window; i <= window; i++)
                {
                    var testStep = currentStep + i;
                    var testTime = testStep * timeStep;
                    
                    var computedOtp = totp.ComputeTotp(DateTimeOffset.FromUnixTimeSeconds(testTime).UtcDateTime);

                    if (computedOtp == otp)
                    {
                        // Check if this OTP was already used (replay protection)
                        var lastAuth = _token.Count;
                        if (testTime <= lastAuth)
                        {
                            _logger?.LogWarning("Replay attempt detected for token {Serial}, timestamp {Time} <= {LastAuth}",
                                _token.Serial, testTime, lastAuth);
                            return -1;
                        }

                        // Update last authentication timestamp
                        _token.Count = (int)testTime;
                        await _context.SaveChangesAsync();
                        
                        _logger?.LogInformation("Valid TOTP for token {Serial} at time {Time} (step {Step})", 
                            _token.Serial, testTime, testStep);
                        
                        return testTime;
                    }
                }

                _logger?.LogWarning("Invalid TOTP for token {Serial}, checked window of ±{Window} steps", 
                    _token.Serial, window);
                return -1;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking TOTP for token {Serial}", _token.Serial);
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
        public new async Task<(long counter, bool pinMatch, bool otpMatch, Dictionary<string, object>? reply)>
            AuthenticateWithOtpAsync(string passw, object? user = null, Dictionary<string, object>? options = null)
        {
            var reply = new Dictionary<string, object>();
            var pinMatch = false;
            var otpMatch = false;
            long counter = -1;

            // Split PIN and OTP
            var pin = await GetPinAsync();
            var otpLen = await GetOtpLengthAsync();
            
            string otp;
            if (!string.IsNullOrEmpty(pin))
            {
                // PIN is set, check it
                if (passw.StartsWith(pin))
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

            // Get time window from options or use default (±3 steps)
            var window = 3;
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
        /// Gets the time step from token info
        /// </summary>
        private async Task<int> GetTimeStepAsync()
        {
            var timeStepStr = (await GetTokenInfoAsync("timeStep"))?.ToString();
            return int.TryParse(timeStepStr, out var step) ? step : 30;
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
        /// Gets the provisioning URI for QR code generation (otpauth://)
        /// </summary>
        /// <param name="user">Username</param>
        /// <param name="issuer">Issuer name</param>
        /// <returns>OTP Auth URI</returns>
        public new async Task<string> GetOtpAuthUriAsync(string user, string? issuer = null)
        {
            var secret = await GetOtpKeyAsync();
            var otpLen = await GetOtpLengthAsync();
            var hashLib = (await GetTokenInfoAsync("hashlib"))?.ToString() ?? "sha1";
            var timeStep = await GetTimeStepAsync();

            var uri = $"otpauth://totp/{Uri.EscapeDataString(user)}?secret={secret}&digits={otpLen}&algorithm={hashLib.ToUpper()}&period={timeStep}";
            
            if (!string.IsNullOrEmpty(issuer))
            {
                uri += $"&issuer={Uri.EscapeDataString(issuer)}";
            }

            return uri;
        }
    }
}
