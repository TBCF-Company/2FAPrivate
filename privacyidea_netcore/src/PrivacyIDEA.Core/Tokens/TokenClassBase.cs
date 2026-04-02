using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Tokens;

/// <summary>
/// Base class for all token implementations
/// Maps to Python: privacyidea/lib/tokenclass.py - TokenClass
/// </summary>
public abstract class TokenClassBase : ITokenClass
{
    protected Token? TokenEntity { get; private set; }
    protected ICryptoService CryptoService { get; }
    protected Dictionary<string, object> TokenInfoCache { get; } = new();

    public abstract string Type { get; }
    public abstract string DisplayName { get; }
    public virtual bool SupportsChallengeResponse => false;
    public virtual bool SupportsOffline => false;

    public bool IsActive => TokenEntity?.Active ?? false;
    public bool IsLocked => TokenEntity?.FailCount >= TokenEntity?.MaxFail;

    protected TokenClassBase(ICryptoService cryptoService)
    {
        CryptoService = cryptoService;
    }

    public virtual void Initialize(Token tokenEntity)
    {
        TokenEntity = tokenEntity;
        LoadTokenInfo();
    }

    protected virtual void LoadTokenInfo()
    {
        TokenInfoCache.Clear();
        if (TokenEntity?.TokenInfos != null)
        {
            foreach (var info in TokenEntity.TokenInfos)
            {
                TokenInfoCache[info.Key] = info.Value ?? string.Empty;
            }
        }
    }

    public abstract Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp);

    public abstract Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null);

    public virtual bool CheckOtp(string otp, Token token)
    {
        // Synchronous wrapper - uses the token entity passed in
        Initialize(token);
        return CheckOtpAsync(otp).GetAwaiter().GetResult();
    }

    public virtual async Task<bool> CheckPinAsync(string pin)
    {
        if (TokenEntity == null || string.IsNullOrEmpty(TokenEntity.PinHash))
            return true; // No PIN set

        return CryptoService.VerifyPinHash(pin, TokenEntity.PinHash);
    }

    public virtual Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        return Task.FromResult(new ChallengeResult
        {
            Success = false,
            Message = "This token type does not support challenge-response"
        });
    }

    public virtual Task UpdateAsync()
    {
        // Override in subclasses to update token state
        return Task.CompletedTask;
    }

    public virtual Dictionary<string, object> GetTokenInfo()
    {
        return new Dictionary<string, object>(TokenInfoCache);
    }

    public virtual void SetTokenInfo(string key, object value)
    {
        TokenInfoCache[key] = value;
    }

    protected string? GetTokenInfoValue(string key)
    {
        return TokenInfoCache.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    public virtual Task<string?> GetOtpAsync(long? timestamp = null)
    {
        return Task.FromResult<string?>(null);
    }

    public virtual Task<bool> ResyncAsync(string otp1, string otp2)
    {
        return Task.FromResult(false);
    }

    public virtual async Task SetPinAsync(string pin)
    {
        if (TokenEntity != null)
        {
            TokenEntity.PinHash = CryptoService.HashPin(pin);
        }
    }

    public virtual Task ResetFailCounterAsync()
    {
        if (TokenEntity != null)
        {
            TokenEntity.FailCount = 0;
        }
        return Task.CompletedTask;
    }

    public virtual Task IncrementFailCounterAsync()
    {
        if (TokenEntity != null)
        {
            TokenEntity.FailCount++;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get the decrypted secret key
    /// </summary>
    protected byte[] GetSecretKey()
    {
        if (TokenEntity == null || string.IsNullOrEmpty(TokenEntity.KeyEnc) || string.IsNullOrEmpty(TokenEntity.KeyIv))
            throw new InvalidOperationException("Token secret key not available");

        return CryptoService.DecryptKey(TokenEntity.KeyEnc, TokenEntity.KeyIv);
    }

    /// <summary>
    /// Set the encrypted secret key
    /// </summary>
    protected void SetSecretKey(byte[] key)
    {
        if (TokenEntity == null)
            throw new InvalidOperationException("Token entity not initialized");

        var (encryptedKey, iv) = CryptoService.EncryptKey(key);
        TokenEntity.KeyEnc = encryptedKey;
        TokenEntity.KeyIv = iv;
    }

    /// <summary>
    /// Generate serial number for new token
    /// </summary>
    public static string GenerateSerial(string prefix, int length = 8)
    {
        var random = new Random();
        var serial = new char[length];
        const string chars = "0123456789ABCDEF";
        
        for (int i = 0; i < length; i++)
        {
            serial[i] = chars[random.Next(chars.Length)];
        }
        
        return $"{prefix}{new string(serial)}";
    }
}
