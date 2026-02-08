# Hướng dẫn Khởi động Nhanh - Sử dụng Các Tiện ích Đã Chuyển đổi

## Tổng quan
Tất cả các tiện ích Python từ `privacyidea/lib/utils/` đã được chuyển đổi sang C# và có sẵn trong namespace `PrivacyIdeaServer.Lib.Utils`.

## Ví dụ Sử dụng Thông dụng

### 1. Mã hóa Chuỗi

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Chuyển đổi sang bytes UTF-8
var bytes = StringEncoding.ToUtf8("Xin chào, Thế giới!");

// Mã hóa Base64
var base64 = StringEncoding.B64EncodeAndUnicode("bí mật");

// Mã hóa Base32
var base32 = StringEncoding.B32EncodeAndUnicode("OTPKEY");

// Mã hóa Hex
var hex = StringEncoding.HexlifyAndUnicode("dữ liệu");

// Base64 an toàn cho URL
var urlSafe = StringEncoding.UrlSafeB64EncodeAndUnicode("url-data");
```

### 2. Tiện ích Thời gian

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Kiểm tra thời gian hiện tại có trong khoảng không
var inRange = TimeUtilities.CheckTimeInRange("Mon-Fri:09:00-17:00");

// Phân tích offset ngày
var futureDate = TimeUtilities.ParseDate("+30d");  // 30 ngày từ bây giờ

// Phân tích delta thời gian
var delta = TimeUtilities.ParseTimeDelta("+5h");   // 5 giờ

// Phân tích giới hạn thời gian (2 trong 5 phút)
var (count, timespan) = TimeUtilities.ParseTimeLimit("2/5m");

// Chuyển đổi sang UTC
var utc = TimeUtilities.ConvertTimestampToUtc(DateTime.Now);
```

### 3. Xử lý Mạng & IP

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Phân tích cài đặt proxy
var proxyPaths = NetworkUtilities.ParseProxy("10.0.0.0/24 > 192.168.0.0/24");

// Kiểm tra IP trong policy
var (found, excluded) = NetworkUtilities.CheckIpInPolicy(
    "192.168.1.100", 
    new List<string> { "192.168.1.0/24", "!192.168.1.12" }
);

// Lấy IP client từ request (với hỗ trợ proxy)
var clientIp = NetworkUtilities.GetClientIp(
    httpRequest, 
    proxySettings, 
    clientParam, 
    logger
);
```

### 4. Xác thực

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Xác thực email
var isValid = EmailValidation.ValidateEmail("user@example.com");

// Kiểm tra số serial
ValidationHelpers.CheckSerialValid("TOKEN12345");  // throw nếu không hợp lệ

// Xác thực tên
ValidationHelpers.SanityNameCheck("my-resolver");  // throw nếu không hợp lệ

// Kiểm tra nội dung PIN theo policy
var (valid, message) = ValidationHelpers.CheckPinContents("Pin123!", "cns");
// Policy: c=ký tự, n=số, s=đặc biệt

// Giải mã Base32Check (cho đăng ký token)
var payload = ValidationHelpers.DecodeBase32Check("ABCDEFGH123456");
```

### 5. Tạo mã QR

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Tạo mã QR dưới dạng data URI cho thẻ HTML <img>
var qrDataUri = QrCodeGenerator.CreateImg(
    "otpauth://totp/user@example.com?secret=JBSWY3DPEHPK3PXP",
    pixelsPerModule: 10
);

// Sử dụng trong HTML: <img src="@qrDataUri" />
```

### 6. Tiện ích Token

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Tách PIN và OTP
var (pin, otp) = TokenUtilities.SplitPinPass(
    password: "pin123456",
    otpLen: 6,
    prependPin: true
);
// pin = "pin", otp = "123456"

// Tạo số serial ngẫu nhiên
var serial = TokenUtilities.GenerateSerialNumber("TOTP");
// Kết quả: "TOTP00012345"

// Xác thực OTP value
var isValidOtp = TokenUtilities.CheckOtp("123456");
```

### 7. Hash & Mã hóa

```csharp
using PrivacyIdeaServer.Lib.Utils;

// SHA256 hash
var hash = HashingUtilities.ComputeSha256("data");

// HMAC-SHA1
var hmac = HashingUtilities.ComputeHmacSha1("key", "message");

// Tạo chuỗi ngẫu nhiên
var randomStr = CryptoUtilities.GenerateRandomString(16);

// Tạo hex ngẫu nhiên
var randomHex = CryptoUtilities.GenerateRandomHex(32);

// So sánh an toàn (constant-time)
var areEqual = CryptoUtilities.SecureCompare(hash1, hash2);
```

