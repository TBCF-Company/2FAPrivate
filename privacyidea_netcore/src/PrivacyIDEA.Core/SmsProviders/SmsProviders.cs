using Microsoft.Extensions.Logging;

namespace PrivacyIDEA.Core.SmsProviders;

/// <summary>
/// Interface for SMS provider implementations
/// Maps to Python: privacyidea/lib/smsprovider/SMSProvider.py
/// </summary>
public interface ISmsProvider
{
    /// <summary>
    /// Provider type name
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Display name for UI
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Initialize the provider with configuration
    /// </summary>
    void Initialize(Dictionary<string, string> config);

    /// <summary>
    /// Send an SMS message
    /// </summary>
    Task<SmsResult> SendSmsAsync(string phoneNumber, string message);

    /// <summary>
    /// Test the provider configuration
    /// </summary>
    Task<SmsResult> TestAsync(string testNumber);
}

/// <summary>
/// Result of SMS operation
/// </summary>
public class SmsResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? MessageId { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Base class for SMS providers
/// </summary>
public abstract class SmsProviderBase : ISmsProvider
{
    protected readonly ILogger Logger;
    protected Dictionary<string, string> Config = new();

    protected SmsProviderBase(ILogger logger)
    {
        Logger = logger;
    }

    public abstract string Type { get; }
    public abstract string DisplayName { get; }

    public virtual void Initialize(Dictionary<string, string> config)
    {
        Config = config;
    }

    public abstract Task<SmsResult> SendSmsAsync(string phoneNumber, string message);

    public virtual async Task<SmsResult> TestAsync(string testNumber)
    {
        return await SendSmsAsync(testNumber, "Test message from PrivacyIDEA");
    }

    protected string GetConfig(string key, string defaultValue = "")
    {
        return Config.TryGetValue(key, out var value) ? value : defaultValue;
    }

    protected int GetConfigInt(string key, int defaultValue = 0)
    {
        return Config.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : defaultValue;
    }

    protected bool GetConfigBool(string key, bool defaultValue = false)
    {
        return Config.TryGetValue(key, out var value) && bool.TryParse(value, out var result) ? result : defaultValue;
    }
}

/// <summary>
/// HTTP SMS Provider - sends SMS via HTTP API
/// Maps to Python: privacyidea/lib/smsprovider/HttpSMSProvider.py
/// </summary>
public class HttpSmsProvider : SmsProviderBase
{
    private readonly HttpClient _httpClient;

    public override string Type => "http";
    public override string DisplayName => "HTTP SMS Gateway";

    private string _url = "";
    private string _method = "POST";
    private string _phoneParameter = "phone";
    private string _messageParameter = "message";
    private Dictionary<string, string> _headers = new();
    private Dictionary<string, string> _staticParams = new();
    private string _successRegex = "";

    public HttpSmsProvider(ILogger<HttpSmsProvider> logger, HttpClient? httpClient = null) : base(logger)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public override void Initialize(Dictionary<string, string> config)
    {
        base.Initialize(config);

        _url = GetConfig("URL");
        _method = GetConfig("HTTP_Method", "POST");
        _phoneParameter = GetConfig("phone_parameter", "phone");
        _messageParameter = GetConfig("message_parameter", "message");
        _successRegex = GetConfig("REGEXP", "");

        // Parse headers
        var headersJson = GetConfig("HTTP_Headers", "{}");
        try
        {
            _headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson)
                       ?? new Dictionary<string, string>();
        }
        catch
        {
            _headers = new Dictionary<string, string>();
        }

        // Parse static parameters
        foreach (var key in Config.Keys.Where(k => k.StartsWith("static_")))
        {
            _staticParams[key.Replace("static_", "")] = Config[key];
        }
    }

