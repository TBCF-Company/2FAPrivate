namespace PrivacyIDEA.Domain.Enums;

/// <summary>
/// Supported token types in PrivacyIDEA
/// </summary>
public enum TokenType
{
    Hotp,
    Totp,
    Push,
    Passkey,
    WebAuthn,
    Sms,
    Email,
    Motp,
    Radius,
    Certificate,
    SshKey,
    TiQR,
    Paper,
    Tan,
    Yubico,
    YubiKey,
    U2F,
    Ocra,
    Password,
    Questionnaire,
    Registration,
    Remote,
    FourEyes,
    DayPassword,
    IndexedSecret,
    Daplug,
    Vasco,
    Spass,
    ApplicationSpecificPassword
}
