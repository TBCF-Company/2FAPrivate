using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.Tokens;

/// <summary>
/// TOTP Token implementation (RFC 6238)
/// Maps to Python: privacyidea/lib/tokens/totptoken.py
/// </summary>
public class TotpToken : TokenClassBase
{
    private const int DefaultTimeStep = 30;
    private const string HashAlgorithmKey = "hashlib";

    public override string Type => "totp";
    public override string DisplayName => "TOTP";
    public override bool SupportsOffline => true;

    public TotpToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        var result = new AuthenticationResult();

        // Check PIN if provided
        if (!string.IsNullOrEmpty(pin))
        {
            result.PinCorrect = await CheckPinAsync(pin);
            if (!result.PinCorrect)
            {
                await IncrementFailCounterAsync();
                result.Success = false;
                result.Message = "PIN incorrect";
                return result;
            }
        }
        else
        {
            result.PinCorrect = true;
        }

        // Check OTP
        if (!string.IsNullOrEmpty(otp))
        {
            result.OtpCorrect = await CheckOtpAsync(otp);
            if (result.OtpCorrect)
            {
                await ResetFailCounterAsync();
                result.Success = true;
                result.Message = "Authentication successful";
            }
            else
            {
                await IncrementFailCounterAsync();
                result.Success = false;
                result.Message = "OTP incorrect";
            }
        }
        else
        {
            result.Success = result.PinCorrect;
        }

        return result;
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        if (TokenEntity == null)
            return Task.FromResult(false);

        try
        {
            var secretKey = GetSecretKey();
            var otpLength = TokenEntity.OtpLen;
            var timeStep = GetTimeStep();
            var lookAheadWindow = window ?? TokenEntity.CountWindow;
            var hashAlgorithm = GetHashAlgorithm();

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var currentCounter = counter ?? (currentTime / timeStep);

            // Check within the window (before and after current time)
            for (int i = -lookAheadWindow; i <= lookAheadWindow; i++)
            {
                var testCounter = currentCounter + i;
                var expectedOtp = GenerateTotp(secretKey, testCounter, otpLength, hashAlgorithm);
                if (CryptoService.SecureCompare(otp, expectedOtp))
                {
                    // Store the counter to prevent replay
                    if (testCounter > TokenEntity.Count)
                    {
                        TokenEntity.Count = (int)testCounter;
                    }
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public override Task<string?> GetOtpAsync(long? timestamp = null)
    {
        if (TokenEntity == null)
            return Task.FromResult<string?>(null);

        try
        {
            var secretKey = GetSecretKey();
            var otpLength = TokenEntity.OtpLen;
            var timeStep = GetTimeStep();
            var hashAlgorithm = GetHashAlgorithm();

            var time = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var counter = time / timeStep;
            var otp = GenerateTotp(secretKey, counter, otpLength, hashAlgorithm);
            return Task.FromResult<string?>(otp);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public override Task<bool> ResyncAsync(string otp1, string otp2)
    {
        // TOTP doesn't typically need resync as it's time-based
        // But we can try to find the time drift
        return Task.FromResult(false);
    }

    private int GetTimeStep()
    {
        var timeStepStr = GetTokenInfoValue("timeStep");
        if (int.TryParse(timeStepStr, out var timeStep) && timeStep > 0)
            return timeStep;
        return DefaultTimeStep;
    }

    private string GetHashAlgorithm()
    {
        return GetTokenInfoValue(HashAlgorithmKey) ?? "sha1";
    }

    /// <summary>
    /// Generate TOTP value according to RFC 6238
    /// </summary>
    private string GenerateTotp(byte[] key, long counter, int digits, string hashAlgorithm)
    {
        // Convert counter to big-endian bytes
        var counterBytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        // HMAC based on algorithm
        byte[] hash = hashAlgorithm.ToLower() switch
        {
            "sha256" => CryptoService.HmacSha256(key, counterBytes),
            "sha512" => CryptoService.HmacSha512(key, counterBytes),
            _ => CryptoService.HmacSha1(key, counterBytes)
        };

        // Dynamic truncation
        int offset = hash[^1] & 0x0f;
        int binary = ((hash[offset] & 0x7f) << 24) |
                     ((hash[offset + 1] & 0xff) << 16) |
                     ((hash[offset + 2] & 0xff) << 8) |
                     (hash[offset + 3] & 0xff);

        int otp = binary % (int)Math.Pow(10, digits);
        return otp.ToString().PadLeft(digits, '0');
    }
}
