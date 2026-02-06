// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/apps.py
//
// 2017-07-13 Cornelius Kölbel <cornelius.koelbel@netknights.it>
//            Add period to key uri for TOTP token
// 2016-05-21 Cornelius Kölbel <cornelius.koelbel@netknights.it>
//            urlencode token issuer.
// 2015-07-01 Cornelius Kölbel <cornelius.koelbel@netknights.it>
//            Add SHA Algorithms to QR Code
// 2014-12-01 Cornelius Kölbel <cornelius@privacyidea.org>
//            Migrate to flask
//
// Copyright (C) 2010 - 2014 LSE Leading Security Experts GmbH
//
// This code is free software; you can redistribute it and/or
// modify it under the terms of the GNU AFFERO GENERAL PUBLIC LICENSE
// License as published by the Free Software Foundation; either
// version 3 of the License, or any later version.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;

namespace PrivacyIdeaServer.Lib.Applications
{
    /// <summary>
    /// Generates URLs for smartphone authentication apps
    /// Supports Google Authenticator, OATH Token, MOTP, and other OTP applications
    /// </summary>
    public class Apps
    {
        private readonly ILogger<Apps> _logger;
        private const int MaxQrCodeLength = 180;

        public Apps(ILogger<Apps> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// User information for token label generation
        /// </summary>
        public class UserInfo
        {
            public string? Givenname { get; set; }
            public string? Surname { get; set; }
            public Dictionary<string, string> Info { get; set; } = new();
        }

        /// <summary>
        /// Constructs extra parameters for OTP URLs
        /// </summary>
        /// <param name="extraData">Dictionary of extra key-value pairs</param>
        /// <returns>URL-encoded parameter string</returns>
        private static string ConstructExtraParameters(Dictionary<string, string>? extraData)
        {
            if (extraData == null || extraData.Count == 0)
                return string.Empty;

            var parameters = extraData
                .Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}")
                .ToList();

            return parameters.Count > 0 ? "&" + string.Join("&", parameters) : string.Empty;
        }

        /// <summary>
        /// Creates MOTP URL for mobile OTP applications
        /// Format: motp://SecureSite:alice@wonder.land?secret=JBSWY3DPEHPK3PXP
        /// </summary>
        /// <param name="key">Hexadecimal OTP key</param>
        /// <param name="user">Username</param>
        /// <param name="realm">User realm</param>
        /// <param name="serial">Token serial number</param>
        /// <returns>MOTP URL string</returns>
        public async Task<string> CreateMotpUrlAsync(
            string key,
            string? user = null,
            string? realm = null,
            string serial = "")
        {
            return await Task.Run(() => CreateMotpUrl(key, user, realm, serial));
        }

        /// <summary>
        /// Creates MOTP URL synchronously
        /// </summary>
        public string CreateMotpUrl(
            string key,
            string? user = null,
            string? realm = null,
            string serial = "")
        {
            _logger.LogDebug("Creating MOTP URL for serial {Serial}", serial);

            // For MOTP/Token2, the key is hexencoded, not base32
            var otpKey = key;

            // TODO: Implement policy-based token label generation
            var label = "mylabel";
            const int allowedLabelLen = 20;
            label = label.Length > allowedLabelLen 
                ? label.Substring(0, allowedLabelLen) 
                : label;

            var urlLabel = HttpUtility.UrlEncode(label);

            var url = $"motp://privacyidea:{urlLabel}?secret={otpKey}";
            _logger.LogDebug("Created MOTP URL for {Serial}", serial);

            return url;
        }

