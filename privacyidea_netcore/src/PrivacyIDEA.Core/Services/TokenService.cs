using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Core.Tokens;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// Token Service implementation
/// Maps to Python: privacyidea/lib/token.py
/// </summary>
public class TokenService : ITokenService
{
    private readonly ITokenRepository _tokenRepository;
    private readonly IRealmRepository _realmRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<TokenService> _logger;

    private readonly Dictionary<string, Func<ITokenClass>> _tokenFactories;

    public TokenService(
        IUnitOfWork unitOfWork,
        ICryptoService cryptoService,
        ILogger<TokenService> logger)
    {
        _unitOfWork = unitOfWork;
        _tokenRepository = unitOfWork.Tokens;
        _realmRepository = unitOfWork.Realms;
        _cryptoService = cryptoService;
        _logger = logger;

        // Register token type factories
        _tokenFactories = new Dictionary<string, Func<ITokenClass>>(StringComparer.OrdinalIgnoreCase)
        {
            // Core OTP tokens
            ["hotp"] = () => new HotpToken(_cryptoService),
            ["totp"] = () => new TotpToken(_cryptoService),
            
            // Challenge-response tokens
            ["sms"] = () => new SmsToken(_cryptoService),
            ["email"] = () => new EmailToken(_cryptoService),
            ["push"] = () => new PushToken(_cryptoService),
            
            // Additional tokens (from AdditionalTokens.cs)
            ["motp"] = () => new MotpToken(_cryptoService),
            ["pw"] = () => new PasswordToken(_cryptoService),
            ["password"] = () => new PasswordToken(_cryptoService),
            ["registration"] = () => new RegistrationToken(_cryptoService),
            ["paper"] = () => new PaperToken(_cryptoService),
            ["daypassword"] = () => new DayPasswordToken(_cryptoService),
            ["question"] = () => new QuestionnaireToken(_cryptoService),
            ["4eyes"] = () => new FourEyesToken(_cryptoService),
            ["radius"] = () => new RadiusToken(_cryptoService),
            ["certificate"] = () => new CertificateToken(_cryptoService),
            ["sshkey"] = () => new SshKeyToken(_cryptoService),
            
            // WebAuthn/FIDO tokens (from WebAuthnTokens.cs)
            ["webauthn"] = () => new WebAuthnToken(_cryptoService),
            ["passkey"] = () => new PasskeyToken(_cryptoService),
            ["u2f"] = () => new U2fToken(_cryptoService),
            ["tiqr"] = () => new TiqrToken(_cryptoService),
            ["yubico"] = () => new YubicoToken(_cryptoService),
            ["yubikey"] = () => new YubiKeyToken(_cryptoService),
            
            // Specialized tokens (from SpecializedTokens.cs)
            ["ocra"] = () => new OcraToken(_cryptoService),
            ["indexedsecret"] = () => new IndexedSecretToken(_cryptoService),
            ["remote"] = () => new RemoteToken(_cryptoService),
            ["tan"] = () => new TanToken(_cryptoService),
            ["vasco"] = () => new VascoToken(_cryptoService),
            ["spass"] = () => new SpassToken(_cryptoService),
            ["daplug"] = () => new DaplugToken(_cryptoService),
        };
    }

    public async Task<Token?> GetTokenBySerialAsync(string serial)
    {
        return await _tokenRepository.GetBySerialAsync(serial);
    }

    public async Task<IEnumerable<Token>> GetTokensForUserAsync(string userId, string? realm = null)
    {
        return await _tokenRepository.GetForUserAsync(userId, realm);
    }

    public async Task<PagedResult<Token>> SearchTokensAsync(TokenSearchFilter filter, int page = 1, int pageSize = 15)
    {
        return await _tokenRepository.SearchAsync(filter, page, pageSize);
    }

    public async Task<Token> InitTokenAsync(TokenInitParameters parameters)
    {
        var serial = parameters.Serial ?? GenerateSerial(parameters.Type);

        _logger.LogInformation("Initializing token {Serial} of type {Type}", serial, parameters.Type);

        var token = new Token
        {
            Serial = serial,
            TokenType = parameters.Type.ToLower(),
            Description = parameters.Description ?? string.Empty,
            OtpLen = parameters.OtpLen,
            Active = true
        };

        // Generate or set secret key
        if (parameters.GenerateKey || parameters.OtpKey == null)
        {
            var keyLength = parameters.Type.ToLower() switch
            {
                "totp" when parameters.HashAlgorithm == "sha256" => 32,
                "totp" when parameters.HashAlgorithm == "sha512" => 64,
                _ => 20
            };
            var secretKey = _cryptoService.GenerateRandomBytes(keyLength);
            var (encKey, iv) = _cryptoService.EncryptKey(secretKey);
            token.KeyEnc = encKey;
            token.KeyIv = iv;
        }
        else
        {
            var (encKey, iv) = _cryptoService.EncryptKey(parameters.OtpKey);
            token.KeyEnc = encKey;
            token.KeyIv = iv;
        }

        // Set PIN if provided
        if (!string.IsNullOrEmpty(parameters.Pin))
        {
            token.PinHash = _cryptoService.HashPin(parameters.Pin);
        }

        await _tokenRepository.AddAsync(token);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Token {Serial} created successfully", serial);

        return token;
    }

