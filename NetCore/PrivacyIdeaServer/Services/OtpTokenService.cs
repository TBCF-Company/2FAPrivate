// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// OTP Token Service for HOTP/TOTP generation and validation

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace PrivacyIdeaServer.Services
{
    /// <summary>
    /// Interface for OTP token operations
    /// </summary>
    public interface IOtpTokenService
    {
        /// <summary>
        /// Generate a new secret key for a token
        /// </summary>
        string GenerateSecret(int length = 20);
        
        /// <summary>
        /// Generate TOTP code for current time
        /// </summary>
        string GenerateTotp(string secret, int digits = 6, int step = 30);
        
        /// <summary>
        /// Validate TOTP code
        /// </summary>
        bool ValidateTotp(string secret, string code, int digits = 6, int step = 30, int window = 1);
        
        /// <summary>
        /// Generate HOTP code for a given counter
        /// </summary>
        string GenerateHotp(string secret, long counter, int digits = 6);
        
        /// <summary>
        /// Validate HOTP code
        /// </summary>
        bool ValidateHotp(string secret, string code, long counter, int digits = 6, int window = 10);
        
        /// <summary>
        /// Generate QR code provisioning URI for Google Authenticator
        /// </summary>
        string GenerateProvisioningUri(string secret, string issuer, string account, bool isTotp = true, int digits = 6, int step = 30);
    }

    /// <summary>
    /// OTP Token Service implementation using OtpNet
    /// </summary>
    public class OtpTokenService : IOtpTokenService
    {
        private readonly ILogger<OtpTokenService> _logger;

        public OtpTokenService(ILogger<OtpTokenService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateSecret(int length = 20)
        {
            var key = KeyGeneration.GenerateRandomKey(length);
            var base32Secret = Base32Encoding.ToString(key);
            _logger.LogDebug("Generated new secret key of length {Length}", length);
            return base32Secret;
        }

        public string GenerateTotp(string secret, int digits = 6, int step = 30)
        {
            try
            {
                var secretBytes = Base32Encoding.ToBytes(secret);
                var totp = new Totp(secretBytes, step: step, totpSize: digits);
                var code = totp.ComputeTotp();
                _logger.LogDebug("Generated TOTP code");
                return code;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate TOTP code");
                throw;
            }
        }

        public bool ValidateTotp(string secret, string code, int digits = 6, int step = 30, int window = 1)
        {
            try
            {
                var secretBytes = Base32Encoding.ToBytes(secret);
                var totp = new Totp(secretBytes, step: step, totpSize: digits);
                
                // Verify with time window (allows for clock drift)
                long timeStepMatched;
                var isValid = totp.VerifyTotp(code, out timeStepMatched, new VerificationWindow(window, window));
                
                if (isValid)
                {
                    _logger.LogInformation("TOTP code validated successfully");
                }
                else
                {
                    _logger.LogWarning("TOTP code validation failed");
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating TOTP code");
                return false;
            }
        }

        public string GenerateHotp(string secret, long counter, int digits = 6)
        {
            try
            {
                var secretBytes = Base32Encoding.ToBytes(secret);
                var hotp = new Hotp(secretBytes, mode: OtpHashMode.Sha1);
                var code = hotp.ComputeHOTP(counter);
                
                // Format to correct number of digits
                var formattedCode = code.ToString().PadLeft(digits, '0');
                if (formattedCode.Length > digits)
                {
                    formattedCode = formattedCode.Substring(formattedCode.Length - digits);
                }
                
                _logger.LogDebug("Generated HOTP code for counter {Counter}", counter);
                return formattedCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate HOTP code");
                throw;
            }
        }

        public bool ValidateHotp(string secret, string code, long counter, int digits = 6, int window = 10)
        {
            try
            {
                var secretBytes = Base32Encoding.ToBytes(secret);
                var hotp = new Hotp(secretBytes, mode: OtpHashMode.Sha1);
                
                // Check within window
                for (long i = counter; i <= counter + window; i++)
                {
                    var expectedCode = hotp.ComputeHOTP(i);
                    var formattedCode = expectedCode.ToString().PadLeft(digits, '0');
                    if (formattedCode.Length > digits)
                    {
                        formattedCode = formattedCode.Substring(formattedCode.Length - digits);
                    }
                    
                    if (formattedCode == code)
                    {
                        _logger.LogInformation("HOTP code validated successfully at counter {Counter}", i);
                        return true;
                    }
                }
                
                _logger.LogWarning("HOTP code validation failed");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating HOTP code");
                return false;
            }
        }

        public string GenerateProvisioningUri(string secret, string issuer, string account, bool isTotp = true, int digits = 6, int step = 30)
        {
            try
            {
                var secretBytes = Base32Encoding.ToBytes(secret);
                
                if (isTotp)
                {
                    var totp = new Totp(secretBytes, step: step, totpSize: digits);
                    return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={digits}&period={step}";
                }
                else
                {
                    return $"otpauth://hotp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={digits}&counter=0";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate provisioning URI");
                throw;
            }
        }
    }
}
