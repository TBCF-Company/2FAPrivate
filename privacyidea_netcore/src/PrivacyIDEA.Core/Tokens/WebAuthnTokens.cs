using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;
using System.Text.Json;

namespace PrivacyIDEA.Core.Tokens;

/// <summary>
/// WebAuthn Token implementation
/// Maps to Python: privacyidea/lib/tokens/webauthntoken.py
/// FIDO2 authentication using platform authenticators or security keys
/// </summary>
public class WebAuthnToken : TokenClassBase
{
    public override string Type => "webauthn";
    public override string DisplayName => "WebAuthn Token";
    public override bool SupportsChallengeResponse => true;

    public WebAuthnToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        // WebAuthn doesn't use traditional OTP
        return Task.FromResult(new AuthenticationResult
        {
            Success = false,
            Message = "WebAuthn requires challenge-response authentication"
        });
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        return Task.FromResult(false);
    }

    public override Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        var challenge = CryptoService.GenerateRandomBytes(32);
        var rpId = GetTokenInfoValue("webauthn.rp_id") ?? "localhost";
        var credentialId = GetTokenInfoValue("webauthn.credential_id");

        if (string.IsNullOrEmpty(credentialId))
        {
            return Task.FromResult(new ChallengeResult { Success = false, Message = "No credential registered" });
        }

        var options = new
        {
            challenge = Convert.ToBase64String(challenge),
            rpId,
            timeout = 60000,
            allowCredentials = new[]
            {
                new
                {
                    type = "public-key",
                    id = credentialId
                }
            },
            userVerification = GetTokenInfoValue("webauthn.user_verification") ?? "preferred"
        };

        // Store challenge for verification
        SetTokenInfo("webauthn.challenge", Convert.ToBase64String(challenge));

        return Task.FromResult(new ChallengeResult
        {
            Success = true,
            Challenge = Convert.ToBase64String(challenge),
            Message = "Please confirm with your authenticator",
            Attributes = new Dictionary<string, object>
            {
                ["webAuthnSignRequest"] = options,
                ["mode"] = "webauthn"
            }
        });
    }

    /// <summary>
    /// Verify WebAuthn assertion response
    /// </summary>
    public async Task<AuthenticationResult> VerifyAssertionAsync(
        string clientDataJson,
        string authenticatorData,
        string signature,
        string? userHandle = null)
    {
        try
        {
            var publicKey = GetPublicKey();
            if (publicKey == null)
            {
                return new AuthenticationResult { Success = false, Message = "No public key stored" };
            }

            // Decode client data
            var clientDataBytes = Convert.FromBase64String(clientDataJson);
            var clientData = JsonSerializer.Deserialize<WebAuthnClientData>(clientDataBytes);

            if (clientData == null)
            {
                return new AuthenticationResult { Success = false, Message = "Invalid client data" };
            }

            // Verify challenge
            var storedChallenge = GetTokenInfoValue("webauthn.challenge");
            if (storedChallenge != clientData.Challenge)
            {
                return new AuthenticationResult { Success = false, Message = "Challenge mismatch" };
            }

            // Verify origin
            var expectedOrigin = GetTokenInfoValue("webauthn.origin");
            if (!string.IsNullOrEmpty(expectedOrigin) && expectedOrigin != clientData.Origin)
            {
                return new AuthenticationResult { Success = false, Message = "Origin mismatch" };
            }

            // Verify signature
            var authDataBytes = Convert.FromBase64String(authenticatorData);
            var clientDataHash = CryptoService.Sha256(clientDataBytes);
            var signedData = authDataBytes.Concat(clientDataHash).ToArray();
            var signatureBytes = Convert.FromBase64String(signature);

            var verified = await VerifySignatureAsync(publicKey, signedData, signatureBytes);

            if (!verified)
            {
                return new AuthenticationResult { Success = false, Message = "Signature verification failed" };
            }

            // Update sign count
            var newSignCount = ExtractSignCount(authDataBytes);
            var storedSignCount = int.TryParse(GetTokenInfoValue("webauthn.sign_count"), out var sc) ? sc : 0;

            if (newSignCount <= storedSignCount && storedSignCount > 0)
            {
                return new AuthenticationResult { Success = false, Message = "Sign count mismatch - possible cloned authenticator" };
            }

            SetTokenInfo("webauthn.sign_count", newSignCount.ToString());

            return new AuthenticationResult { Success = true, Message = "Authentication successful" };
        }
        catch (Exception ex)
        {
            return new AuthenticationResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Register a new WebAuthn credential
    /// </summary>
    public void RegisterCredential(
        string credentialId,
        byte[] publicKey,
        string origin,
        string rpId)
    {
        SetTokenInfo("webauthn.credential_id", credentialId);
        SetTokenInfo("webauthn.public_key", Convert.ToBase64String(publicKey));
        SetTokenInfo("webauthn.origin", origin);
        SetTokenInfo("webauthn.rp_id", rpId);
        SetTokenInfo("webauthn.sign_count", "0");
    }

    private byte[]? GetPublicKey()
    {
        var keyBase64 = GetTokenInfoValue("webauthn.public_key");
        return string.IsNullOrEmpty(keyBase64) ? null : Convert.FromBase64String(keyBase64);
    }

    private Task<bool> VerifySignatureAsync(byte[] publicKey, byte[] data, byte[] signature)
    {
        try
        {
            // Simplified verification - in production would use proper COSE key parsing
            // and support multiple algorithms (ES256, RS256, etc.)
            
            // For ES256 (most common)
            using var ecdsa = System.Security.Cryptography.ECDsa.Create();
            
            // Parse COSE public key (simplified)
            // In production, use a proper CBOR library
            if (publicKey.Length >= 64)
            {
                var x = publicKey[..32];
                var y = publicKey[32..64];
                
                var parameters = new System.Security.Cryptography.ECParameters
                {
                    Curve = System.Security.Cryptography.ECCurve.NamedCurves.nistP256,
                    Q = new System.Security.Cryptography.ECPoint { X = x, Y = y }
                };
                ecdsa.ImportParameters(parameters);

                return Task.FromResult(ecdsa.VerifyData(data, signature, 
                    System.Security.Cryptography.HashAlgorithmName.SHA256));
            }

            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static int ExtractSignCount(byte[] authData)
    {
        // Sign count is at offset 33-36 (after rpIdHash and flags)
        if (authData.Length < 37) return 0;
        return (authData[33] << 24) | (authData[34] << 16) | (authData[35] << 8) | authData[36];
    }
}

/// <summary>
/// WebAuthn client data structure
/// </summary>
public class WebAuthnClientData
{
    public string? Type { get; set; }
    public string? Challenge { get; set; }
    public string? Origin { get; set; }
    public bool CrossOrigin { get; set; }
}

/// <summary>
/// Passkey Token - extends WebAuthn with discoverable credentials
/// Maps to Python: privacyidea/lib/tokens/passkeytoken.py
/// </summary>
public class PasskeyToken : WebAuthnToken
{
    public override string Type => "passkey";
    public override string DisplayName => "Passkey Token";

    public PasskeyToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        var challenge = CryptoService.GenerateRandomBytes(32);
        var rpId = GetTokenInfoValue("passkey.rp_id") ?? "localhost";

        // Passkey uses discoverable credentials - no credential ID needed in request
        var options = new
        {
            challenge = Convert.ToBase64String(challenge),
            rpId,
            timeout = 60000,
            userVerification = "required", // Passkeys require user verification
        };

        SetTokenInfo("webauthn.challenge", Convert.ToBase64String(challenge));

        return Task.FromResult(new ChallengeResult
        {
            Success = true,
            Challenge = Convert.ToBase64String(challenge),
            Message = "Please authenticate with your passkey",
            Attributes = new Dictionary<string, object>
            {
                ["webAuthnSignRequest"] = options,
                ["mode"] = "passkey"
            }
        });
    }
}

/// <summary>
/// U2F Token (legacy, superseded by WebAuthn)
/// Maps to Python: privacyidea/lib/tokens/u2ftoken.py
/// </summary>
public class U2fToken : TokenClassBase
{
    public override string Type => "u2f";
    public override string DisplayName => "U2F Token";
    public override bool SupportsChallengeResponse => true;

    public U2fToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        return Task.FromResult(new AuthenticationResult
        {
            Success = false,
            Message = "U2F requires challenge-response authentication"
        });
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        return Task.FromResult(false);
    }

    public override Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        var challenge = CryptoService.GenerateRandomBytes(32);
        var keyHandle = GetTokenInfoValue("u2f.keyHandle");
        var appId = GetTokenInfoValue("u2f.appId") ?? "https://localhost";

        if (string.IsNullOrEmpty(keyHandle))
        {
            return Task.FromResult(new ChallengeResult { Success = false, Message = "No U2F key registered" });
        }

        var signRequest = new
        {
            version = "U2F_V2",
            challenge = Convert.ToBase64String(challenge),
            keyHandle,
            appId
        };

        SetTokenInfo("u2f.challenge", Convert.ToBase64String(challenge));

        return Task.FromResult(new ChallengeResult
        {
            Success = true,
            Challenge = Convert.ToBase64String(challenge),
            Message = "Please touch your U2F device",
            Attributes = new Dictionary<string, object>
            {
                ["u2fSignRequest"] = signRequest,
                ["mode"] = "u2f"
            }
        });
    }
}