    public override async Task<SmsResult> SendSmsAsync(string phoneNumber, string message)
    {
        Logger.LogInformation("Sending SMS via HTTP to {Phone}", phoneNumber);

        try
        {
            var parameters = new Dictionary<string, string>(_staticParams)
            {
                [_phoneParameter] = phoneNumber,
                [_messageParameter] = message
            };

            HttpResponseMessage response;

            if (_method.ToUpper() == "GET")
            {
                var queryString = string.Join("&", parameters.Select(p => 
                    $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
                var url = _url.Contains('?') ? $"{_url}&{queryString}" : $"{_url}?{queryString}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddHeaders(request);
                response = await _httpClient.SendAsync(request);
            }
            else
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _url);
                AddHeaders(request);
                request.Content = new FormUrlEncodedContent(parameters);
                response = await _httpClient.SendAsync(request);
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            var success = response.IsSuccessStatusCode;
            if (success && !string.IsNullOrEmpty(_successRegex))
            {
                success = System.Text.RegularExpressions.Regex.IsMatch(responseBody, _successRegex);
            }

            return new SmsResult
            {
                Success = success,
                Message = success ? "SMS sent successfully" : $"Failed: {responseBody}",
                Details = new Dictionary<string, object>
                {
                    ["status_code"] = (int)response.StatusCode,
                    ["response_body"] = responseBody
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "HTTP SMS failed");
            return new SmsResult { Success = false, Message = ex.Message };
        }
    }

    private void AddHeaders(HttpRequestMessage request)
    {
        foreach (var (key, value) in _headers)
        {
            request.Headers.TryAddWithoutValidation(key, value);
        }
    }
}

/// <summary>
/// SMPP SMS Provider - sends SMS via SMPP protocol
/// Maps to Python: privacyidea/lib/smsprovider/SmppSMSProvider.py
/// </summary>
public class SmppSmsProvider : SmsProviderBase
{
    public override string Type => "smpp";
    public override string DisplayName => "SMPP SMS Gateway";

    private string _host = "";
    private int _port = 2775;
    private string _systemId = "";
    private string _password = "";
    private string _systemType = "";
    private string _sourceAddr = "";
    private byte _sourceAddrTon = 1;
    private byte _sourceAddrNpi = 1;
    private byte _destAddrTon = 1;
    private byte _destAddrNpi = 1;

    public SmppSmsProvider(ILogger<SmppSmsProvider> logger) : base(logger)
    {
    }

    public override void Initialize(Dictionary<string, string> config)
    {
        base.Initialize(config);

        _host = GetConfig("SMSC_HOST");
        _port = GetConfigInt("SMSC_PORT", 2775);
        _systemId = GetConfig("SYSTEM_ID");
        _password = GetConfig("PASSWORD");
        _systemType = GetConfig("SYSTEM_TYPE", "");
        _sourceAddr = GetConfig("SOURCE_ADDR");
        _sourceAddrTon = (byte)GetConfigInt("SOURCE_TON", 1);
        _sourceAddrNpi = (byte)GetConfigInt("SOURCE_NPI", 1);
        _destAddrTon = (byte)GetConfigInt("DEST_TON", 1);
        _destAddrNpi = (byte)GetConfigInt("DEST_NPI", 1);
    }

    public override Task<SmsResult> SendSmsAsync(string phoneNumber, string message)
    {
        Logger.LogInformation("Sending SMS via SMPP to {Phone}", phoneNumber);

        // SMPP implementation would require JamaaTech.SMPP.Net library
        // This is a placeholder for the actual implementation
        try
        {
            // Actual implementation would:
            // 1. Connect to SMSC
            // 2. Bind as transmitter
            // 3. Submit message
            // 4. Get message ID
            // 5. Unbind and disconnect

            Logger.LogWarning("SMPP provider requires JamaaTech.SMPP.Net library implementation");
            
            return Task.FromResult(new SmsResult
            {
                Success = false,
                Message = "SMPP provider not fully implemented"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SMPP SMS failed");
            return Task.FromResult(new SmsResult { Success = false, Message = ex.Message });
        }
    }
}

/// <summary>
/// Twilio SMS Provider
/// </summary>
public class TwilioSmsProvider : SmsProviderBase
{
    private readonly HttpClient _httpClient;

    public override string Type => "twilio";
    public override string DisplayName => "Twilio SMS";

    private string _accountSid = "";
    private string _authToken = "";
    private string _fromNumber = "";

    public TwilioSmsProvider(ILogger<TwilioSmsProvider> logger, HttpClient? httpClient = null) : base(logger)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public override void Initialize(Dictionary<string, string> config)
    {
        base.Initialize(config);

        _accountSid = GetConfig("ACCOUNT_SID");
        _authToken = GetConfig("AUTH_TOKEN");
        _fromNumber = GetConfig("FROM_NUMBER");
    }

    public override async Task<SmsResult> SendSmsAsync(string phoneNumber, string message)
    {
        Logger.LogInformation("Sending SMS via Twilio to {Phone}", phoneNumber);

        try
        {
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json";
            
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = phoneNumber,
                ["From"] = _fromNumber,
                ["Body"] = message
            });

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var authBytes = System.Text.Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(authBytes));
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = System.Text.Json.JsonDocument.Parse(responseBody);
                var sid = json.RootElement.GetProperty("sid").GetString();
                
                return new SmsResult
                {
                    Success = true,
                    Message = "SMS sent successfully",
                    MessageId = sid
                };
            }

            return new SmsResult
            {
                Success = false,
                Message = $"Twilio error: {responseBody}"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Twilio SMS failed");
            return new SmsResult { Success = false, Message = ex.Message };
        }
    }
}

/// <summary>
/// AWS SNS SMS Provider
/// </summary>
public class AwsSnsProvider : SmsProviderBase
{
    private readonly HttpClient _httpClient;