        /// <summary>
        /// Creates Google Authenticator URL for HOTP/TOTP tokens
        /// Maximum URL length is constrained by QR code limitations
        /// </summary>
        /// <param name="key">Hexadecimal OTP key</param>
        /// <param name="user">Username</param>
        /// <param name="realm">User realm</param>
        /// <param name="tokenType">Token type (hotp, totp, daypassword)</param>
        /// <param name="period">Time period for TOTP in seconds</param>
        /// <param name="serial">Token serial number</param>
        /// <param name="tokenLabel">Token label template</param>
        /// <param name="hashAlgo">Hash algorithm (SHA1, SHA256, SHA512)</param>
        /// <param name="digits">Number of OTP digits</param>
        /// <param name="issuer">Issuer name</param>
        /// <param name="userObj">User information object</param>
        /// <param name="creator">Creator identifier</param>
        /// <param name="extraData">Additional URL parameters</param>
        /// <returns>Google Authenticator compatible URL</returns>
        public async Task<string> CreateGoogleAuthenticatorUrlAsync(
            string key,
            string? user = null,
            string? realm = null,
            string tokenType = "hotp",
            string period = "30",
            string serial = "mylabel",
            string tokenLabel = "<s>",
            string hashAlgo = "SHA1",
            string digits = "6",
            string issuer = "privacyIDEA",
            UserInfo? userObj = null,
            string creator = "privacyidea",
            Dictionary<string, string>? extraData = null)
        {
            return await Task.Run(() => CreateGoogleAuthenticatorUrl(
                key, user, realm, tokenType, period, serial, tokenLabel,
                hashAlgo, digits, issuer, userObj, creator, extraData));
        }

        /// <summary>
        /// Creates Google Authenticator URL synchronously
        /// </summary>
        public string CreateGoogleAuthenticatorUrl(
            string key,
            string? user = null,
            string? realm = null,
            string tokenType = "hotp",
            string period = "30",
            string serial = "mylabel",
            string tokenLabel = "<s>",
            string hashAlgo = "SHA1",
            string digits = "6",
            string issuer = "privacyIDEA",
            UserInfo? userObj = null,
            string creator = "privacyidea",
            Dictionary<string, string>? extraData = null)
        {
            _logger.LogDebug("Creating Google Authenticator URL for {Serial}, type {TokenType}", 
                serial, tokenType);

            extraData ??= new Dictionary<string, string>();
            userObj ??= new UserInfo();
            tokenType = tokenType.ToLowerInvariant();
            realm ??= string.Empty;
            user ??= string.Empty;

            // Determine counter parameter for HOTP
            var counter = tokenType == "hotp" ? "counter=1&" : string.Empty;

            // Convert hex key to base32
            var keyBytes = ConvertHexToBytes(key);
            var otpKey = ConvertToBase32(keyBytes).TrimEnd('=');

            // Calculate allowed label length
            var baseLen = $"otpauth://{tokenType}/?secret={otpKey}&counter=1".Length;
            var allowedLabelLen = MaxQrCodeLength - baseLen;
            
            _logger.LogDebug("Available characters for token label: {Length}", allowedLabelLen);

            // Build label from template
            var label = BuildTokenLabel(tokenLabel, serial, user, realm, userObj);
            label = label.Length > allowedLabelLen 
                ? label.Substring(0, allowedLabelLen) 
                : label;

            // Build issuer from template
            issuer = BuildTokenLabel(issuer, serial, user, realm, userObj);

            // URL encode components
            var urlLabel = HttpUtility.UrlEncode(label);
            var urlIssuer = HttpUtility.UrlEncode(issuer);
            var urlSerial = HttpUtility.UrlEncode(serial);

            // Hash algorithm parameter (omit SHA1 as it's default)
            var hashParam = hashAlgo.ToUpperInvariant() != "SHA1"
                ? $"algorithm={hashAlgo.ToUpperInvariant()}&"
                : string.Empty;

            // Period parameter for TOTP
            var periodParam = string.Empty;
            if (tokenType == "totp")
            {
                periodParam = $"period={period}&";
            }
            else if (tokenType == "daypassword")
            {
                var periodSeconds = ParseTimeDelta(period);
                periodParam = $"period={periodSeconds}&";
            }

            // Build final URL
            var url = $"otpauth://{tokenType}/{urlLabel}?secret={otpKey}&" +
                     $"{counter}{hashParam}{periodParam}" +
                     $"digits={digits}&" +
                     $"creator={creator}&" +
                     $"issuer={urlIssuer}&" +
                     $"serial={urlSerial}{ConstructExtraParameters(extraData)}";

            _logger.LogDebug("Created Google Authenticator URL for {Serial}", serial);
            return url;
        }

