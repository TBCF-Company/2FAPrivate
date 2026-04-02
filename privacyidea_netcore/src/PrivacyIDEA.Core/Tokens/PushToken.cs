using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.Tokens;

/// <summary>
/// Push Token implementation for push notification authentication
/// Maps to Python: privacyidea/lib/tokens/pushtoken.py
/// </summary>
public class PushToken : TokenClassBase
{
    public override string Type => "push";
    public override string DisplayName => "Push";
    public override bool SupportsChallengeResponse => true;

    public PushToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        var result = new AuthenticationResult();

        // Push tokens typically don't use PIN during authentication
        result.PinCorrect = true;

        // The OTP is the signature from the app
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
                result.Message = "Invalid signature";
            }
        }
        else
        {
            result.Success = false;
            result.Message = "Waiting for push confirmation";
        }

        return result;
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        // Push token verification checks the signature from the mobile app
        // The signature is created using the private key stored on the device
        // and verified using the public key stored in token info
        if (TokenEntity == null)
            return Task.FromResult(false);

        try
        {
            var publicKeyPem = GetTokenInfoValue("public_key_smartphone");
            if (string.IsNullOrEmpty(publicKeyPem))
                return Task.FromResult(false);

            // The OTP is actually a signature that needs to be verified
            // This is a simplified implementation - real implementation would
            // verify the signature against the challenge data
            
            // For now, return false as this requires full crypto implementation
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
            var txId = transactionId ?? Guid.NewGuid().ToString();
            var nonce = CryptoService.GenerateRandomHex(32);
            
            // Get Firebase token for sending push notification
            var firebaseToken = GetTokenInfoValue("firebase_token");
            var serial = TokenEntity.Serial;

            return Task.FromResult(new ChallengeResult
            {
                Success = true,
                TransactionId = txId,
                Message = "Please confirm the authentication on your smartphone",
                Challenge = nonce,
                Attributes = new Dictionary<string, object>
                {
                    ["serial"] = serial,
                    ["nonce"] = nonce,
                    ["firebase_token"] = firebaseToken ?? "",
                    ["title"] = GetTokenInfoValue("title") ?? "Authentication Request",
                    ["question"] = data ?? "Do you want to authenticate?"
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

    /// <summary>
    /// Check if the push token is enrolled (has required token info)
    /// </summary>
    public bool IsEnrolled()
    {
        return !string.IsNullOrEmpty(GetTokenInfoValue("public_key_smartphone")) &&
               !string.IsNullOrEmpty(GetTokenInfoValue("firebase_token"));
    }
}