    public override string Type => "sns";
    public override string DisplayName => "AWS SNS";

    private string _accessKey = "";
    private string _secretKey = "";
    private string _region = "us-east-1";

    public AwsSnsProvider(ILogger<AwsSnsProvider> logger, HttpClient? httpClient = null) : base(logger)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public override void Initialize(Dictionary<string, string> config)
    {
        base.Initialize(config);

        _accessKey = GetConfig("AWS_ACCESS_KEY");
        _secretKey = GetConfig("AWS_SECRET_KEY");
        _region = GetConfig("AWS_REGION", "us-east-1");
    }

    public override async Task<SmsResult> SendSmsAsync(string phoneNumber, string message)
    {
        Logger.LogInformation("Sending SMS via AWS SNS to {Phone}", phoneNumber);

        // Simplified implementation - production would use AWS SDK
        try
        {
            var endpoint = $"https://sns.{_region}.amazonaws.com/";
            
            var parameters = new Dictionary<string, string>
            {
                ["Action"] = "Publish",
                ["PhoneNumber"] = phoneNumber,
                ["Message"] = message,
                ["Version"] = "2010-03-31"
            };

            // Note: This is a simplified implementation
            // Production should use AWS SDK or implement proper AWS signature V4
            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            return new SmsResult
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "SMS sent" : $"AWS SNS error: {responseBody}"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AWS SNS SMS failed");
            return new SmsResult { Success = false, Message = ex.Message };
        }
    }
}

/// <summary>
/// Firebase SMS Provider (for testing/development)
/// </summary>
public class FirebaseSmsProvider : SmsProviderBase
{
    public override string Type => "firebase";
    public override string DisplayName => "Firebase (Mock)";

    public FirebaseSmsProvider(ILogger<FirebaseSmsProvider> logger) : base(logger)
    {
    }

    public override Task<SmsResult> SendSmsAsync(string phoneNumber, string message)
    {
        // Firebase doesn't directly send SMS, but this can be used for testing
        // or integration with Firebase Cloud Functions
        Logger.LogInformation("Mock SMS to {Phone}: {Message}", phoneNumber, message);

        return Task.FromResult(new SmsResult
        {
            Success = true,
            Message = "Mock SMS logged",
            MessageId = Guid.NewGuid().ToString()
        });
    }
}

/// <summary>
/// Console SMS Provider (for development/testing)
/// </summary>
public class ConsoleSmsProvider : SmsProviderBase
{
    public override string Type => "console";
    public override string DisplayName => "Console (Development)";