/// <summary>
/// TiQR Token implementation
/// Maps to Python: privacyidea/lib/tokens/tiqrtoken.py
/// QR-code based challenge response
/// </summary>
public class TiqrToken : TokenClassBase
{
    public override string Type => "tiqr";
    public override string DisplayName => "TiQR Token";
    public override bool SupportsChallengeResponse => true;

    public TiqrToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        if (string.IsNullOrEmpty(otp))
            return new AuthenticationResult { Success = false, Message = "Response required" };

        var result = await CheckOtpAsync(otp);
        
        if (result)
        {
            await ResetFailCounterAsync();
            return new AuthenticationResult { Success = true, Message = "Authentication successful" };
        }

        await IncrementFailCounterAsync();
        return new AuthenticationResult { Success = false, Message = "Invalid response" };
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        // TiQR uses OCRA-based verification
        var challenge = GetTokenInfoValue("tiqr.challenge");
        if (string.IsNullOrEmpty(challenge))
            return Task.FromResult(false);

        try
        {
            var secret = GetSecretKey();
            var expectedResponse = CalculateOcraResponse(secret, challenge);
            return Task.FromResult(CryptoService.SecureCompare(otp, expectedResponse));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public override Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        var challenge = Convert.ToHexString(CryptoService.GenerateRandomBytes(16));
        var serviceUrl = GetTokenInfoValue("tiqr.service_url") ?? "https://localhost/tiqr";
        var serial = TokenEntity?.Serial ?? "";

        // Store challenge for verification
        SetTokenInfo("tiqr.challenge", challenge);

        // Build TiQR authentication URL
        var authUrl = $"tiqrauth://{serviceUrl}?c={challenge}&s={serial}&t={transactionId}";

        return Task.FromResult(new ChallengeResult
        {
            Success = true,
            Challenge = challenge,
            Message = "Scan the QR code with your TiQR app",
            Attributes = new Dictionary<string, object>
            {
                ["tiqr_url"] = authUrl,
                ["mode"] = "tiqr"
            }
        });
    }

