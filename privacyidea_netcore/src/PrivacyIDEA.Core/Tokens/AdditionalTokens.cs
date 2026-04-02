using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Tokens;

/// <summary>
/// mOTP (Mobile OTP) Token implementation
/// Maps to Python: privacyidea/lib/tokens/motptoken.py
/// Mobile OTP uses: OTP = truncate(MD5(epoch + pin + secret))
/// </summary>
public class MotpToken : TokenClassBase
{
    public override string Type => "motp";
    public override string DisplayName => "mOTP Token";

    public MotpToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        if (string.IsNullOrEmpty(otp))
            return new AuthenticationResult { Success = false, Message = "OTP required" };

        // mOTP requires PIN as part of the OTP calculation
        if (string.IsNullOrEmpty(pin))
            return new AuthenticationResult { Success = false, Message = "PIN required for mOTP" };

        var result = await CheckOtpAsync(otp, pin: pin);
        
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
        return CheckOtpAsync(otp, pin: null);
    }

    private Task<bool> CheckOtpAsync(string otp, string? pin)
    {
        if (TokenEntity == null) return Task.FromResult(false);

        try
        {
            var secret = GetSecretKey();
            var motpPin = pin ?? GetTokenInfoValue("pin") ?? "";

            // Check current and nearby time windows (10-second periods)
            var checkWindow = 3;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 10;

            for (var offset = -checkWindow; offset <= checkWindow; offset++)
            {
                var epoch = now + offset;
                var expectedOtp = CalculateMotp(epoch, motpPin, secret);
                
                if (CryptoService.SecureCompare(otp.ToLower(), expectedOtp.ToLower()))
                {
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

    private string CalculateMotp(long epoch, string pin, byte[] secret)
    {
        var secretHex = Convert.ToHexString(secret).ToLower();
        var input = $"{epoch}{pin}{secretHex}";
        var hash = CryptoService.Md5(System.Text.Encoding.ASCII.GetBytes(input));
        return Convert.ToHexString(hash)[..6].ToLower();
    }
}

/// <summary>
/// Password Token implementation
/// Maps to Python: privacyidea/lib/tokens/passwordtoken.py
/// Static password authentication
/// </summary>
public class PasswordToken : TokenClassBase
{
    public override string Type => "pw";
    public override string DisplayName => "Password Token";

    public PasswordToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        // For password token, the entire input is treated as the password
        var password = string.IsNullOrEmpty(pin) ? otp : $"{pin}{otp}";
        
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
        if (TokenEntity == null) return Task.FromResult(false);

        // Password is stored hashed in PinHash
        var storedHash = TokenEntity.PinHash;
        if (string.IsNullOrEmpty(storedHash))
            return Task.FromResult(false);

        return Task.FromResult(CryptoService.VerifyPinHash(otp, storedHash));
    }

    public void SetPassword(string password)
    {
        if (TokenEntity != null)
        {
            TokenEntity.PinHash = CryptoService.HashPin(password);
        }
    }
}

/// <summary>
/// Registration Token implementation
/// Maps to Python: privacyidea/lib/tokens/registrationtoken.py
/// One-time registration code for user self-enrollment
/// </summary>
public class RegistrationToken : TokenClassBase
{
    public override string Type => "registration";
    public override string DisplayName => "Registration Token";

    public RegistrationToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        var code = string.IsNullOrEmpty(pin) ? otp : $"{pin}{otp}";
        
        if (string.IsNullOrEmpty(code))
            return new AuthenticationResult { Success = false, Message = "Registration code required" };

        var result = await CheckOtpAsync(code);
        
        if (result)
        {
            return new AuthenticationResult { Success = true, Message = "Registration successful" };
        }

        return new AuthenticationResult { Success = false, Message = "Invalid or expired registration code" };
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        if (TokenEntity == null) return Task.FromResult(false);

        // Registration code is stored in KeyEnc (not encrypted for simplicity)
        var storedCode = TokenEntity.KeyEnc;
        if (string.IsNullOrEmpty(storedCode))
            return Task.FromResult(false);

        if (!CryptoService.SecureCompare(otp.ToUpper(), storedCode.ToUpper()))
            return Task.FromResult(false);

        // Check if already used
        if (TokenEntity.Count > 0)
            return Task.FromResult(false);

        // Mark as used
        TokenEntity.Count = 1;
        TokenEntity.Active = false;

        return Task.FromResult(true);
    }

    public string GenerateRegistrationCode(int length = 12)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude ambiguous chars
        var bytes = CryptoService.GenerateRandomBytes(length);
        var code = new char[length];
        for (int i = 0; i < length; i++)
        {
            code[i] = chars[bytes[i] % chars.Length];
        }
        return new string(code);
    }
}

