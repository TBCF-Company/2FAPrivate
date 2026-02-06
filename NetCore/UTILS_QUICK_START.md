# Quick Start Guide - Using the Converted Utilities

## Overview
All Python utilities from `privacyidea/lib/utils/` have been converted to C# and are available in the `PrivacyIdeaServer.Lib.Utils` namespace.

## Common Usage Examples

### 1. String Encoding

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Convert to UTF-8 bytes
var bytes = StringEncoding.ToUtf8("Hello, World!");

// Base64 encoding
var base64 = StringEncoding.B64EncodeAndUnicode("secret");

// Base32 encoding
var base32 = StringEncoding.B32EncodeAndUnicode("OTPKEY");

// Hex encoding
var hex = StringEncoding.HexlifyAndUnicode("data");

// URL-safe Base64
var urlSafe = StringEncoding.UrlSafeB64EncodeAndUnicode("url-data");
```

### 2. Time Utilities

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Check if current time is in range
var inRange = TimeUtilities.CheckTimeInRange("Mon-Fri:09:00-17:00");

// Parse date offsets
var futureDate = TimeUtilities.ParseDate("+30d");  // 30 days from now

// Parse time deltas
var delta = TimeUtilities.ParseTimeDelta("+5h");   // 5 hours

// Parse time limit (2 in 5 minutes)
var (count, timespan) = TimeUtilities.ParseTimeLimit("2/5m");

// Convert to UTC
var utc = TimeUtilities.ConvertTimestampToUtc(DateTime.Now);
```

### 3. Network & IP Handling

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Parse proxy settings
var proxyPaths = NetworkUtilities.ParseProxy("10.0.0.0/24 > 192.168.0.0/24");

// Check IP in policy
var (found, excluded) = NetworkUtilities.CheckIpInPolicy(
    "192.168.1.100", 
    new List<string> { "192.168.1.0/24", "!192.168.1.12" }
);

// Get client IP from request (with proxy support)
var clientIp = NetworkUtilities.GetClientIp(
    httpRequest, 
    proxySettings, 
    clientParam, 
    logger
);
```

### 4. Validation

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Validate email
var isValid = EmailValidation.ValidateEmail("user@example.com");

// Check serial number
ValidationHelpers.CheckSerialValid("TOKEN12345");  // throws if invalid

// Validate name
ValidationHelpers.SanityNameCheck("my-resolver");  // throws if invalid

// Check PIN against policy
var (valid, message) = ValidationHelpers.CheckPinContents("Pin123!", "cns");
// Policy: c=characters, n=numbers, s=special

// Decode Base32Check (for token enrollment)
var payload = ValidationHelpers.DecodeBase32Check("ABCDEFGH123456");
```

### 5. QR Code Generation

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Generate QR code as data URI for HTML <img> tag
var qrDataUri = QrCodeGenerator.CreateImg(
    "otpauth://totp/user@example.com?secret=JBSWY3DPEHPK3PXP",
    pixelsPerModule: 10
);

// Use in HTML: <img src="@qrDataUri" />
```

### 6. Token Utilities

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Split PIN and OTP
var (pin, otp) = TokenUtilities.SplitPinPass(
    password: "pin123456",
    otpLen: 6,
    prependPin: true
);
// pin = "pin", otp = "123456"

// Create email tag dictionary
var tags = TokenUtilities.CreateTagDict(
    loggedInUser: userDict,
    request: httpRequest,
    serial: "TOKEN001",
    tokenType: "TOTP",
    escapeHtml: true
);
// Use tags in email templates: {serial}, {tokentype}, {user}, etc.
```

### 7. Comparison Framework

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Compare values with operators
var result = CompareUtilities.CompareValues(10, "<", 20);  // true
var result2 = CompareUtilities.CompareValues("hello", "matches", "h.*o");  // true
var result3 = CompareUtilities.CompareValues("item", "in", "item1,item2,item");  // true

// Compare integers from string condition
var valid = CompareUtilities.CompareInts("<100", 50, logger);  // true

// Compare time (within last 5 days)
var recent = CompareUtilities.CompareTime("5d", someDateTime, logger);

// Parse and compare generic conditions
var matches = CompareUtilities.CompareGeneric(
    condition: "status == active",
    keyMethod: key => myDictionary[key],
    warning: "Invalid condition: {0}",
    logger: logger
);
```

### 8. Data Redaction (Privacy)

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Redact email for logs
var redacted = DataRedaction.RedactedEmail("user@example.com");
// Output: "us********@e****.com"

// Redact phone number
var redacted = DataRedaction.RedactedPhoneNumber("01234567890");
// Output: "****-******90"
```

### 9. Yubikey Utilities

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Encode to Modhex
var modhex = YubikeyUtilities.ModhexEncode("data");

// Decode from Modhex
var original = YubikeyUtilities.ModhexDecode(modhex);