    private string CalculateOcraResponse(byte[] secret, string challenge)
    {
        // Simplified OCRA calculation
        var challengeBytes = System.Text.Encoding.ASCII.GetBytes(challenge);
        var hash = CryptoService.HmacSha1(secret, challengeBytes);
        var offset = hash[^1] & 0x0f;
        var binary = (hash[offset] & 0x7f) << 24
                   | (hash[offset + 1] & 0xff) << 16
                   | (hash[offset + 2] & 0xff) << 8
                   | (hash[offset + 3] & 0xff);
        return (binary % 1000000).ToString("D6");
    }
}

/// <summary>
/// Yubico OTP Token implementation
/// Maps to Python: privacyidea/lib/tokens/yubicotoken.py
/// Validates Yubico OTP against Yubico Cloud or local validation
/// </summary>
public class YubicoToken : TokenClassBase
{
    public override string Type => "yubico";
    public override string DisplayName => "Yubico Cloud Token";

    private readonly HttpClient? _httpClient;

    public YubicoToken(ICryptoService cryptoService, HttpClient? httpClient = null) : base(cryptoService)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        // Check PIN first if set
        if (!string.IsNullOrEmpty(TokenEntity.PinHash))
        {
            if (string.IsNullOrEmpty(pin) || !await CheckPinAsync(pin))
            {
                await IncrementFailCounterAsync();
                return new AuthenticationResult { Success = false, Message = "Invalid PIN" };
            }
        }

        if (string.IsNullOrEmpty(otp) || otp.Length < 32)
            return new AuthenticationResult { Success = false, Message = "Invalid Yubico OTP" };

        var result = await CheckOtpAsync(otp);
        
        if (result)
        {
            await ResetFailCounterAsync();
            return new AuthenticationResult { Success = true, Message = "Authentication successful" };
        }