    public ConsoleSmsProvider(ILogger<ConsoleSmsProvider> logger) : base(logger)
    {
    }

    public override Task<SmsResult> SendSmsAsync(string phoneNumber, string message)
    {
        Logger.LogInformation("SMS to {Phone}: {Message}", phoneNumber, message);
        Console.WriteLine($"[SMS] To: {phoneNumber}");
        Console.WriteLine($"[SMS] Message: {message}");

        return Task.FromResult(new SmsResult
        {
            Success = true,
            Message = "SMS logged to console",
            MessageId = Guid.NewGuid().ToString()
        });
    }
}

/// <summary>
/// SMS Gateway configuration info
/// </summary>
public class SmsGatewayInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Interface for SMS service
/// </summary>
public interface ISmsService
{
    void SetDefaultProvider(string providerType);
    Task SendAsync(string to, string message);
    Task<SmsResult> SendAsync(string to, string message, string? providerType = null);
    Task<IEnumerable<SmsGatewayInfo>> GetAllGatewaysAsync();
    Task<SmsGatewayInfo?> GetGatewayAsync(int id);
    Task<int> CreateGatewayAsync(string name, string provider, Dictionary<string, object> options, string? description = null);
    Task<bool> DeleteGatewayAsync(string name);
}

/// <summary>
/// SMS Service implementation using configured providers
/// </summary>
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly Dictionary<string, ISmsProvider> _providers = new();
    private readonly Dictionary<int, SmsGatewayInfo> _gateways = new();
    private string _defaultProvider = "http";
    private int _nextGatewayId = 1;

    public SmsService(ILogger<SmsService> logger, IEnumerable<ISmsProvider> providers)
    {
        _logger = logger;
        foreach (var provider in providers)
        {
            _providers[provider.Type] = provider;
        }
    }

    public void SetDefaultProvider(string providerType)
    {
        _defaultProvider = providerType;
    }

    public async Task SendAsync(string to, string message)
    {
        if (!_providers.TryGetValue(_defaultProvider, out var provider))
        {
            throw new InvalidOperationException($"SMS provider '{_defaultProvider}' not found");
        }

        var result = await provider.SendSmsAsync(to, message);
        if (!result.Success)
        {
            throw new InvalidOperationException($"SMS failed: {result.Message}");
        }
    }

    public async Task<SmsResult> SendAsync(string to, string message, string? providerType = null)
    {
        var type = providerType ?? _defaultProvider;
        
        if (!_providers.TryGetValue(type, out var provider))
        {
            return new SmsResult { Success = false, Message = $"Provider '{type}' not found" };
        }

        return await provider.SendSmsAsync(to, message);
    }

    public Task<IEnumerable<SmsGatewayInfo>> GetAllGatewaysAsync()
    {
        return Task.FromResult<IEnumerable<SmsGatewayInfo>>(_gateways.Values);
    }

    public Task<SmsGatewayInfo?> GetGatewayAsync(int id)
    {
        _gateways.TryGetValue(id, out var gateway);
        return Task.FromResult(gateway);
    }

    public Task<int> CreateGatewayAsync(string name, string provider, Dictionary<string, object> options, string? description = null)
    {
        var id = _nextGatewayId++;
        _gateways[id] = new SmsGatewayInfo
        {
            Id = id,
            Name = name,
            Provider = provider,
            Options = options,
            Description = description
        };
        _logger.LogInformation("Created SMS gateway {Name} with provider {Provider}", name, provider);
        return Task.FromResult(id);
    }

    public Task<bool> DeleteGatewayAsync(string name)
    {
        var gateway = _gateways.Values.FirstOrDefault(g => g.Name == name);
        if (gateway != null)
        {
            _gateways.Remove(gateway.Id);
            _logger.LogInformation("Deleted SMS gateway {Name}", name);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