/// <summary>
/// Paper Token (TAN) implementation
/// Maps to Python: privacyidea/lib/tokens/papertoken.py
/// One-time codes from a printed list
/// </summary>
public class PaperToken : TokenClassBase
{
    public override string Type => "paper";
    public override string DisplayName => "Paper/TAN Token";
    public override bool SupportsChallengeResponse => true;

    public PaperToken(ICryptoService cryptoService) : base(cryptoService)
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
        if (TokenEntity == null) return Task.FromResult(false);

        // Get current TAN index
        var currentIndex = TokenEntity.Count;
        var tan = GetTokenInfoValue($"tan.{currentIndex}");

        if (string.IsNullOrEmpty(tan))
            return Task.FromResult(false);

        // TANs are stored hashed
        if (!CryptoService.VerifyPinHash(otp, tan))
            return Task.FromResult(false);

        // Mark TAN as used
        TokenEntity.Count = currentIndex + 1;
        return Task.FromResult(true);
    }

    public override Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        if (TokenEntity == null)
            return Task.FromResult(new ChallengeResult { Success = false });

        var nextIndex = TokenEntity.Count + 1;
        return Task.FromResult(new ChallengeResult
        {
            Success = true,
            Message = $"Please enter TAN #{nextIndex}",
            Attributes = new Dictionary<string, object>
            {
                ["tan_index"] = nextIndex
            }
        });
    }

    public List<string> GenerateTanList(int count = 100)
    {
        var tans = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var tan = GenerateTan();
            tans.Add(tan);
            SetTokenInfo($"tan.{i}", CryptoService.HashPin(tan));
        }
        SetTokenInfo("tan_count", count.ToString());
        return tans;
    }

    private string GenerateTan()
    {
        var bytes = CryptoService.GenerateRandomBytes(4);
        var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return number.ToString("D6");
    }
}

/// <summary>
/// Day Password Token implementation
/// Maps to Python: privacyidea/lib/tokens/daypasswordtoken.py
/// Password that changes daily
/// </summary>
public class DayPasswordToken : TokenClassBase
{
    public override string Type => "daypassword";
    public override string DisplayName => "Day Password Token";

    public DayPasswordToken(ICryptoService cryptoService) : base(cryptoService)
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
            return new AuthenticationResult { Success = false, Message = "Password required" };

        var result = await CheckOtpAsync(otp);
        
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
        var expectedOtp = CalculateDayPassword();
        return Task.FromResult(CryptoService.SecureCompare(otp, expectedOtp));
    }

    public override Task<string?> GetOtpAsync(long? timestamp = null)
    {
        return Task.FromResult<string?>(CalculateDayPassword(timestamp));
    }

    private string CalculateDayPassword(long? timestamp = null)
    {
        try
        {
            var secret = GetSecretKey();

            // Use date as counter
            var time = timestamp.HasValue 
                ? DateTimeOffset.FromUnixTimeSeconds(timestamp.Value).UtcDateTime 
                : DateTime.UtcNow;
            var daysSinceEpoch = (time - DateTime.UnixEpoch).Days;
            var counterBytes = BitConverter.GetBytes((long)daysSinceEpoch);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counterBytes);

            var hash = CryptoService.HmacSha1(secret, counterBytes);
            var otp = TruncateHash(hash);

            var otpLen = TokenEntity?.OtpLen ?? 6;
            return otp.ToString().PadLeft(otpLen, '0')[^otpLen..];
        }
        catch
        {
            return "";
        }
    }

    private static int TruncateHash(byte[] hash)
    {
        var offset = hash[^1] & 0x0f;
        var binary = (hash[offset] & 0x7f) << 24
                   | (hash[offset + 1] & 0xff) << 16
                   | (hash[offset + 2] & 0xff) << 8
                   | (hash[offset + 3] & 0xff);
        return binary % 1000000;
    }
}

/// <summary>
/// Questionnaire Token implementation
/// Maps to Python: privacyidea/lib/tokens/questionnairetoken.py
/// Challenge-response based on security questions
/// </summary>
public class QuestionnaireToken : TokenClassBase
{
    public override string Type => "question";
    public override string DisplayName => "Security Question Token";
    public override bool SupportsChallengeResponse => true;

    private string? _currentQuestion;
    private string? _currentAnswerHash;

