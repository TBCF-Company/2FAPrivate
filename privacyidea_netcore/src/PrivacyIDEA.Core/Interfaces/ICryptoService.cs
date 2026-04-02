namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Interface for cryptographic operations
/// Maps to Python: privacyidea/lib/crypto.py
/// </summary>
public interface ICryptoService
{
    /// <summary>
    /// Encrypt data using AES-256-CBC
    /// </summary>
    (byte[] ciphertext, byte[] iv) AesEncrypt(byte[] plaintext, byte[] key);

    /// <summary>
    /// Decrypt data using AES-256-CBC
    /// </summary>
    byte[] AesDecrypt(byte[] ciphertext, byte[] iv, byte[] key);

    /// <summary>
    /// Encrypt and encode a PIN
    /// </summary>
    (string encryptedPin, string iv) EncryptPin(string pin);

    /// <summary>
    /// Decrypt a PIN
    /// </summary>
    string DecryptPin(string encryptedPin, string iv);

    /// <summary>
    /// Encrypt a token key
    /// </summary>
    (string encryptedKey, string iv) EncryptKey(byte[] key);

    /// <summary>
    /// Decrypt a token key
    /// </summary>
    byte[] DecryptKey(string encryptedKey, string iv);

    /// <summary>
    /// Hash a PIN using Argon2
    /// </summary>
    string HashPin(string pin);

    /// <summary>
    /// Verify a PIN against its hash
    /// </summary>
    bool VerifyPinHash(string pin, string hash);

    /// <summary>
    /// Hash a password using Argon2
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against its hash
    /// </summary>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Hash using Argon2id
    /// </summary>
    string HashArgon2(string input);

    /// <summary>
    /// Verify Argon2 hash
    /// </summary>
    bool VerifyArgon2(string input, string hash);

    /// <summary>
    /// Generate cryptographically secure random bytes
    /// </summary>
    byte[] GenerateRandomBytes(int length);

    /// <summary>
    /// Generate a random string (hex encoded)
    /// </summary>
    string GenerateRandomHex(int length);

    /// <summary>
    /// Generate HMAC-SHA1
    /// </summary>
    byte[] HmacSha1(byte[] key, byte[] data);

    /// <summary>
    /// Generate HMAC-SHA256
    /// </summary>
    byte[] HmacSha256(byte[] key, byte[] data);

    /// <summary>
    /// Generate HMAC-SHA512
    /// </summary>
    byte[] HmacSha512(byte[] key, byte[] data);

    /// <summary>
    /// Compute MD5 hash (for legacy compatibility only)
    /// </summary>
    byte[] Md5(byte[] data);

    /// <summary>
    /// Compute SHA1 hash
    /// </summary>
    byte[] Sha1(byte[] data);

    /// <summary>
    /// Compute SHA256 hash
    /// </summary>
    byte[] Sha256(byte[] data);

    /// <summary>
    /// Compute SHA512 hash
    /// </summary>
    byte[] Sha512(byte[] data);

    /// <summary>
    /// Generate a random OTP secret (Base32 encoded)
    /// </summary>
    string GenerateOtpSecret(int length = 20);

    /// <summary>
    /// Initialize the encryption key from file or generate new one
    /// </summary>
    Task InitializeEncryptionKeyAsync(string keyFile);

    /// <summary>
    /// Check if encryption key is initialized
    /// </summary>
    bool IsKeyInitialized { get; }

    /// <summary>
    /// Generate a new encryption key file
    /// </summary>
    Task CreateEncryptionKeyAsync(string keyFile);

    /// <summary>
    /// Sign data using RSA
    /// </summary>
    byte[] RsaSign(byte[] data, byte[] privateKey);

    /// <summary>
    /// Verify RSA signature
    /// </summary>
    bool RsaVerify(byte[] data, byte[] signature, byte[] publicKey);

    /// <summary>
    /// Generate RSA key pair
    /// </summary>
    (byte[] publicKey, byte[] privateKey) GenerateRsaKeyPair(int keySize = 2048);

    /// <summary>
    /// Constant-time string comparison
    /// </summary>
    bool SecureCompare(string a, string b);
}
