using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace PrivacyIDEA.Core.Tokens;

/// <summary>
/// OCRA Token implementation
/// Maps to Python: privacyidea/lib/tokens/ocratoken.py
/// Implements RFC 6287 OCRA algorithm
/// </summary>
public class OcraToken : TokenClassBase
{
    public override string Type => "ocra";
    public override string DisplayName => "OCRA Token";
    public override bool SupportsChallengeResponse => true;

    public OcraToken(ICryptoService cryptoService) : base(cryptoService)
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

        if (string.IsNullOrEmpty(otp))
            return new AuthenticationResult { Success = false, Message = "Response required" };

        var result = await CheckOtpAsync(otp);

        if (result)
        {
            await ResetFailCounterAsync();
            return new AuthenticationResult { Success = true, Message = "Authentication successful" };
        }

        await IncrementFailCounterAsync();
        return new AuthenticationResult { Success = false, Message = "Invalid OCRA response" };
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        var challenge = GetTokenInfoValue("ocra.challenge");
        if (string.IsNullOrEmpty(challenge))
            return Task.FromResult(false);

        try
        {
            var suite = GetTokenInfoValue("ocra.suite") ?? "OCRA-1:HOTP-SHA1-6:QN08";
            var secret = GetSecretKey();
            var expectedOtp = CalculateOcra(suite, secret, challenge);

            return Task.FromResult(CryptoService.SecureCompare(otp, expectedOtp));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public override Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        var suite = GetTokenInfoValue("ocra.suite") ?? "OCRA-1:HOTP-SHA1-6:QN08";
        var challengeLength = ExtractChallengeLength(suite);
        var challenge = GenerateChallenge(challengeLength);

        SetTokenInfo("ocra.challenge", challenge);

        return Task.FromResult(new ChallengeResult
        {
            Success = true,
            Challenge = challenge,
            Message = "Please provide the OCRA response",
            Attributes = new Dictionary<string, object>
            {
                ["ocra_suite"] = suite,
                ["mode"] = "ocra"
            }
        });
    }

    private string CalculateOcra(string suite, byte[] key, string challenge)
    {
        // Parse OCRA suite
        var parts = suite.Split(':');
        if (parts.Length < 3) throw new ArgumentException("Invalid OCRA suite");

        var cryptoFunction = parts[1]; // e.g., "HOTP-SHA1-6"
        var dataInput = parts[2]; // e.g., "QN08"

        // Parse crypto function
        var cryptoParts = cryptoFunction.Split('-');
        var hashAlgo = cryptoParts.Length > 1 ? cryptoParts[1] : "SHA1";
        var digits = cryptoParts.Length > 2 ? int.Parse(cryptoParts[2]) : 6;

        // Build data input
        var dataBytes = new List<byte>();
        
        // Suite string
        dataBytes.AddRange(Encoding.UTF8.GetBytes(suite));
        dataBytes.Add(0); // Separator

        // Parse data input format
        var idx = 0;
        while (idx < dataInput.Length)
        {
            var c = dataInput[idx];
            switch (c)
            {
                case 'Q': // Question/Challenge
                    idx++;
                    var qType = dataInput[idx++];
                    var qLen = int.Parse(dataInput.Substring(idx, 2));
                    idx += 2;
                    dataBytes.AddRange(FormatQuestion(challenge, qType, qLen));
                    break;
                case 'C': // Counter
                    var counterStr = GetTokenInfoValue("ocra.counter") ?? "0";
                    var counter = long.Parse(counterStr);
                    dataBytes.AddRange(BitConverter.GetBytes(counter).Reverse());
                    idx++;
                    break;
                case 'T': // Time
                    var timeStep = idx + 1 < dataInput.Length ? int.Parse(dataInput.Substring(idx + 1, 1)) : 1;
                    var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (timeStep * 60);
                    dataBytes.AddRange(BitConverter.GetBytes(time).Reverse());
                    idx += 2;
                    break;
                default:
                    idx++;
                    break;
            }
        }

        // Calculate HMAC
        byte[] hash;
        using (var hmac = hashAlgo.ToUpper() switch
        {
            "SHA256" => (HMAC)new HMACSHA256(key),
            "SHA512" => new HMACSHA512(key),
            _ => new HMACSHA1(key)
        })
        {
            hash = hmac.ComputeHash(dataBytes.ToArray());
        }

        // Truncate
        var offset = hash[^1] & 0x0f;
        var binary = (hash[offset] & 0x7f) << 24
                   | (hash[offset + 1] & 0xff) << 16
                   | (hash[offset + 2] & 0xff) << 8
                   | (hash[offset + 3] & 0xff);

        var otp = binary % (int)Math.Pow(10, digits);
        return otp.ToString($"D{digits}");
    }