    public QuestionnaireToken(ICryptoService cryptoService) : base(cryptoService)
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
            return new AuthenticationResult { Success = false, Message = "Answer required" };

        var result = await CheckOtpAsync(otp);
        
        if (result)
        {
            await ResetFailCounterAsync();
            return new AuthenticationResult { Success = true, Message = "Authentication successful" };
        }

        await IncrementFailCounterAsync();
        return new AuthenticationResult { Success = false, Message = "Incorrect answer" };
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        if (string.IsNullOrEmpty(_currentAnswerHash))
            return Task.FromResult(false);

        // Normalize answer: lowercase and trim
        var normalizedAnswer = otp.ToLower().Trim();
        return Task.FromResult(CryptoService.VerifyPinHash(normalizedAnswer, _currentAnswerHash));
    }

    public override Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        var questions = GetQuestions();
        if (!questions.Any())
        {
            return Task.FromResult(new ChallengeResult { Success = false, Message = "No questions configured" });
        }

        // Select a random question
        var random = new Random();
        var questionIndex = random.Next(questions.Count);
        var question = questions.ElementAt(questionIndex);

        _currentQuestion = question.Key;
        _currentAnswerHash = question.Value;

        return Task.FromResult(new ChallengeResult
        {
            Success = true,
            Challenge = _currentQuestion,
            Message = GetTokenInfoValue($"question.{_currentQuestion}") ?? _currentQuestion,
            Attributes = new Dictionary<string, object>
            {
                ["question"] = GetTokenInfoValue($"question.{_currentQuestion}") ?? _currentQuestion,
                ["question_id"] = _currentQuestion
            }
        });
    }

    public void AddQuestion(string questionId, string questionText, string answer)
    {
        SetTokenInfo($"question.{questionId}", questionText);
        SetTokenInfo($"answer.{questionId}", CryptoService.HashPin(answer.ToLower().Trim()));
    }

    private Dictionary<string, string> GetQuestions()
    {
        var questions = new Dictionary<string, string>();
        foreach (var info in TokenInfoCache)
        {
            if (info.Key.StartsWith("answer."))
            {
                var questionId = info.Key["answer.".Length..];
                questions[questionId] = info.Value?.ToString() ?? "";
            }
        }
        return questions;
    }
}

/// <summary>
/// Four Eyes Token implementation
/// Maps to Python: privacyidea/lib/tokens/foureyestoken.py
/// Requires multiple users to authenticate
/// </summary>
public class FourEyesToken : TokenClassBase
{
    public override string Type => "4eyes";
    public override string DisplayName => "Four Eyes Token";
    public override bool SupportsChallengeResponse => true;

    public FourEyesToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        // Four eyes authentication requires multiple authenticated users
        // This is handled at a higher level (ValidationService)
        return Task.FromResult(new AuthenticationResult 
        { 
            Success = false, 
            Message = "Four Eyes token requires challenge-response authentication" 
        });
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        return Task.FromResult(false);
    }

    public override Task<ChallengeResult> CreateChallengeAsync(string? transactionId = null, string? data = null)
    {
        var requiredApprovals = GetRequiredApprovals();
        var realm = GetRealmRequirement();

        return Task.FromResult(new ChallengeResult
        {
            Success = true,
            Challenge = transactionId ?? Guid.NewGuid().ToString(),
            Message = $"Requires {requiredApprovals} approvals",
            Attributes = new Dictionary<string, object>
            {
                ["required_approvals"] = requiredApprovals,
                ["realm"] = realm ?? "",
                ["mode"] = "4eyes"
            }
        });
    }

    public int GetRequiredApprovals()
    {
        var count = GetTokenInfoValue("4eyes.count");
        return int.TryParse(count, out var c) ? c : 2;
    }

    public string? GetRealmRequirement()
    {
        return GetTokenInfoValue("4eyes.realm");
    }
}

/// <summary>
/// RADIUS Token implementation
/// Maps to Python: privacyidea/lib/tokens/radiustoken.py
/// Forwards authentication to a RADIUS server
/// </summary>
public class RadiusToken : TokenClassBase
{
    public override string Type => "radius";
    public override string DisplayName => "RADIUS Token";
    public override bool SupportsChallengeResponse => true;

    public RadiusToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override async Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        var password = string.IsNullOrEmpty(pin) ? otp : $"{pin}{otp}";
        
        if (string.IsNullOrEmpty(password))
            return new AuthenticationResult { Success = false, Message = "Password required" };

        var result = await CheckOtpAsync(password);
        
