// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/crypto.py
// Core cryptographic functions for PrivacyIDEA

using System;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace PrivacyIdeaServer.Lib.Crypto
{
    /// <summary>
    /// Secure object for storing sensitive data like keys and PINs
    /// Equivalent to Python's SecretObj dataclass
    /// </summary>
    public class SecretObj
    {
        public byte[] Value { get; }
        public byte[] Iv { get; }

        public SecretObj(byte[] value, byte[] iv)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Iv = iv ?? throw new ArgumentNullException(nameof(iv));
        }

        /// <summary>
        /// Convert the secret to a string (decrypted)
        /// </summary>
        public string GetValue(string key)
        {
            // TODO: Implement decryption using HSM or key
            // This is a placeholder implementation
            return Encoding.UTF8.GetString(Value);
        }
    }

    /// <summary>
    /// Core cryptographic functions
    /// Equivalent to Python's crypto module functions
    /// </summary>
    public static class CryptoFunctions
    {
        /// <summary>
        /// Safe constant-time comparison of two byte arrays
        /// Equivalent to Python's safe_compare using hmac.compare_digest
        /// </summary>
        public static bool SafeCompare(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        /// <summary>
        /// Safe constant-time comparison of two strings
        /// </summary>
        public static bool SafeCompare(string a, string b)
        {
            return SafeCompare(
                Encoding.UTF8.GetBytes(a ?? string.Empty),
                Encoding.UTF8.GetBytes(b ?? string.Empty)
            );
        }

        /// <summary>
        /// Generate cryptographically secure random bytes
        /// Equivalent to Python's geturandom function
        /// </summary>
        public static byte[] GetUrandom(int length)
        {
            return RandomNumberGenerator.GetBytes(length);
        }

        /// <summary>
        /// Hash a value with a seed
        /// Equivalent to Python's hash function using SHA256
        /// </summary>
        public static string Hash(string value, byte[] seed, string? algo = null)
        {
            using var hasher = SHA256.Create();
            var data = Encoding.UTF8.GetBytes(value);
            var combined = new byte[data.Length + seed.Length];
            Buffer.BlockCopy(data, 0, combined, 0, data.Length);
            Buffer.BlockCopy(seed, 0, combined, data.Length, seed.Length);

            var hash = hasher.ComputeHash(combined);
            return Convert.ToHexString(hash).ToLower();
        }

        /// <summary>
        /// Hash a password using bcrypt (or Argon2)
        /// Equivalent to Python's pass_hash using passlib CryptContext
        /// Uses BCrypt.Net-Next library
        /// </summary>
        public static string PassHash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <summary>
        /// Verify a password against a hash
        /// Equivalent to Python's verify_pass_hash
        /// </summary>
        public static bool VerifyPassHash(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Encrypt data using AES-256-CBC
        /// Equivalent to Python's encrypt function
        /// Returns hexlified encrypted data
        /// </summary>
        public static string Encrypt(string data, byte[] iv, string? key = null)
        {
            // TODO: Integrate with HSM if needed
            // For now, using a default key (should be replaced with proper key management)
            var keyBytes = key != null ? Encoding.UTF8.GetBytes(key) : GetDefaultKey();
            
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var encrypted = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
            
            return Convert.ToHexString(encrypted).ToLower();
        }

        /// <summary>
        /// Decrypt data using AES-256-CBC
        /// Equivalent to Python's decrypt function
        /// </summary>
        public static string Decrypt(string encData, byte[] iv, string? key = null)
        {
            // TODO: Integrate with HSM if needed
            var keyBytes = key != null ? Encoding.UTF8.GetBytes(key) : GetDefaultKey();
            
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var encBytes = Convert.FromHexString(encData);
            var decrypted = decryptor.TransformFinalBlock(encBytes, 0, encBytes.Length);
            
            return Encoding.UTF8.GetString(decrypted);
        }

        /// <summary>
        /// Encrypt a PIN using AES
        /// Equivalent to Python's encryptPin
        /// </summary>
        public static string EncryptPin(string pin)
        {
            var iv = GetUrandom(16);
            var encrypted = Encrypt(pin, iv);
            var ivHex = Convert.ToHexString(iv).ToLower();
            return $"{ivHex}{encrypted}";
        }

        /// <summary>
        /// Decrypt a PIN
        /// Equivalent to Python's decryptPin
        /// </summary>
        public static string DecryptPin(string encryptedPin)
        {
            if (encryptedPin.Length < 32)
                throw new ArgumentException("Invalid encrypted PIN length");

            var ivHex = encryptedPin.Substring(0, 32);
            var dataHex = encryptedPin.Substring(32);
            var iv = Convert.FromHexString(ivHex);

            return Decrypt(dataHex, iv);
        }

        /// <summary>
        /// Get default encryption key (placeholder - should be replaced with proper key management)
        /// </summary>
        private static byte[] GetDefaultKey()
        {
            // SECURITY WARNING: This is a development placeholder only!
            // In production, this should:
            // 1. Retrieve key from secure key storage (Azure Key Vault, AWS KMS, etc.)
            // 2. Integrate with HSM (Hardware Security Module)
            // 3. Use configuration-based key management
            
            throw new InvalidOperationException(
                "Default encryption key is not configured. " +
                "Please configure encryption keys using a secure key management system " +
                "(Azure Key Vault, AWS KMS, HSM, or appsettings with proper protection). " +
                "This is a security requirement and cannot use default keys in production.");
            
            // For development/testing only, you can uncomment this (but NEVER in production):
            // using var sha = SHA256.Create();
            // return sha.ComputeHash(Encoding.UTF8.GetBytes("privacyidea-default-key-DEVELOPMENT-ONLY"));
        }

        /// <summary>
        /// Hexlify bytes and return as unicode string
        /// Equivalent to Python's hexlify_and_unicode
        /// </summary>
        public static string HexlifyAndUnicode(byte[] data)
        {
            return Convert.ToHexString(data).ToLower();
        }

        /// <summary>
        /// Base64 encode and return as unicode string
        /// </summary>
        public static string Base64EncodeAndUnicode(byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// Generate a random password/secret
        /// </summary>
        public static string GeneratePassword(int length = 16, bool includeSpecialChars = true)
        {
            const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";
            
            var chars = letters + digits;
            if (includeSpecialChars)
                chars += special;

            var password = new char[length];
            var randomBytes = GetUrandom(length);
            
            for (int i = 0; i < length; i++)
            {
                password[i] = chars[randomBytes[i] % chars.Length];
            }

            return new string(password);
        }
    }
}
