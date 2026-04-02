using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// Implementation of cryptographic operations
/// Maps to Python: privacyidea/lib/crypto.py
/// </summary>
public class CryptoService : ICryptoService
{
    private byte[]? _encryptionKey;
    private const int ArgonRounds = 9;
    private const int ArgonMemorySize = 65536; // 64 MB
    private const int ArgonParallelism = 4;
    private const int KeySize = 32; // 256 bits
    private const int IvSize = 16; // 128 bits

    public bool IsKeyInitialized => _encryptionKey != null;

    public (byte[] ciphertext, byte[] iv) AesEncrypt(byte[] plaintext, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        return (ciphertext, aes.IV);
    }

    public byte[] AesDecrypt(byte[] ciphertext, byte[] iv, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    public (string encryptedPin, string iv) EncryptPin(string pin)
    {
        EnsureKeyInitialized();
        var pinBytes = Encoding.UTF8.GetBytes(pin);
        var (ciphertext, iv) = AesEncrypt(pinBytes, _encryptionKey!);
        return (Convert.ToHexString(ciphertext), Convert.ToHexString(iv));
    }

    public string DecryptPin(string encryptedPin, string iv)
    {
        EnsureKeyInitialized();
        var ciphertext = Convert.FromHexString(encryptedPin);
        var ivBytes = Convert.FromHexString(iv);
        var plaintext = AesDecrypt(ciphertext, ivBytes, _encryptionKey!);
        return Encoding.UTF8.GetString(plaintext);
    }

    public (string encryptedKey, string iv) EncryptKey(byte[] key)
    {
        EnsureKeyInitialized();
        var (ciphertext, iv) = AesEncrypt(key, _encryptionKey!);
        return (Convert.ToHexString(ciphertext), Convert.ToHexString(iv));
    }

    public byte[] DecryptKey(string encryptedKey, string iv)
    {
        EnsureKeyInitialized();
        var ciphertext = Convert.FromHexString(encryptedKey);
        var ivBytes = Convert.FromHexString(iv);
        return AesDecrypt(ciphertext, ivBytes, _encryptionKey!);
    }

    public string HashPin(string pin)
    {
        return HashWithArgon2(pin);
    }

    public bool VerifyPinHash(string pin, string hash)
    {
        return VerifyArgon2Hash(pin, hash);
    }

    public string HashPassword(string password)
    {
        return HashWithArgon2(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return VerifyArgon2Hash(password, hash);
    }

    public string HashArgon2(string input)
    {
        return HashWithArgon2(input);
    }

    public bool VerifyArgon2(string input, string hash)
    {
        return VerifyArgon2Hash(input, hash);
    }

    private string HashWithArgon2(string input)
    {
        var salt = GenerateRandomBytes(16);
        var inputBytes = Encoding.UTF8.GetBytes(input);

        using var argon2 = new Argon2id(inputBytes);
        argon2.Salt = salt;
        argon2.DegreeOfParallelism = ArgonParallelism;
        argon2.MemorySize = ArgonMemorySize;
        argon2.Iterations = ArgonRounds;

        var hash = argon2.GetBytes(32);

        // Format: $argon2id$v=19$m=65536,t=9,p=4$<salt>$<hash>
        var saltBase64 = Convert.ToBase64String(salt).TrimEnd('=');
        var hashBase64 = Convert.ToBase64String(hash).TrimEnd('=');
        return $"$argon2id$v=19$m={ArgonMemorySize},t={ArgonRounds},p={ArgonParallelism}${saltBase64}${hashBase64}";
    }

    private bool VerifyArgon2Hash(string input, string hashString)
    {
        try
        {
            // Parse the hash string
            var parts = hashString.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id")
                return false;

            var paramParts = parts[3].Split(',');
            var memorySize = int.Parse(paramParts[0].Substring(2));
            var iterations = int.Parse(paramParts[1].Substring(2));
            var parallelism = int.Parse(paramParts[2].Substring(2));

            var salt = Convert.FromBase64String(PadBase64(parts[4]));
            var expectedHash = Convert.FromBase64String(PadBase64(parts[5]));

            var inputBytes = Encoding.UTF8.GetBytes(input);

            using var argon2 = new Argon2id(inputBytes);
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = parallelism;
            argon2.MemorySize = memorySize;
            argon2.Iterations = iterations;

            var computedHash = argon2.GetBytes(expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    private static string PadBase64(string base64)
    {
        var padding = 4 - (base64.Length % 4);
        if (padding < 4)
            base64 += new string('=', padding);
        return base64;
    }

    public byte[] GenerateRandomBytes(int length)
    {
        return RandomNumberGenerator.GetBytes(length);
    }

    public string GenerateRandomHex(int length)
    {
        var bytes = GenerateRandomBytes(length);
        return Convert.ToHexString(bytes).ToLower();
    }

    public byte[] HmacSha1(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA1(key);
        return hmac.ComputeHash(data);
    }

    public byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    public byte[] HmacSha512(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA512(key);
        return hmac.ComputeHash(data);
    }

    public byte[] Md5(byte[] data)
    {
        return MD5.HashData(data);
    }

    public byte[] Sha1(byte[] data)
    {
        return SHA1.HashData(data);
    }

    public byte[] Sha256(byte[] data)
    {
        return SHA256.HashData(data);
    }

    public byte[] Sha512(byte[] data)
    {
        return SHA512.HashData(data);
    }

    public string GenerateOtpSecret(int length = 20)
    {
        var bytes = GenerateRandomBytes(length);
        return Base32Encode(bytes);
    }

    public async Task InitializeEncryptionKeyAsync(string keyFile)
    {
        if (File.Exists(keyFile))
        {
            var keyHex = await File.ReadAllTextAsync(keyFile);
            _encryptionKey = Convert.FromHexString(keyHex.Trim());
        }
        else
        {
            throw new FileNotFoundException($"Encryption key file not found: {keyFile}");
        }
    }

    public async Task CreateEncryptionKeyAsync(string keyFile)
    {
        var key = GenerateRandomBytes(KeySize);
        var keyHex = Convert.ToHexString(key);
        await File.WriteAllTextAsync(keyFile, keyHex);
        _encryptionKey = key;
    }

    public byte[] RsaSign(byte[] data, byte[] privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKey, out _);
        return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public bool RsaVerify(byte[] data, byte[] signature, byte[] publicKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKey, out _);
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public (byte[] publicKey, byte[] privateKey) GenerateRsaKeyPair(int keySize = 2048)
    {
        using var rsa = RSA.Create(keySize);
        var publicKey = rsa.ExportRSAPublicKey();
        var privateKey = rsa.ExportRSAPrivateKey();
        return (publicKey, privateKey);
    }

    public bool SecureCompare(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private void EnsureKeyInitialized()
    {
        if (_encryptionKey == null)
            throw new InvalidOperationException("Encryption key not initialized. Call InitializeEncryptionKeyAsync first.");
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder();
        int buffer = 0;
        int bitsInBuffer = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsInBuffer += 8;

            while (bitsInBuffer >= 5)
            {
                bitsInBuffer -= 5;
                result.Append(alphabet[(buffer >> bitsInBuffer) & 0x1F]);
            }
        }

        if (bitsInBuffer > 0)
        {
            result.Append(alphabet[(buffer << (5 - bitsInBuffer)) & 0x1F]);
        }

        return result.ToString();
    }
}
