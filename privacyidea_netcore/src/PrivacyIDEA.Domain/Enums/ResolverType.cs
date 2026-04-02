namespace PrivacyIDEA.Domain.Enums;

/// <summary>
/// Types of user resolvers
/// </summary>
public enum ResolverType
{
    Ldap,
    Sql,
    Passwd,
    Scim,
    Http,
    EntraId,
    Keycloak
}
