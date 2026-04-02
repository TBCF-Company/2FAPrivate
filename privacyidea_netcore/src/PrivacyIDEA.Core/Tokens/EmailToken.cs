using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.Tokens;

/// <summary>
/// Email Token implementation
/// Maps to Python: privacyidea/lib/tokens/emailtoken.py
/// </summary>
public class EmailToken : TokenClassBase
{
    public override string Type => "email";
    public override string DisplayName => "Email";
    public override bool SupportsChallengeResponse => true;

    public EmailToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        var result = new AuthenticationResult();

        // Check PIN
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
            result.Success = false;
            result.Message = "Please provide the OTP sent to your email";
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

            for (int i = 0; i <= lookAheadWindow; i++)
            {
                var expectedOtp = GenerateOtp(secretKey, currentCounter + i, otpLength);
                if (CryptoService.SecureCompare(otp, expectedOtp))
                {
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

    public override Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        if (TokenEntity == null)
        {
            return Task.FromResult(new ChallengeResult
            {
                Success = false,
                Message = "Token not initialized"
            });
        }

        try
        {
            var secretKey = GetSecretKey();
            var otpLength = TokenEntity.OtpLen;
            var counter = TokenEntity.Count;
            var otp = GenerateOtp(secretKey, counter, otpLength);

            return Task.FromResult(new ChallengeResult
            {
                Success = true,
                TransactionId = transactionId ?? Guid.NewGuid().ToString(),
                Message = "Enter the OTP sent to your email",
                Challenge = otp,
                Attributes = new Dictionary<string, object>
                {
                    ["email"] = GetTokenInfoValue("email") ?? ""
                }
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ChallengeResult
            {
                Success = false,
                Message = $"Failed to create challenge: {ex.Message}"
            });
        }
    }

    private string GenerateOtp(byte[] key, long counter, int digits)
    {
        var counterBytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        var hash = CryptoService.HmacSha1(key, counterBytes);
        int offset = hash[^1] & 0x0f;
        int binary = ((hash[offset] & 0x7f) << 24) |
                     ((hash[offset + 1] & 0xff) << 16) |
                     ((hash[offset + 2] & 0xff) << 8) |
                     (hash[offset + 3] & 0xff);

        int otp = binary % (int)Math.Pow(10, digits);
        return otp.ToString().PadLeft(digits, '0');
    }
}