        if (result)
        {
            return new AuthenticationResult { Success = true, Message = "Authentication successful" };
        }

        return new AuthenticationResult { Success = false, Message = "RADIUS authentication failed" };
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        // RADIUS authentication would be implemented here
        // Using a RADIUS client library to forward the authentication
        var radiusServer = GetTokenInfoValue("radius.server");
        var radiusSecret = GetTokenInfoValue("radius.secret");
        var radiusUser = GetTokenInfoValue("radius.user") ?? TokenEntity?.TokenOwners?.FirstOrDefault()?.UserId;

        if (string.IsNullOrEmpty(radiusServer) || string.IsNullOrEmpty(radiusSecret))
            return Task.FromResult(false);

        // TODO: Implement actual RADIUS authentication
        // Would use a RADIUS client library here
        return Task.FromResult(false);
    }
}

/// <summary>
/// Certificate Token implementation
/// Maps to Python: privacyidea/lib/tokens/certificatetoken.py
/// Authentication via X.509 certificate
/// </summary>
public class CertificateToken : TokenClassBase
{
    public override string Type => "certificate";
    public override string DisplayName => "Certificate Token";

    public CertificateToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        // Certificate token authentication is done via client certificate
        // This is handled at the transport layer
        return Task.FromResult(new AuthenticationResult 
        { 
            Success = false, 
            Message = "Certificate authentication requires client certificate" 
        });
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        // Certificate token doesn't use OTP
        return Task.FromResult(false);
    }

    public async Task<AuthenticationResult> AuthenticateWithCertificateAsync(byte[] certificateData)
    {
        if (TokenEntity == null)
            return new AuthenticationResult { Success = false, Message = "Token not initialized" };

        try
        {
            var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certificateData);
            var storedFingerprint = GetTokenInfoValue("certificate.fingerprint");
            var actualFingerprint = cert.GetCertHashString();

            if (!CryptoService.SecureCompare(storedFingerprint ?? "", actualFingerprint))
                return new AuthenticationResult { Success = false, Message = "Certificate fingerprint mismatch" };

            // Check certificate validity
            if (DateTime.UtcNow < cert.NotBefore || DateTime.UtcNow > cert.NotAfter)
                return new AuthenticationResult { Success = false, Message = "Certificate expired or not yet valid" };

            return await Task.FromResult(new AuthenticationResult { Success = true, Message = "Certificate authentication successful" });
        }
        catch (Exception ex)
        {
            return new AuthenticationResult { Success = false, Message = ex.Message };
        }
    }

    public void SetCertificate(byte[] certificateData)
    {
        var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certificateData);
        SetTokenInfo("certificate.fingerprint", cert.GetCertHashString());
        SetTokenInfo("certificate.subject", cert.Subject);
        SetTokenInfo("certificate.issuer", cert.Issuer);
        SetTokenInfo("certificate.notBefore", cert.NotBefore.ToString("O"));
        SetTokenInfo("certificate.notAfter", cert.NotAfter.ToString("O"));
    }
}

/// <summary>
/// SSH Key Token implementation
/// Maps to Python: privacyidea/lib/tokens/sshkeytoken.py
/// Stores SSH public keys for authentication
/// </summary>
public class SshKeyToken : TokenClassBase
{
    public override string Type => "sshkey";
    public override string DisplayName => "SSH Key Token";

    public SshKeyToken(ICryptoService cryptoService) : base(cryptoService)
    {
    }

    public override Task<AuthenticationResult> AuthenticateAsync(string? pin, string? otp)
    {
        // SSH key token authentication is done via SSH protocol
        return Task.FromResult(new AuthenticationResult 
        { 
            Success = false, 
            Message = "SSH key authentication not supported via API" 
        });
    }

    public override Task<bool> CheckOtpAsync(string otp, int? counter = null, int? window = null)
    {
        // SSH key token doesn't use OTP
        return Task.FromResult(false);
    }

    public string? GetPublicKey()
    {
        return TokenEntity?.KeyEnc; // SSH token stores public key in KeyEnc
    }

    public void SetPublicKey(string publicKey)
    {
        if (TokenEntity != null)
        {
            TokenEntity.KeyEnc = publicKey;
            
            // Parse and store key info
            var parts = publicKey.Split(' ');
            if (parts.Length >= 2)
            {
                SetTokenInfo("sshkey.type", parts[0]);
                if (parts.Length >= 3)
                {
                    SetTokenInfo("sshkey.comment", parts[2]);
                }
            }
        }
    }
}