    public async Task<bool> DeleteTokenAsync(string serial)
    {
        var token = await _tokenRepository.GetBySerialAsync(serial);
        if (token == null) return false;

        await _tokenRepository.DeleteAsync(token);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Token {Serial} deleted", serial);
        return true;
    }

    public async Task<bool> EnableTokenAsync(string serial)
    {
        var token = await _tokenRepository.GetBySerialAsync(serial);
        if (token == null) return false;

        token.Active = true;
        await _tokenRepository.UpdateAsync(token);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DisableTokenAsync(string serial)
    {
        var token = await _tokenRepository.GetBySerialAsync(serial);
        if (token == null) return false;

        token.Active = false;
        await _tokenRepository.UpdateAsync(token);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignTokenAsync(string serial, string userId, string? resolver = null, string? realm = null)
    {
        var token = await _tokenRepository.GetBySerialAsync(serial);
        if (token == null) return false;

        // This would need proper implementation with TokenOwner
        // For now, just return true
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnassignTokenAsync(string serial)
    {
        var token = await _tokenRepository.GetBySerialAsync(serial);
        if (token == null) return false;

        // This would need proper implementation to clear TokenOwners
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetPinAsync(string serial, string pin)
    {
        var token = await _tokenRepository.GetBySerialAsync(serial);
        if (token == null) return false;

        token.PinHash = _cryptoService.HashPin(pin);
        await _tokenRepository.UpdateAsync(token);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetFailCounterAsync(string serial)
    {
        var token = await _tokenRepository.GetBySerialAsync(serial);
        if (token == null) return false;

        token.FailCount = 0;
        await _tokenRepository.UpdateAsync(token);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResyncTokenAsync(string serial, string otp1, string otp2)
    {
        var token = await GetTokenBySerialAsync(serial);
        if (token == null) return false;

        var tokenClass = GetTokenClassForType(token.TokenType);
        tokenClass.Initialize(token);

        var result = await tokenClass.ResyncAsync(otp1, otp2);
        if (result)
        {
            await _tokenRepository.UpdateAsync(token);
            await _unitOfWork.SaveChangesAsync();
        }

        return result;
    }

    public ITokenClass GetTokenClassForType(string tokenType)
    {
        if (_tokenFactories.TryGetValue(tokenType, out var factory))
        {
            return factory();
        }

        // Default to HOTP
        _logger.LogWarning("Unknown token type {TokenType}, defaulting to HOTP", tokenType);
        return new HotpToken(_cryptoService);
    }

    public async Task<bool> SetTokenInfoAsync(string serial, string key, string value)
    {
        var token = await _tokenRepository.GetBySerialAsync(serial);
        if (token == null) return false;

        // Would need to update TokenInfo through repository
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<Dictionary<string, string>> GetTokenInfoAsync(string serial)
    {
        var token = await _tokenRepository.GetBySerialAsync(serial);

        if (token == null) return new Dictionary<string, string>();

        return token.TokenInfos?.ToDictionary(i => i.Key, i => i.Value ?? string.Empty) 
               ?? new Dictionary<string, string>();
    }

    public async Task<bool> RevokeTokenAsync(string serial)
    {
        var token = await _tokenRepository.GetBySerialAsync(serial);
        if (token == null) return false;

        token.Revoked = true;
        token.Active = false;
        await _tokenRepository.UpdateAsync(token);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<TokenStatistics> GetStatisticsAsync()
    {
        var stats = new TokenStatistics
        {
            TotalTokens = await _tokenRepository.CountAsync(),
            ActiveTokens = await _tokenRepository.CountActiveAsync(),
            AssignedTokens = await _tokenRepository.CountAssignedAsync(),
            ByType = await _tokenRepository.CountByTypeAsync()
        };

        return stats;
    }

    private static string GenerateSerial(string tokenType)
    {
        var prefix = tokenType.ToUpper() switch
        {
            "HOTP" => "OATH",
            "TOTP" => "TOTP",
            "SMS" => "SMSTOKEN",
            "EMAIL" => "EMAIL",
            "PUSH" => "PIPU",
            "WEBAUTHN" or "PASSKEY" => "WAN",
            _ => tokenType.ToUpper()[..Math.Min(4, tokenType.Length)]
        };

        return $"{prefix}{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
