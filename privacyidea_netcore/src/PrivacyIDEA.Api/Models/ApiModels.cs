namespace PrivacyIDEA.Api.Models;

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public string JsonRpc { get; set; } = "2.0";
    public ResultWrapper<T> Result { get; set; } = new();
    public string? Version { get; set; }
    public string? VersionNumber { get; set; }
    public string? Signature { get; set; }
    public int Id { get; set; } = 1;
    public double? Time { get; set; }
}

public class ResultWrapper<T>
{
    public bool Status { get; set; }
    public T? Value { get; set; }
    public ErrorInfo? Error { get; set; }
}

public class ErrorInfo
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Validate check request
/// </summary>
public class ValidateCheckRequest
{
    public string? User { get; set; }
    public string? Realm { get; set; }
    public string? Pass { get; set; }
    public string? Serial { get; set; }
    public string? TransactionId { get; set; }
    public string? OtpOnly { get; set; }
}

/// <summary>
/// Validate check response
/// </summary>
public class ValidateCheckResponse
{
    public bool Authentication { get; set; }
    public Dictionary<string, object>? Detail { get; set; }
}

/// <summary>
/// Token list response
/// </summary>
public class TokenListResponse
{
    public int Count { get; set; }
    public int Current { get; set; }
    public IEnumerable<TokenDto> Tokens { get; set; } = Enumerable.Empty<TokenDto>();
}

/// <summary>
/// Token DTO
/// </summary>
public class TokenDto
{
    public int Id { get; set; }
    public string Serial { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; }
    public bool Revoked { get; set; }
    public bool Locked { get; set; }
    public int FailCount { get; set; }
    public int MaxFail { get; set; }
    public int OtpLen { get; set; }
    public int Count { get; set; }
    public string? RolloutState { get; set; }
    public Dictionary<string, string>? Info { get; set; }
    public IEnumerable<TokenOwnerDto>? Owners { get; set; }
    public IEnumerable<string>? Realms { get; set; }
}

/// <summary>
/// Token owner DTO
/// </summary>
public class TokenOwnerDto
{
    public string? UserId { get; set; }
    public string? Resolver { get; set; }
    public string? Realm { get; set; }
}

/// <summary>
/// Token init/create request
/// </summary>
public class TokenInitRequest
{
    public string Type { get; set; } = "hotp";
    public string? Serial { get; set; }
    public string? User { get; set; }
    public string? Realm { get; set; }
    public string? Description { get; set; }
    public string? Pin { get; set; }
    public string? OtpKey { get; set; }
    public int? OtpLen { get; set; }
    public string? GenKey { get; set; }
    public int? HashLib { get; set; }
    public int? TimeStep { get; set; }
    public Dictionary<string, string>? Info { get; set; }
}

/// <summary>
/// User DTO
/// </summary>
public class UserDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Description { get; set; }
    public string? Resolver { get; set; }
    public string? Realm { get; set; }
}

/// <summary>
/// User list response
/// </summary>
public class UserListResponse
{
    public int Count { get; set; }
    public IEnumerable<UserDto> Users { get; set; } = Enumerable.Empty<UserDto>();
}

/// <summary>
/// Realm DTO
/// </summary>
public class RealmDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public IEnumerable<ResolverInRealmDto>? Resolvers { get; set; }
}

/// <summary>
/// Resolver in realm DTO
/// </summary>
public class ResolverInRealmDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? Priority { get; set; }
}

/// <summary>
/// Policy DTO
/// </summary>
public class PolicyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string? Action { get; set; }
    public string? Realm { get; set; }
    public string? User { get; set; }
    public string? Client { get; set; }
    public string? Time { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// Auth token request
/// </summary>
public class AuthTokenRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Auth token response
/// </summary>
public class AuthTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Realm { get; set; }
    public string? Rights { get; set; }
    public DateTime ExpiresAt { get; set; }
}
