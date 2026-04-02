namespace PrivacyIDEA.Domain.Enums;

/// <summary>
/// Policy scopes defining where policies apply
/// </summary>
public enum PolicyScope
{
    Admin,
    User,
    Authentication,
    Authorization,
    Enrollment,
    WebUI,
    Audit,
    Register
}