    private static byte[] FormatQuestion(string question, char type, int length)
    {
        return type switch
        {
            'N' => Encoding.UTF8.GetBytes(question.PadLeft(length, '0')[^length..]),
            'A' => Encoding.UTF8.GetBytes(question.PadRight(length)[..length]),
            'H' => Convert.FromHexString(question.PadLeft(length * 2, '0')[^(length * 2)..]),
            _ => Encoding.UTF8.GetBytes(question)
        };
    }

    private static int ExtractChallengeLength(string suite)
    {
        var idx = suite.IndexOf(":Q");
        if (idx < 0) return 8;
        var len = suite.Substring(idx + 3, 2);
        return int.TryParse(len, out var l) ? l : 8;
    }

    private string GenerateChallenge(int length)
    {
        var bytes = CryptoService.GenerateRandomBytes(length);
        var number = new System.Numerics.BigInteger(bytes, isUnsigned: true);
        return (number % System.Numerics.BigInteger.Pow(10, length)).ToString($"D{length}");
    }
}

/// <summary>
/// Indexed Secret Token
/// Maps to Python: privacyidea/lib/tokens/indexedsecrettoken.py
/// Challenge asks for specific positions in a secret
/// </summary>
public class IndexedSecretToken : TokenClassBase
{
    public override string Type => "indexedsecret";
    public override string DisplayName => "Indexed Secret Token";
    public override bool SupportsChallengeResponse => true;

    public IndexedSecretToken(ICryptoService cryptoService) : base(cryptoService)
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
        var positionsStr = GetTokenInfoValue("indexedsecret.positions");
        if (string.IsNullOrEmpty(positionsStr))
            return Task.FromResult(false);

        try
        {
            var secret = Encoding.UTF8.GetString(GetSecretKey());
            var positions = positionsStr.Split(',').Select(int.Parse).ToArray();

            if (otp.Length != positions.Length)
                return Task.FromResult(false);

            var expected = new string(positions.Select(p => 
                p > 0 && p <= secret.Length ? secret[p - 1] : '\0').ToArray());

            return Task.FromResult(CryptoService.SecureCompare(otp, expected));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public override Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        var secret = Encoding.UTF8.GetString(GetSecretKey());
        var count = int.TryParse(GetTokenInfoValue("indexedsecret.count"), out var c) ? c : 4;

        // Generate random positions
        var random = new Random();
        var positions = Enumerable.Range(1, secret.Length)
            .OrderBy(_ => random.Next())
            .Take(count)
            .OrderBy(p => p)
            .ToArray();

        SetTokenInfo("indexedsecret.positions", string.Join(",", positions));

        return Task.FromResult(new ChallengeResult
        {
            Success = true,
            Challenge = string.Join(",", positions),
            Message = $"Enter characters at positions: {string.Join(", ", positions)}",
            Attributes = new Dictionary<string, object>
            {
                ["positions"] = positions,
                ["mode"] = "indexedsecret"
            }
        });
    }
}

/// <summary>
/// Remote Token - validates against remote server
/// Maps to Python: privacyidea/lib/tokens/remotetoken.py
/// </summary>
public class RemoteToken : TokenClassBase
{
    public override string Type => "remote";
    public override string DisplayName => "Remote Token";

    private readonly HttpClient? _httpClient;

    public RemoteToken(ICryptoService cryptoService, HttpClient? httpClient = null) : base(cryptoService)
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

        if (string.IsNullOrEmpty(otp))
            return new AuthenticationResult { Success = false, Message = "OTP required" };

        var result = await CheckOtpAsync(otp);

        if (result)
        {
            await ResetFailCounterAsync();
            return new AuthenticationResult { Success = true, Message = "Authentication successful" };
        }