// Calculate CRC-16 checksum
var crc = YubikeyUtilities.Checksum(data);
```

### 10. Configuration Parsing

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Check if value is "true"
var isTrue = ConfigurationParser.IsTrue("1");  // true
var isTrue2 = ConfigurationParser.IsTrue("True");  // true

// Parse string to dictionary
var dict = ConfigurationParser.ParseStringToDict(
    ":key1: val1 val2 :key2: val3"
);
// Result: { "key1": ["val1", "val2"], "key2": ["val3"] }

// Parse resolver/connector parameters
var (data, types, descriptions) = ConfigurationParser.GetDataFromParams(
    @params: requestParams,
    excludeParams: new List<string> { "resolver", "type" },
    configDescription: configDict,
    module: "resolver",
    type: "LDAP",
    logger: logger
);
```

### 11. Response Formatting

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Prepare API response
var response = ResponseFormatter.PrepareResult(
    value: true,
    rid: 1,
    details: new Dictionary<string, object>
    {
        ["message"] = "Authentication successful"
    },
    version: VersionInfo.GetVersion(),
    versionNumber: VersionInfo.GetVersionNumber()
);

// Access authentication constants
var accept = ResponseFormatter.AuthResponse.Accept;      // "ACCEPT"
var reject = ResponseFormatter.AuthResponse.Reject;      // "REJECT"
var challenge = ResponseFormatter.AuthResponse.Challenge; // "CHALLENGE"
```

### 12. User Agent Parsing

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Parse user agent
var (agent, version, comment) = UserAgentParser.GetPluginInfoFromUserAgent(
    "MyPlugin/1.2.3 MyApp/2.0"
);
// agent = "MyPlugin", version = "1.2.3", comment = "MyApp/2.0"

// Extract computer name
var computerName = UserAgentParser.GetComputerNameFromUserAgent(
    "Mozilla/5.0 ComputerName/LAPTOP-ABC123",
    customKeys: new List<string> { "CustomKey" }
);
// computerName = "LAPTOP-ABC123"
```

## Integration with ASP.NET Core

### Dependency Injection (Optional)

While all utilities are static, you can create wrapper services if needed:

```csharp
// Startup.cs or Program.cs
builder.Services.AddSingleton<IEmailValidator, EmailValidator>();

// EmailValidator.cs
public class EmailValidator : IEmailValidator
{
    public bool Validate(string email) 
        => EmailValidation.ValidateEmail(email);
}
```

### Use in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly ILogger<TokenController> _logger;

    public TokenController(ILogger<TokenController> logger)
    {
        _logger = logger;
    }

    [HttpPost("validate")]
    public IActionResult ValidateToken([FromBody] ValidationRequest request)
    {
        // Get client IP with proxy support
        var clientIp = NetworkUtilities.GetClientIp(
            Request, 
            _configuration["ProxySettings"], 
            request.ClientParam,
            _logger
        );

        // Split PIN and OTP
        var (pin, otp) = TokenUtilities.SplitPinPass(
            request.Password, 
            otpLen: 6, 
            prependPin: true
        );

        // Validate serial
        try
        {
            ValidationHelpers.CheckSerialValid(request.Serial);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        // ... continue with token validation
    }
}
```

## Error Handling

All utility methods throw appropriate exceptions:

```csharp
try
{
    // Validation
    ValidationHelpers.CheckSerialValid("invalid@serial");
}
catch (ArgumentException ex)
{
    _logger.LogError(ex, "Invalid serial number");
    return BadRequest(ex.Message);
}

try
{
    // Comparison
    var result = CompareUtilities.CompareValues(value, "invalid_op", other);
}
catch (CompareException ex)
{
    _logger.LogError(ex, "Comparison failed");
    return BadRequest("Invalid comparison");
}
```

## Performance Tips

1. **Compiled Regex**: The utilities use `[GeneratedRegex]` for optimal performance
2. **Static Methods**: No object allocation overhead
3. **Minimal Dependencies**: Only QRCoder is external, rest is .NET standard library
4. **Efficient Encoding**: Uses native .NET encoding classes

## Testing

Example unit test:

```csharp
[Fact]
public void TestEmailValidation()
{
    Assert.True(EmailValidation.ValidateEmail("user@example.com"));
    Assert.False(EmailValidation.ValidateEmail("invalid-email"));
}

[Fact]
public void TestTimeInRange()
{
    var monday9AM = new DateTime(2025, 2, 3, 9, 0, 0); // Monday
    Assert.True(TimeUtilities.CheckTimeInRange(
        "Mon-Fri:08:00-18:00", 
        monday9AM
    ));
}

[Theory]
[InlineData("<100", 50, true)]
[InlineData(">100", 50, false)]
[InlineData("==50", 50, true)]
public void TestCompareInts(string condition, int value, bool expected)
{
    Assert.Equal(expected, CompareUtilities.CompareInts(condition, value));
}
```

## Next Steps

1. Add these utilities to your token validation logic
2. Use in email notification handlers
3. Implement policy-based access control with comparison utilities
4. Generate QR codes for token enrollment
5. Create comprehensive unit tests

## Documentation

See `NetCore/UTILS_CONVERSION_REPORT.md` for detailed conversion information.
