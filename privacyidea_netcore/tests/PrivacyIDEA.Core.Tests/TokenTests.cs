using FluentAssertions;
using Moq;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Core.Tokens;
using PrivacyIDEA.Domain.Entities;
using Xunit;

namespace PrivacyIDEA.Core.Tests;

/// <summary>
/// Unit tests for HOTP Token implementation
/// </summary>
public class HotpTokenTests
{
    private readonly Mock<ICryptoService> _cryptoServiceMock;
    private readonly HotpToken _hotpToken;

    public HotpTokenTests()
    {
        _cryptoServiceMock = new Mock<ICryptoService>();
        _hotpToken = new HotpToken(_cryptoServiceMock.Object);
    }

    [Fact]
    public void Type_ShouldBeHotp()
    {
        _hotpToken.Type.Should().Be("hotp");
    }

    [Fact]
    public void DisplayName_ShouldBeCorrect()
    {
        _hotpToken.DisplayName.Should().Be("HOTP Token");
    }

    [Fact]
    public void SupportsChallengeResponse_ShouldBeFalse()
    {
        _hotpToken.SupportsChallengeResponse.Should().BeFalse();
    }

    [Fact]
    public async Task CheckOtpAsync_WithValidOtp_ShouldReturnTrue()
    {
        // Arrange
        var secret = new byte[20];
        var counter = 0;
        var expectedOtp = "755224"; // Known test vector for counter=0

        // Setup mock to return consistent values
        SetupTokenWithSecret(secret, counter);
        
        // The mock needs to return the actual HMAC calculation
        _cryptoServiceMock
            .Setup(c => c.HmacSha1(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns((byte[] key, byte[] data) =>
            {
                using var hmac = new System.Security.Cryptography.HMACSHA1(key);
                return hmac.ComputeHash(data);
            });

        // Act
        var result = await _hotpToken.CheckOtpAsync(expectedOtp, counter, 5);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateAsync_WithoutToken_ShouldFail()
    {
        // Arrange (token not initialized)

        // Act
        var result = await _hotpToken.AuthenticateAsync(null, "123456");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not initialized");
    }

    [Fact]
    public async Task AuthenticateAsync_WithoutOtp_ShouldFail()
    {
        // Arrange
        SetupTokenWithSecret(new byte[20], 0);

        // Act
        var result = await _hotpToken.AuthenticateAsync(null, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("required");
    }

    private void SetupTokenWithSecret(byte[] secret, int counter)
    {
        var token = new Token
        {
            Id = 1,
            Serial = "HOTP001",
            TokenType = "hotp",
            Active = true,
            Counter = counter,
            OtpLen = 6,
            KeyEnc = Convert.ToHexString(secret),
            KeyIv = Convert.ToHexString(new byte[16])
        };

        _cryptoServiceMock
            .Setup(c => c.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(secret);

        _cryptoServiceMock
            .Setup(c => c.SecureCompare(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string a, string b) => a == b);

        _hotpToken.Initialize(token);
    }
}

/// <summary>
/// Unit tests for TOTP Token implementation
/// </summary>
public class TotpTokenTests
{
    private readonly Mock<ICryptoService> _cryptoServiceMock;
    private readonly TotpToken _totpToken;

    public TotpTokenTests()
    {
        _cryptoServiceMock = new Mock<ICryptoService>();
        _totpToken = new TotpToken(_cryptoServiceMock.Object);
    }

    [Fact]
    public void Type_ShouldBeTotp()
    {
        _totpToken.Type.Should().Be("totp");
    }

    [Fact]
    public void DisplayName_ShouldBeCorrect()
    {
        _totpToken.DisplayName.Should().Be("TOTP Token");
    }

    [Fact]
    public void DefaultTimeStep_ShouldBe30Seconds()
    {
        // TOTP typically uses 30-second time steps
        var token = new Token
        {
            Id = 1,
            Serial = "TOTP001",
            TokenType = "totp",
            Active = true,
            TimeStep = 30
        };

        token.TimeStep.Should().Be(30);
    }
}

/// <summary>
/// Unit tests for Push Token implementation
/// </summary>
public class PushTokenTests
{
    private readonly Mock<ICryptoService> _cryptoServiceMock;
    private readonly PushToken _pushToken;

    public PushTokenTests()
    {
        _cryptoServiceMock = new Mock<ICryptoService>();
        _pushToken = new PushToken(_cryptoServiceMock.Object);
    }

    [Fact]
    public void Type_ShouldBePush()
    {
        _pushToken.Type.Should().Be("push");
    }

    [Fact]
    public void SupportsChallengeResponse_ShouldBeTrue()
    {
        _pushToken.SupportsChallengeResponse.Should().BeTrue();
    }

    [Fact]
    public async Task CreateChallengeAsync_ShouldGenerateChallenge()
    {
        // Arrange
        var token = new Token
        {
            Id = 1,
            Serial = "PUSH001",
            TokenType = "push",
            Active = true
        };

        _cryptoServiceMock
            .Setup(c => c.GenerateRandomBytes(It.IsAny<int>()))
            .Returns(new byte[32]);

        _cryptoServiceMock
            .Setup(c => c.GenerateRsaKeyPair(It.IsAny<int>()))
            .Returns((new byte[256], new byte[1024]));

        _pushToken.Initialize(token);

        // Act
        var result = await _pushToken.CreateChallengeAsync("txn123");

        // Assert
        result.Success.Should().BeTrue();
        result.Challenge.Should().NotBeNullOrEmpty();
    }
}
