namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// SMTP Server service interface
/// Maps to Python: privacyidea/lib/smtpserver.py
/// </summary>
public interface ISmtpService
{
    Task<IEnumerable<SmtpServerInfo>> GetAllServersAsync();
    Task<SmtpServerInfo?> GetServerAsync(string identifier);
    Task<int> CreateServerAsync(SmtpServerConfig config);
    Task<bool> DeleteServerAsync(string identifier);
    Task<SmtpTestResult> TestConnectionAsync(string identifier, string recipient);
    Task<bool> SendEmailAsync(string identifier, string recipient, string subject, string body, bool isHtml = false);
}

/// <summary>
/// SMTP server configuration
/// </summary>
public class SmtpServerConfig
{
    public string Identifier { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string Sender { get; set; } = string.Empty;
    public bool Tls { get; set; } = false;
    public int Timeout { get; set; } = 10;
    public string? Description { get; set; }
}

/// <summary>
/// SMTP server info (for listing)
/// </summary>
public class SmtpServerInfo
{
    public int Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string Sender { get; set; } = string.Empty;
    public bool Tls { get; set; }
    public int Timeout { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// SMTP test result
/// </summary>
public class SmtpTestResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