### 8. Tiện ích File & Path

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Đọc file an toàn
var content = await FileUtilities.SafeReadFileAsync("/path/to/file.txt");

// Ghi file an toàn
await FileUtilities.SafeWriteFileAsync("/path/to/file.txt", content);

// Kiểm tra path có hợp lệ không
var isValid = FileUtilities.ValidatePath("/etc/privacyidea/config");

// Lấy thư mục tạm
var tempDir = FileUtilities.GetTempDirectory();
```

### 9. Xử lý JSON & Serialization

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Serialize object sang JSON
var json = JsonUtilities.Serialize(myObject);

// Deserialize JSON sang object
var obj = JsonUtilities.Deserialize<MyType>(json);

// Parse JSON an toàn (không throw exception)
var (success, result) = JsonUtilities.TryDeserialize<MyType>(json);
```

### 10. Tiện ích HTTP

```csharp
using PrivacyIdeaServer.Lib.Utils;

// Tạo query string
var queryString = HttpUtilities.BuildQueryString(new Dictionary<string, string>
{
    { "param1", "value1" },
    { "param2", "value2" }
});
// Kết quả: "param1=value1&param2=value2"

// Parse query string
var params = HttpUtilities.ParseQueryString("?param1=value1&param2=value2");

// URL encode
var encoded = HttpUtilities.UrlEncode("hello world");

// URL decode
var decoded = HttpUtilities.UrlDecode("hello+world");
```

## Tích hợp với ASP.NET Core

### Sử dụng trong Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class UtilityController : ControllerBase
{
    private readonly ILogger<UtilityController> _logger;
    
    public UtilityController(ILogger<UtilityController> logger)
    {
        _logger = logger;
    }
    
    [HttpPost("validate-email")]
    public IActionResult ValidateEmail([FromBody] string email)
    {
        var isValid = EmailValidation.ValidateEmail(email);
        return Ok(new { isValid, email });
    }
    
    [HttpPost("generate-qr")]
    public IActionResult GenerateQrCode([FromBody] string data)
    {
        var qrCode = QrCodeGenerator.CreateImg(data, pixelsPerModule: 8);
        return Ok(new { qrCode });
    }
    
    [HttpGet("check-time-range")]
    public IActionResult CheckTimeRange([FromQuery] string timeRange)
    {
        var inRange = TimeUtilities.CheckTimeInRange(timeRange);
        return Ok(new { inRange, currentTime = DateTime.Now });
    }
}
```

### Sử dụng trong Middleware

```csharp
public class IpValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpValidationMiddleware> _logger;
    
    public IpValidationMiddleware(
        RequestDelegate next, 
        ILogger<IpValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = NetworkUtilities.GetClientIp(
            context.Request, 
            proxySettings: null, 
            clientParam: null, 
            _logger
        );
        
        var allowedIps = new List<string> { "192.168.1.0/24", "10.0.0.0/8" };
        var (allowed, excluded) = NetworkUtilities.CheckIpInPolicy(
            clientIp, 
            allowedIps
        );
        
        if (!allowed || excluded)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("IP không được phép truy cập");
            return;
        }
        
        await _next(context);
    }
}
```

## Configuration trong appsettings.json

```json
{
  "UtilitySettings": {
    "QrCode": {
      "DefaultPixelsPerModule": 10,
      "DefaultBorder": 4
    },
    "Validation": {
      "MaxSerialLength": 50,
      "MinPinLength": 4,
      "MaxPinLength": 20
    },
    "TimeSettings": {
      "DefaultTimezone": "Asia/Ho_Chi_Minh",
      "WorkHours": "Mon-Fri:08:00-18:00"
    },
    "Network": {
      "ProxySettings": "10.0.0.0/24 > 192.168.0.0/24",
      "AllowedIPs": [
        "192.168.1.0/24",
        "10.0.0.0/8"
      ]
    }
  }
}
```

## Unit Testing

```csharp
[TestClass]
public class StringEncodingTests
{
    [TestMethod]
    public void TestBase64Encoding()
    {
        var input = "Hello, World!";
        var encoded = StringEncoding.B64EncodeAndUnicode(input);
        var decoded = StringEncoding.B64DecodeAndUnicode(encoded);
        
        Assert.AreEqual(input, decoded);
    }
    
