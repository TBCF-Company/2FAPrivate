using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MimeKit;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// SMTP service implementation
/// Maps to Python: privacyidea/lib/smtpserver.py
/// </summary>
public class SmtpService : ISmtpService
{
    private readonly ILogger<SmtpService> _logger;
    private readonly Dictionary<string, SmtpServerConfig> _servers = new();

    public SmtpService(ILogger<SmtpService> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<SmtpServerInfo>> GetAllServersAsync()
    {
        var result = _servers.Values.Select((s, i) => new SmtpServerInfo
        {
            Id = i + 1,
            Identifier = s.Identifier,
            Server = s.Server,
            Port = s.Port,
            Username = s.Username,
            Sender = s.Sender,
            Tls = s.Tls,
            Timeout = s.Timeout,
            Description = s.Description
        });
        
        return Task.FromResult(result);
    }

    public Task<SmtpServerInfo?> GetServerAsync(string identifier)
    {
        if (_servers.TryGetValue(identifier, out var config))
        {
            return Task.FromResult<SmtpServerInfo?>(new SmtpServerInfo
            {
                Id = 1,
                Identifier = config.Identifier,
                Server = config.Server,
                Port = config.Port,
                Username = config.Username,
                Sender = config.Sender,
                Tls = config.Tls,
                Timeout = config.Timeout,
                Description = config.Description
            });
        }
        
        return Task.FromResult<SmtpServerInfo?>(null);
    }

    public Task<int> CreateServerAsync(SmtpServerConfig config)
    {
        _servers[config.Identifier] = config;
        _logger.LogInformation("Created SMTP server: {Identifier}", config.Identifier);
        return Task.FromResult(_servers.Count);
    }

    public Task<bool> DeleteServerAsync(string identifier)
    {
        var result = _servers.Remove(identifier);
        if (result)
        {
            _logger.LogInformation("Deleted SMTP server: {Identifier}", identifier);
        }
        return Task.FromResult(result);
    }

    public async Task<SmtpTestResult> TestConnectionAsync(string identifier, string recipient)
    {
        try
        {
            if (!_servers.TryGetValue(identifier, out var config))
            {
                return new SmtpTestResult { Success = false, Message = "Server not found" };
            }

            using var client = new SmtpClient();
            
            await client.ConnectAsync(config.Server, config.Port, config.Tls ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto);
            
            if (!string.IsNullOrEmpty(config.Username))
            {
                await client.AuthenticateAsync(config.Username, config.Password);
            }

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(config.Sender));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = "PrivacyIDEA SMTP Test";
            message.Body = new TextPart("plain")
            {
                Text = "This is a test email from PrivacyIDEA."
            };

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return new SmtpTestResult { Success = true, Message = "Test email sent successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP test failed for {Identifier}", identifier);
            return new SmtpTestResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<bool> SendEmailAsync(string identifier, string recipient, string subject, string body, bool isHtml = false)
    {
        try
        {
            if (!_servers.TryGetValue(identifier, out var config))
            {
                _logger.LogError("SMTP server not found: {Identifier}", identifier);
                return false;
            }

            using var client = new SmtpClient();
            
            await client.ConnectAsync(config.Server, config.Port, config.Tls ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto);
            
            if (!string.IsNullOrEmpty(config.Username))
            {
                await client.AuthenticateAsync(config.Username, config.Password);
            }

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(config.Sender));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = new TextPart(isHtml ? "html" : "plain")
            {
                Text = body
            };

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Recipient} via {Server}", recipient, identifier);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient} via {Server}", recipient, identifier);
            return false;
        }
    }
}