        await IncrementFailCounterAsync();
        return new AuthenticationResult { Success = false, Message = "Yubico validation failed" };
    }

    public override async Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        if (otp.Length < 32) return false;

        // Extract public ID (first 12 chars)
        var publicId = otp[..12];
        var storedPublicId = GetTokenInfoValue("yubico.tokenid");

        if (!CryptoService.SecureCompare(publicId, storedPublicId ?? ""))
            return false;

        // Validate with Yubico Cloud
        return await ValidateWithYubicoCloudAsync(otp);
    }

    private async Task<bool> ValidateWithYubicoCloudAsync(string otp)
    {
        if (_httpClient == null) return false;

        var clientId = GetTokenInfoValue("yubico.client_id");

        if (string.IsNullOrEmpty(clientId)) return false;

        var nonce = Convert.ToHexString(CryptoService.GenerateRandomBytes(16));
        var url = $"https://api.yubico.com/wsapi/2.0/verify?id={clientId}&nonce={nonce}&otp={otp}";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            return response.Contains("status=OK");
        }
        catch
        {
            return false;
        }
    }

    public void SetYubicoCredentials(string publicId, string clientId, string? apiKey = null)
    {
        SetTokenInfo("yubico.tokenid", publicId);
        SetTokenInfo("yubico.client_id", clientId);
        if (!string.IsNullOrEmpty(apiKey))
        {
            SetTokenInfo("yubico.api_key", apiKey);
        }
    }
}

/// <summary>
/// YubiKey Token implementation (local AES validation)
/// Maps to Python: privacyidea/lib/tokens/yubikeytoken.py
/// </summary>
public class YubiKeyToken : TokenClassBase
{
    public override string Type => "yubikey";
    public override string DisplayName => "YubiKey Token";

    public YubiKeyToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        // Check PIN first if set
        if (!string.IsNullOrEmpty(TokenEntity.PinHash))
        {
            if (string.IsNullOrEmpty(pin) || !await CheckPinAsync(pin))
            {
                await IncrementFailCounterAsync();
                return new AuthenticationResult { Success = false, Message = "Invalid PIN" };
            }
        }

        if (string.IsNullOrEmpty(otp) || otp.Length < 32)
            return new AuthenticationResult { Success = false, Message = "Invalid YubiKey OTP" };

        var result = await CheckOtpAsync(otp);
        
        if (result)
        {
            await ResetFailCounterAsync();
            return new AuthenticationResult { Success = true, Message = "Authentication successful" };
        }

        await IncrementFailCounterAsync();
        return new AuthenticationResult { Success = false, Message = "Invalid OTP" };
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        if (otp.Length < 32) return Task.FromResult(false);

        try
        {
            // Extract public ID (first 12 chars in modhex)
            var publicId = otp[..12];
            var storedPublicId = GetTokenInfoValue("yubikey.publicid");

            if (!CryptoService.SecureCompare(publicId.ToLower(), storedPublicId?.ToLower() ?? ""))
                return Task.FromResult(false);

            // Decode modhex OTP
            var otpPart = otp[12..];
            var decoded = ModhexDecode(otpPart);
            if (decoded == null || decoded.Length != 16)
                return Task.FromResult(false);

            // Get AES key
            var aesKey = GetSecretKey();
            
            // Decrypt OTP
            var decrypted = CryptoService.AesDecrypt(decoded, aesKey, new byte[16]);

            // Verify CRC
            if (!VerifyCrc(decrypted))
                return Task.FromResult(false);

            // Extract and verify counter
            var sessionCounter = (decrypted[0] << 8) | decrypted[1];
            var useCounter = (decrypted[7] << 8) | decrypted[8];

            var storedSessionCounter = int.TryParse(GetTokenInfoValue("yubikey.session_counter"), out var ssc) ? ssc : 0;
            var storedUseCounter = int.TryParse(GetTokenInfoValue("yubikey.use_counter"), out var suc) ? suc : 0;

            // Counter must be greater
            if (sessionCounter < storedSessionCounter)
                return Task.FromResult(false);
            if (sessionCounter == storedSessionCounter && useCounter <= storedUseCounter)
                return Task.FromResult(false);

            // Update counters
            SetTokenInfo("yubikey.session_counter", sessionCounter.ToString());
            SetTokenInfo("yubikey.use_counter", useCounter.ToString());

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static byte[]? ModhexDecode(string modhex)
    {
        const string modhexChars = "cbdefghijklnrtuv";
        var result = new List<byte>();

        for (int i = 0; i < modhex.Length; i += 2)
        {
            var high = modhexChars.IndexOf(char.ToLower(modhex[i]));
            var low = modhexChars.IndexOf(char.ToLower(modhex[i + 1]));
            if (high < 0 || low < 0) return null;
            result.Add((byte)((high << 4) | low));
        }

        return result.ToArray();
    }

    private static bool VerifyCrc(byte[] data)
    {
        ushort crc = 0xffff;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                var j = crc & 1;
                crc >>= 1;
                if (j != 0) crc ^= 0x8408;
            }
        }
        return crc == 0xf0b8;
    }
}