        await IncrementFailCounterAsync();
        return new AuthenticationResult { Success = false, Message = "Remote validation failed" };
    }

    public override async Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        if (_httpClient == null) return false;

        var remoteUrl = GetTokenInfoValue("remote.server");
        var remoteSerial = GetTokenInfoValue("remote.serial");
        var remoteUser = GetTokenInfoValue("remote.user");
        var remoteRealm = GetTokenInfoValue("remote.realm");

        if (string.IsNullOrEmpty(remoteUrl))
            return false;

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["pass"] = otp,
                ["serial"] = remoteSerial ?? "",
                ["user"] = remoteUser ?? "",
                ["realm"] = remoteRealm ?? ""
            });

            var response = await _httpClient.PostAsync($"{remoteUrl}/validate/check", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Parse response (expecting JSON with "result" -> "value" = true/false)
            return responseBody.Contains("\"value\": true") || responseBody.Contains("\"value\":true");
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// TAN Token - Transaction Authentication Number list
/// Maps to Python: privacyidea/lib/tokens/tantoken.py
/// </summary>
public class TanToken : TokenClassBase
{
    public override string Type => "tan";
    public override string DisplayName => "TAN Token";

    public TanToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        if (string.IsNullOrEmpty(otp))
            return new AuthenticationResult { Success = false, Message = "TAN required" };

        var result = await CheckOtpAsync(otp);

        if (result)
        {
            await ResetFailCounterAsync();
            return new AuthenticationResult { Success = true, Message = "Authentication successful" };
        }

        await IncrementFailCounterAsync();
        return new AuthenticationResult { Success = false, Message = "Invalid TAN" };
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        try
        {
            // Get TAN list from token info
            var tanListStr = GetTokenInfoValue("tan.list");
            if (string.IsNullOrEmpty(tanListStr))
                return Task.FromResult(false);

            var tanList = tanListStr.Split(',').ToList();

            // Find and remove the TAN
            var idx = tanList.FindIndex(t => CryptoService.SecureCompare(t.Trim(), otp));
            if (idx < 0)
                return Task.FromResult(false);

            // Remove used TAN
            tanList.RemoveAt(idx);
            SetTokenInfo("tan.list", string.Join(",", tanList));

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Set the TAN list for this token
    /// </summary>
    public void SetTanList(IEnumerable<string> tans)
    {
        SetTokenInfo("tan.list", string.Join(",", tans));
    }

    /// <summary>
    /// Get remaining TANs count
    /// </summary>
    public int GetRemainingCount()
    {
        var tanListStr = GetTokenInfoValue("tan.list");
        if (string.IsNullOrEmpty(tanListStr))
            return 0;
        return tanListStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

/// <summary>
/// VASCO Token implementation
/// Maps to Python: privacyidea/lib/tokens/vascotoken.py
/// VASCO/OneSpan Digipass token support
/// </summary>
public class VascoToken : TokenClassBase
{
    public override string Type => "vasco";
    public override string DisplayName => "VASCO Token";

    public VascoToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        if (string.IsNullOrEmpty(otp))
            return new AuthenticationResult { Success = false, Message = "OTP required" };

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
        // VASCO validation requires proprietary SDK
        // This is a placeholder for integration with VASCO/OneSpan Authentication Server
        
        // Get the blob containing encrypted token data
        var blob = GetTokenInfoValue("vasco.blob");
        if (string.IsNullOrEmpty(blob))
            return Task.FromResult(false);

        // In production, this would call the VASCO Authentication Server API
        // or use the VASCO VACMAN Controller SDK
        throw new NotImplementedException("VASCO validation requires proprietary SDK integration");
    }
}

/// <summary>
/// Spass Token - simple password token
/// Maps to Python: privacyidea/lib/tokens/spasstoken.py
/// Just validates a static password
/// </summary>
public class SpassToken : TokenClassBase
{
    public override string Type => "spass";
    public override string DisplayName => "Simple Password Token";

    public SpassToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        // For spass token, OTP is actually the password
        var password = string.IsNullOrEmpty(otp) ? pin : otp;

        if (string.IsNullOrEmpty(password))
            return new AuthenticationResult { Success = false, Message = "Password required" };

        var result = await CheckOtpAsync(password);

        if (result)
        {
            await ResetFailCounterAsync();
            return new AuthenticationResult { Success = true, Message = "Authentication successful" };
        }

        await IncrementFailCounterAsync();
        return new AuthenticationResult { Success = false, Message = "Invalid password" };
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        // Check against stored password hash
        var storedHash = GetTokenInfoValue("spass.password_hash");
        
        if (string.IsNullOrEmpty(storedHash))
        {
            // Fallback to key-based comparison
            var storedPassword = Encoding.UTF8.GetString(GetSecretKey());
            return Task.FromResult(CryptoService.SecureCompare(otp, storedPassword));
        }

        var result = CryptoService.VerifyPassword(otp, storedHash);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Set the password for this token
    /// </summary>
    public void SetPassword(string password)
    {
        var hash = CryptoService.HashPassword(password);
        SetTokenInfo("spass.password_hash", hash);
    }
}

/// <summary>
/// Daplug Token implementation
/// Maps to Python: privacyidea/lib/tokens/daplugtoken.py
/// Daplug hardware token support
/// </summary>
public class DaplugToken : HotpToken
{
    public override string Type => "daplug";
    public override string DisplayName => "Daplug Token";

    public DaplugToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    // Daplug uses HOTP with specific encoding
    // The main difference is in how the OTP is transmitted (USB HID)
}
