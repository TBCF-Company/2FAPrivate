using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.Tokens;

/// <summary>
/// HOTP Token implementation (RFC 4226)
/// Maps to Python: privacyidea/lib/tokens/hotptoken.py
/// </summary>
public class HotpToken : TokenClassBase
{
    public override string Type => "hotp";
    public override string DisplayName => "HOTP";
    public override bool SupportsOffline => true;

    public HotpToken(ICryptoService cryptoService) : base(cryptoService)
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
            var currentCounter = counter ?? TokenEntity.Count;
            var lookAheadWindow = window ?? TokenEntity.CountWindow;

            // Try to match OTP within the window
            for (int i = 0; i <= lookAheadWindow; i++)
            {
                var expectedOtp = GenerateHotp(secretKey, currentCounter + i, otpLength);
                if (CryptoService.SecureCompare(otp, expectedOtp))
                {
                    // Update counter to next value
                    TokenEntity.Count = currentCounter + i + 1;
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
            var counter = TokenEntity.Count;
            var otpLength = TokenEntity.OtpLen;
            var otp = GenerateHotp(secretKey, counter, otpLength);
            return Task.FromResult<string?>(otp);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public override Task<bool> ResyncAsync(string otp1, string otp2)
    {
        if (TokenEntity == null)
            return Task.FromResult(false);

        try
        {
            var secretKey = GetSecretKey();
            var otpLength = TokenEntity.OtpLen;
            var syncWindow = TokenEntity.SyncWindow;

            // Search for two consecutive matching OTPs
            for (int i = 0; i <= syncWindow; i++)
            {
                var expectedOtp1 = GenerateHotp(secretKey, i, otpLength);
                var expectedOtp2 = GenerateHotp(secretKey, i + 1, otpLength);

                if (CryptoService.SecureCompare(otp1, expectedOtp1) &&
                    CryptoService.SecureCompare(otp2, expectedOtp2))
                {
                    TokenEntity.Count = i + 2;
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

    /// <summary>
    /// Generate HOTP value according to RFC 4226
    /// </summary>
    private string GenerateHotp(byte[] key, long counter, int digits)
    {
        // Convert counter to big-endian bytes
        var counterBytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        // HMAC-SHA1
        var hash = CryptoService.HmacSha1(key, counterBytes);

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