        /// <summary>
        /// Creates OATH Token URL for compatible applications
        /// </summary>
        /// <param name="otpKey">OTP key (base32 encoded)</param>
        /// <param name="user">Username</param>
        /// <param name="realm">User realm</param>
        /// <param name="type">Token type (hotp or totp)</param>
        /// <param name="serial">Token serial number</param>
        /// <param name="tokenLabel">Token label template</param>
        /// <param name="extraData">Additional URL parameters</param>
        /// <returns>OATH Token compatible URL</returns>
        public async Task<string> CreateOathTokenUrlAsync(
            string otpKey,
            string? user = null,
            string? realm = null,
            string type = "hotp",
            string serial = "mylabel",
            string tokenLabel = "<s>",
            Dictionary<string, string>? extraData = null)
        {
            return await Task.Run(() => CreateOathTokenUrl(
                otpKey, user, realm, type, serial, tokenLabel, extraData));
        }

        /// <summary>
        /// Creates OATH Token URL synchronously
        /// </summary>
        public string CreateOathTokenUrl(
            string otpKey,
            string? user = null,
            string? realm = null,
            string type = "hotp",
            string serial = "mylabel",
            string tokenLabel = "<s>",
            Dictionary<string, string>? extraData = null)
        {
            _logger.LogDebug("Creating OATH Token URL for {Serial}, type {Type}", serial, type);

            extraData ??= new Dictionary<string, string>();
            realm ??= string.Empty;
            user ??= string.Empty;

            var timeBased = type.Equals("totp", StringComparison.OrdinalIgnoreCase)
                ? "&timeBased=true"
                : string.Empty;

            // Build label
            var label = tokenLabel
                .Replace("<s>", serial)
                .Replace("<u>", user)
                .Replace("<r>", realm);

            var urlLabel = HttpUtility.UrlEncode(label);
            var extraParameters = ConstructExtraParameters(extraData);

            var url = $"oathtoken:///addToken?name={urlLabel}&lockdown=true&key={otpKey}{timeBased}{extraParameters}";

            _logger.LogDebug("Created OATH Token URL for {Serial}", serial);
            return url;
        }

        /// <summary>
        /// Builds token label from template
        /// </summary>
        private static string BuildTokenLabel(
            string template,
            string serial,
            string user,
            string realm,
            UserInfo userObj)
        {
            var givenname = userObj.Info.GetValueOrDefault("givenname", string.Empty);
            var surname = userObj.Info.GetValueOrDefault("surname", string.Empty);

            // Support both old-style and new-style formatting
            var label = template
                .Replace("<s>", serial)
                .Replace("<u>", user)
                .Replace("<r>", realm);

            // New style with string interpolation
            try
            {
                // Simple string replacement for placeholders
                label = label
                    .Replace("{serial}", serial)
                    .Replace("{user}", user)
                    .Replace("{realm}", realm)
                    .Replace("{givenname}", givenname)
                    .Replace("{surname}", surname);
            }
            catch (Exception)
            {
                // If formatting fails, use original
            }

            return label;
        }

        /// <summary>
        /// Converts hexadecimal string to byte array
        /// </summary>
        private static byte[] ConvertHexToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length", nameof(hex));

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// Converts byte array to base32 string
        /// </summary>
        private static string ConvertToBase32(byte[] data)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();
            
            int buffer = data[0];
            int bitsLeft = 8;
            int index = 1;

            while (bitsLeft > 0 || index < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (index < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= data[index++];
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int value = (buffer >> (bitsLeft - 5)) & 0x1F;
                bitsLeft -= 5;
                result.Append(base32Chars[value]);
            }

            // Add padding
            while (result.Length % 8 != 0)
            {
                result.Append('=');
            }

            return result.ToString();
        }

        /// <summary>
        /// Parses time delta string to seconds
        /// Supports formats like "30s", "5m", "1h", "1d"
        /// </summary>
        private static int ParseTimeDelta(string timeDelta)
        {
            if (string.IsNullOrWhiteSpace(timeDelta))
                return 30; // Default 30 seconds

            if (int.TryParse(timeDelta, out int seconds))
                return seconds;

            var value = timeDelta.TrimEnd('s', 'm', 'h', 'd', 'S', 'M', 'H', 'D');
            var unit = timeDelta[^1..].ToLowerInvariant();

            if (!int.TryParse(value, out int num))
                return 30;

            return unit switch
            {
                "s" => num,
                "m" => num * 60,
                "h" => num * 3600,
                "d" => num * 86400,
                _ => num
            };
        }
    }
}
