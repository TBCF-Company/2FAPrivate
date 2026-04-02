using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.Tokens;

/// <summary>
/// SMS Token implementation
/// Maps to Python: privacyidea/lib/tokens/smstoken.py
/// </summary>
public class SmsToken : TokenClassBase
{
    public override string Type => "sms";
    public override string DisplayName => "SMS";
    public override bool SupportsChallengeResponse => true;

    public SmsToken(ICryptoService cryptoService) : base(cryptoService)
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

        // Check OTP (challenge response)
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
            // No OTP provided, trigger challenge
            result.Success = false;
            result.Message = "Please provide the OTP sent via SMS";
        }

        return result;
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        // SMS OTP verification is done against the challenge
        // This would be handled by the ChallengeService
        if (TokenEntity == null)
            return Task.FromResult(false);

        try
        {
            var secretKey = GetSecretKey();
            var otpLength = TokenEntity.OtpLen;
            var currentCounter = counter ?? TokenEntity.Count;
            var lookAheadWindow = window ?? TokenEntity.CountWindow;

            // Similar to HOTP verification
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
            // Generate OTP for SMS
            var secretKey = GetSecretKey();
            var otpLength = TokenEntity.OtpLen;
            var counter = TokenEntity.Count;
            var otp = GenerateOtp(secretKey, counter, otpLength);

            // The OTP will be sent via SMS by the SMS provider
            return Task.FromResult(new ChallengeResult
            {
                Success = true,
                TransactionId = transactionId ?? Guid.NewGuid().ToString(),
                Message = "Enter the OTP sent to your phone",
                Challenge = otp, // This will be sent via SMS
                Attributes = new Dictionary<string, object>
                {
                    ["phone"] = GetTokenInfoValue("phone") ?? ""
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
        // HOTP-based OTP generation
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
