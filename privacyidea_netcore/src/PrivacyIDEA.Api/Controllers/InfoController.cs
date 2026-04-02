using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// System information API
/// </summary>
[ApiController]
[Route("[controller]")]
public class InfoController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public InfoController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Get system information
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

        return Ok(new
        {
            result = new
            {
                value = new
                {
                    version,
                    product = "PrivacyIDEA.NET",
                    framework = ".NET 8",
                    database = _configuration.GetValue<string>("Database:Provider") ?? "SQLite",
                    hostname = Environment.MachineName,
                    os = Environment.OSVersion.ToString()
                }
            },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get version information
    /// </summary>
    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

        return Ok(new
        {
            result = new
            {
                value = new
                {
                    version,
                    privacyidea_version = "4.0.0-netcore",
                    api_version = "v1"
                }
            },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get installed plugins/extensions
    /// </summary>
    [HttpGet("plugins")]
    public IActionResult GetPlugins()
    {
        // List available token types, resolvers, SMS providers as "plugins"
        var plugins = new
        {
            token_types = new[]
            {
                "hotp", "totp", "sms", "email", "push",
                "certificate", "sshkey", "password",
                "registration", "paper", "tan",
                "radius", "remote", "daypassword",
                "questionnaire", "4eyes", "indexedsecret",
                "webauthn", "passkey", "u2f", "fido2",
                "yubico", "yubikey", "ocra", "motp"
            },
            resolvers = new[]
            {
                "ldap", "sql", "entraid", "scim", "http", "passwd", "file"
            },
            sms_providers = new[]
            {
                "http", "smpp", "twilio", "aws_sns", "sipgate", "firebase"
            },
            event_handlers = new[]
            {
                "notification", "token", "webhook", "counter", "script", "logging"
            }
        };

        return Ok(new
        {
            result = new { value = plugins },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get configuration overview (non-sensitive)
    /// </summary>
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        return Ok(new
        {
            result = new
            {
                value = new
                {
                    default_realm = _configuration.GetValue<string>("PrivacyIDEA:DefaultRealm") ?? "default",
                    prepend_pin = _configuration.GetValue<bool>("PrivacyIDEA:PrependPin"),
                    auto_resync = _configuration.GetValue<bool>("PrivacyIDEA:AutoResync"),
                    fail_counter_inc_on_false_pin = _configuration.GetValue<bool>("PrivacyIDEA:FailCounterIncOnFalsePin")
                }
            },
            version = "1.0",
            id = 1
        });
    }
}
