using FluentAssertions;
using PrivacyIDEA.Core.Services;
using Xunit;

namespace PrivacyIDEA.Core.Tests;

/// <summary>
/// Unit tests for CryptoService
/// </summary>
public class CryptoServiceTests
{
    private readonly CryptoService _cryptoService;

    public CryptoServiceTests()
    {
        _cryptoService = new CryptoService();
    }

    [Fact]
    public void GenerateRandomBytes_ShouldReturnCorrectLength()
    {
        // Arrange
        const int length = 32;

        // Act
        var result = _cryptoService.GenerateRandomBytes(length);

        // Assert
        result.Should().HaveCount(length);
    }

    [Fact]
    public void GenerateRandomBytes_ShouldGenerateUniqueValues()
    {
        // Act
        var result1 = _cryptoService.GenerateRandomBytes(16);
        var result2 = _cryptoService.GenerateRandomBytes(16);

        // Assert
        result1.Should().NotEqual(result2);
    }

    [Fact]
    public void GenerateRandomHex_ShouldReturnHexString()
    {
        // Act
        var result = _cryptoService.GenerateRandomHex(16);

        // Assert
        result.Should().HaveLength(32); // 16 bytes = 32 hex chars
        result.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void AesEncrypt_Decrypt_ShouldRoundTrip()
    {
        // Arrange
        var key = _cryptoService.GenerateRandomBytes(32);
        var plaintext = System.Text.Encoding.UTF8.GetBytes("Hello, World!");

        // Act
        var (ciphertext, iv) = _cryptoService.AesEncrypt(plaintext, key);
        var decrypted = _cryptoService.AesDecrypt(ciphertext, iv, key);

        // Assert
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void AesEncrypt_ShouldProduceDifferentCiphertexts()
    {
        // Arrange
        var key = _cryptoService.GenerateRandomBytes(32);
        var plaintext = System.Text.Encoding.UTF8.GetBytes("Test data");

        // Act
        var (ciphertext1, _) = _cryptoService.AesEncrypt(plaintext, key);
        var (ciphertext2, _) = _cryptoService.AesEncrypt(plaintext, key);

        // Assert (different IVs = different ciphertexts)
        ciphertext1.Should().NotEqual(ciphertext2);
    }

    [Fact]
    public void HashPassword_ShouldProduceArgon2Hash()
    {
        // Arrange
        const string password = "SecurePassword123!";

        // Act
        var hash = _cryptoService.HashPassword(password);

        // Assert
        hash.Should().StartWith("$argon2id$");
        hash.Should().Contain("$m=");
        hash.Should().Contain(",t=");
        hash.Should().Contain(",p=");
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
    {
        // Arrange
        const string password = "CorrectPassword";
        var hash = _cryptoService.HashPassword(password);

        // Act
        var result = _cryptoService.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalseForIncorrectPassword()
    {
        // Arrange
        const string password = "CorrectPassword";
        const string wrongPassword = "WrongPassword";
        var hash = _cryptoService.HashPassword(password);

        // Act
        var result = _cryptoService.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HmacSha1_ShouldProduceCorrectLength()
    {
        // Arrange
        var key = _cryptoService.GenerateRandomBytes(20);
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        var result = _cryptoService.HmacSha1(key, data);

        // Assert
        result.Should().HaveCount(20); // SHA1 produces 20 bytes
    }

    [Fact]
    public void HmacSha256_ShouldProduceCorrectLength()
    {
        // Arrange
        var key = _cryptoService.GenerateRandomBytes(32);
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        var result = _cryptoService.HmacSha256(key, data);

        // Assert
        result.Should().HaveCount(32); // SHA256 produces 32 bytes
    }

    [Fact]
    public void HmacSha512_ShouldProduceCorrectLength()
    {
        // Arrange
        var key = _cryptoService.GenerateRandomBytes(64);
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        var result = _cryptoService.HmacSha512(key, data);

        // Assert
        result.Should().HaveCount(64); // SHA512 produces 64 bytes
    }

    [Fact]
    public void Sha256_ShouldProduceConsistentHash()
    {
        // Arrange
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        var hash1 = _cryptoService.Sha256(data);
        var hash2 = _cryptoService.Sha256(data);

        // Assert
        hash1.Should().Equal(hash2);
        hash1.Should().HaveCount(32);
    }

    [Fact]
    public void SecureCompare_ShouldReturnTrueForEqualStrings()
    {
        // Arrange
        const string a = "test123";
        const string b = "test123";

        // Act
        var result = _cryptoService.SecureCompare(a, b);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SecureCompare_ShouldReturnFalseForDifferentStrings()
    {
        // Arrange
        const string a = "test123";
        const string b = "test456";

        // Act
        var result = _cryptoService.SecureCompare(a, b);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GenerateOtpSecret_ShouldReturnBase32EncodedString()
    {
        // Act
        var secret = _cryptoService.GenerateOtpSecret(20);

        // Assert
        secret.Should().NotBeNullOrEmpty();
        secret.Should().MatchRegex("^[A-Z2-7]+$"); // Base32 alphabet
    }

    [Fact]
    public void GenerateRsaKeyPair_ShouldProduceValidKeys()
    {
        // Act
        var (publicKey, privateKey) = _cryptoService.GenerateRsaKeyPair(2048);

        // Assert
        publicKey.Should().NotBeEmpty();
        privateKey.Should().NotBeEmpty();
        privateKey.Length.Should().BeGreaterThan(publicKey.Length);
    }

    [Fact]
    public void RsaSign_Verify_ShouldRoundTrip()
    {
        // Arrange
        var (publicKey, privateKey) = _cryptoService.GenerateRsaKeyPair(2048);
        var data = System.Text.Encoding.UTF8.GetBytes("Data to sign");

        // Act
        var signature = _cryptoService.RsaSign(data, privateKey);
        var isValid = _cryptoService.RsaVerify(data, signature, publicKey);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void RsaVerify_ShouldReturnFalseForTamperedData()
    {
        // Arrange
        var (publicKey, privateKey) = _cryptoService.GenerateRsaKeyPair(2048);
        var data = System.Text.Encoding.UTF8.GetBytes("Original data");
        var tamperedData = System.Text.Encoding.UTF8.GetBytes("Tampered data");

        // Act
        var signature = _cryptoService.RsaSign(data, privateKey);
        var isValid = _cryptoService.RsaVerify(tamperedData, signature, publicKey);

        // Assert
        isValid.Should().BeFalse();
    }
}
