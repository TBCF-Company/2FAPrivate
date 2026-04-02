using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

using AuthTokenValidationResult = PrivacyIDEA.Core.Interfaces.TokenValidationResult;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// Authentication Service implementation
/// Maps to Python: privacyidea/lib/auth.py
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICryptoService _cryptoService;
    private readonly ITokenService _tokenService;
    private readonly IPolicyService _policyService;
    private readonly IConfiguration _configuration;

    private readonly string _jwtSecret;
    private readonly int _tokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public AuthService(
        IUnitOfWork unitOfWork,
        ICryptoService cryptoService,
        ITokenService tokenService,
        IPolicyService policyService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _cryptoService = cryptoService;
        _tokenService = tokenService;
        _policyService = policyService;
        _configuration = configuration;

        // Get JWT settings from configuration
        _jwtSecret = _configuration["Jwt:Secret"] ?? GenerateDefaultSecret();
        _tokenExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
        _refreshTokenExpirationDays = int.Parse(_configuration["Jwt:RefreshExpirationDays"] ?? "7");
    }

    public async Task<AuthResult> AuthenticateAdminAsync(string username, string password)
    {
        var admin = await GetAdminAsync(username);
        
        if (admin == null)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };
        }

        if (!admin.Active)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Account is disabled"
            };
        }

        if (!VerifyPassword(password, admin.Password))
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };
        }

        // Generate JWT token
        var token = GenerateJwtToken(username, "admin", null);
        var refreshToken = GenerateRefreshToken();

        return new AuthResult
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes),
            Username = username,
            Role = "admin",
            Rights = new List<string> { "admin", "manage_tokens", "manage_users", "manage_policies", "manage_resolvers" }
        };
    }

    public async Task<AuthResult> AuthenticateUserAsync(string username, string? realm, string? password, string? otp)
    {
        // First, check if user exists
        // This would typically involve resolver lookup
        
        // If OTP is provided, validate it
        if (!string.IsNullOrEmpty(otp))
        {
            // Get user's tokens
            var tokens = await _tokenService.GetTokensForUserAsync(username, realm);
            
            foreach (var token in tokens.Where(t => t.Active))
            {
                var tokenClass = _tokenService.GetTokenClassForType(token.TokenType);
                if (tokenClass.CheckOtp(otp, token))
                {
                    // OTP valid
                    var jwtToken = GenerateJwtToken(username, "user", realm);
                    var refreshToken = GenerateRefreshToken();

                    return new AuthResult
                    {
                        Success = true,
                        Token = jwtToken,
                        RefreshToken = refreshToken,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes),
                        Username = username,
                        Realm = realm,
                        Role = "user"
                    };
                }
            }

            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Invalid OTP"
            };
        }

        // If no OTP, might require second factor
        var userTokens = await _tokenService.GetTokensForUserAsync(username, realm);
        if (userTokens.Any(t => t.Active))
        {
            // User has tokens, require OTP
            var transactionId = Guid.NewGuid().ToString("N");
            
            return new AuthResult
            {
                Success = false,
                RequiresSecondFactor = true,
                TransactionId = transactionId,
                ErrorMessage = "Second factor required"
            };
        }

        // No tokens assigned, authenticate with password only
        var token2 = GenerateJwtToken(username, "user", realm);
        return new AuthResult
        {
            Success = true,
            Token = token2,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes),
            Username = username,
            Realm = realm,
            Role = "user"
        };
    }

    public async Task<AuthTokenValidationResult> ValidateAuthTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            var username = principal.FindFirst(ClaimTypes.Name)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            var realm = principal.FindFirst("realm")?.Value;

            return new AuthTokenValidationResult
            {
                IsValid = true,
                Username = username,
                Role = role,
                Realm = realm,
                ExpiresAt = jwtToken.ValidTo
            };
        }
        catch (Exception ex)
        {
            return new AuthTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        // In a real implementation, validate refresh token from database
        // For now, generate a new token
        // This is a simplified implementation
        
        return new AuthResult
        {
            Success = false,
            ErrorMessage = "Refresh token validation not implemented"
        };
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        // In a real implementation, add token to blacklist
        // For now, return true
        return true;
    }

    public async Task<UserRights> GetUserRightsAsync(string username, string? realm = null)
    {
        var admin = await GetAdminAsync(username);
        
        if (admin != null && admin.Active)
        {
            return new UserRights
            {
                Username = username,
                Role = "admin",
                IsAdmin = true,
                Permissions = new List<string>
                {
                    "admin", "manage_tokens", "manage_users", "manage_policies", 
                    "manage_resolvers", "manage_realms", "view_audit"
                }
            };
        }

        // Get user policies
        var policies = await _policyService.GetPoliciesAsync(scope: "user", realm: realm, user: username);
        var permissions = new List<string>();

        foreach (var policy in policies.Where(p => p.Active))
        {
            // Parse policy actions to permissions
            var actions = policy.Action.Split(',').Select(a => a.Trim());
            permissions.AddRange(actions);
        }

        return new UserRights
        {
            Username = username,
            Realm = realm,
            Role = "user",
            IsAdmin = false,
            Permissions = permissions.Distinct().ToList()
        };
    }

    public async Task<bool> HasPermissionAsync(string username, string permission, string? realm = null)
    {
        var rights = await GetUserRightsAsync(username, realm);
        return rights.IsAdmin || rights.Permissions.Contains(permission);
    }

    public async Task<Admin?> GetAdminAsync(string username)
    {
        var admins = await _unitOfWork.Repository<Admin>().GetAllAsync();
        return admins.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Admin> CreateAdminAsync(string username, string password, string? email = null)
    {
        var existing = await GetAdminAsync(username);
        if (existing != null)
        {
            throw new InvalidOperationException($"Admin '{username}' already exists");
        }

        var admin = new Admin
        {
            Username = username,
            Password = HashPassword(password),
            Email = email,
            Active = true
        };

        await _unitOfWork.Repository<Admin>().AddAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        return admin;
    }

    public async Task<bool> UpdateAdminPasswordAsync(string username, string newPassword)
    {
        var admin = await GetAdminAsync(username);
        if (admin == null)
        {
            return false;
        }

        admin.Password = HashPassword(newPassword);
        _unitOfWork.Repository<Admin>().Update(admin);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAdminAsync(string username)
    {
        var admin = await GetAdminAsync(username);
        if (admin == null)
        {
            return false;
        }

        _unitOfWork.Repository<Admin>().Remove(admin);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<Admin>> ListAdminsAsync()
    {
        return await _unitOfWork.Repository<Admin>().GetAllAsync();
    }

    public bool VerifyPassword(string password, string hash)
    {
        // Support multiple hash formats
        if (hash.StartsWith("$argon2"))
        {
            return _cryptoService.VerifyArgon2(password, hash);
        }
        else if (hash.StartsWith("$pbkdf2"))
        {
            return VerifyPbkdf2Password(password, hash);
        }
        else if (hash.StartsWith("{SSHA}") || hash.StartsWith("{SHA}"))
        {
            return VerifyLdapPassword(password, hash);
        }
        else
        {
            // BCrypt or plain comparison (not recommended)
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }

    public string HashPassword(string password)
    {
        // Use Argon2id by default (most secure)
        return _cryptoService.HashArgon2(password);
    }

    private string GenerateJwtToken(string username, string role, string? realm)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        if (!string.IsNullOrEmpty(realm))
        {
            claims.Add(new Claim("realm", realm));
        }

        var token = new JwtSecurityToken(
            issuer: "PrivacyIDEA",
            audience: "PrivacyIDEA",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private string GenerateDefaultSecret()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private bool VerifyPbkdf2Password(string password, string hash)
    {
        // Parse PBKDF2 hash format: $pbkdf2-sha256$iterations$salt$hash
        var parts = hash.Split('$');
        if (parts.Length < 5) return false;

        var algorithm = parts[1]; // pbkdf2-sha256
        var iterations = int.Parse(parts[2]);
        var salt = Convert.FromBase64String(parts[3]);
        var storedHash = Convert.FromBase64String(parts[4]);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var computedHash = pbkdf2.GetBytes(storedHash.Length);

        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }

    private bool VerifyLdapPassword(string password, string hash)
    {
        if (hash.StartsWith("{SSHA}"))
        {
            // Salted SHA-1
            var data = Convert.FromBase64String(hash.Substring(6));
            var sha1Hash = data[..20];
            var salt = data[20..];

            using var sha1 = SHA1.Create();
            var computed = sha1.ComputeHash(Encoding.UTF8.GetBytes(password).Concat(salt).ToArray());
            return CryptographicOperations.FixedTimeEquals(sha1Hash, computed);
        }
        else if (hash.StartsWith("{SHA}"))
        {
            // Plain SHA-1
            var storedHash = Convert.FromBase64String(hash.Substring(5));
            using var sha1 = SHA1.Create();
            var computed = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
            return CryptographicOperations.FixedTimeEquals(storedHash, computed);
        }

        return false;
    }
}