    [TestMethod]
    public void TestBase32Encoding()
    {
        var input = "TESTKEY123";
        var encoded = StringEncoding.B32EncodeAndUnicode(input);
        var decoded = StringEncoding.B32DecodeAndUnicode(encoded);
        
        Assert.AreEqual(input, decoded);
    }
}

[TestClass]
public class ValidationTests
{
    [TestMethod]
    public void TestEmailValidation()
    {
        Assert.IsTrue(EmailValidation.ValidateEmail("user@example.com"));
        Assert.IsFalse(EmailValidation.ValidateEmail("invalid-email"));
        Assert.IsFalse(EmailValidation.ValidateEmail("@example.com"));
    }
    
    [TestMethod]
    public void TestPinContents()
    {
        var (valid1, _) = ValidationHelpers.CheckPinContents("Pin123!", "cns");
        Assert.IsTrue(valid1);
        
        var (valid2, _) = ValidationHelpers.CheckPinContents("123456", "n");
        Assert.IsTrue(valid2);
        
        var (valid3, _) = ValidationHelpers.CheckPinContents("abc", "cns");
        Assert.IsFalse(valid3); // Thiếu số và ký tự đặc biệt
    }
}
```

## Các Thực hành Tốt nhất

### 1. Error Handling
```csharp
try
{
    ValidationHelpers.CheckSerialValid(serial);
}
catch (PrivacyIDEAError ex)
{
    _logger.LogError(ex, "Serial không hợp lệ: {Serial}", serial);
    return BadRequest(ex.Message);
}
```

### 2. Logging
```csharp
_logger.LogDebug("Đang xác thực email: {Email}", email);
var isValid = EmailValidation.ValidateEmail(email);
_logger.LogInformation("Kết quả xác thực email {Email}: {IsValid}", email, isValid);
```

### 3. Performance
```csharp
// Cache kết quả mã hóa nếu có thể
private readonly ConcurrentDictionary<string, string> _base64Cache = new();

public string GetBase64Cached(string input)
{
    return _base64Cache.GetOrAdd(input, 
        key => StringEncoding.B64EncodeAndUnicode(key));
}
```

### 4. Security
```csharp
// Luôn sử dụng SecureCompare cho so sánh sensitive data
if (CryptoUtilities.SecureCompare(expectedHash, actualHash))
{
    // Hợp lệ
}

// Không sử dụng == cho hash hoặc passwords
```

## Xử lý Sự cố

### Lỗi Encoding
```csharp
try
{
    var decoded = StringEncoding.B64DecodeAndUnicode(input);
}
catch (FormatException ex)
{
    _logger.LogError(ex, "Lỗi giải mã Base64");
    // Xử lý input không hợp lệ
}
```

### Lỗi Time Range
```csharp
try
{
    var inRange = TimeUtilities.CheckTimeInRange("Invalid-Range");
}
catch (ArgumentException ex)
{
    _logger.LogError(ex, "Định dạng time range không hợp lệ");
    return BadRequest("Time range phải có định dạng: 'Mon-Fri:09:00-17:00'");
}
```

### Lỗi IP Validation
```csharp
try
{
    var (allowed, excluded) = NetworkUtilities.CheckIpInPolicy(ip, policies);
}
catch (FormatException ex)
{
    _logger.LogError(ex, "Định dạng IP không hợp lệ: {IP}", ip);
    return BadRequest("Địa chỉ IP không hợp lệ");
}
```

## Tài liệu Tham khảo

- `StringEncoding.cs` - Các tiện ích mã hóa chuỗi
- `TimeUtilities.cs` - Xử lý thời gian và ngày tháng
- `NetworkUtilities.cs` - Tiện ích mạng và IP
- `ValidationHelpers.cs` - Các hàm xác thực
- `QrCodeGenerator.cs` - Tạo mã QR
- `TokenUtilities.cs` - Tiện ích liên quan đến token
- `CryptoUtilities.cs` - Các hàm mã hóa và hash
- `FileUtilities.cs` - Xử lý file và path
- `JsonUtilities.cs` - Serialization JSON
- `HttpUtilities.cs` - Tiện ích HTTP
