# Hướng dẫn tích hợp SDK 2FA cho Ứng dụng Chứng khoán

Tài liệu này hướng dẫn chi tiết cách tích hợp SDK xác thực 2 yếu tố vào ứng dụng giao dịch chứng khoán.

## Mục lục

1. [Tổng quan](#tổng-quan)
2. [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
3. [Cài đặt](#cài-đặt)
4. [Kiến trúc hệ thống](#kiến-trúc-hệ-thống)
5. [Triển khai Frontend (Flutter)](#triển-khai-frontend-flutter)
6. [Triển khai Backend](#triển-khai-backend)
7. [Quy trình xác thực](#quy-trình-xác-thực)
8. [Bảo mật](#bảo-mật)
9. [Test và Debug](#test-và-debug)
10. [Production Checklist](#production-checklist)

## Tổng quan

SDK 2FA này cung cấp giải pháp xác thực giao dịch chứng khoán toàn diện với:

- **Xác thực thiết bị**: Chỉ cho phép thiết bị đã đăng ký thực hiện giao dịch
- **OTP động**: Mã xác thực thay đổi mỗi 30 giây
- **Chữ ký giao dịch**: Mỗi giao dịch được ký với Device ID + OTP
- **Quản lý đa thiết bị**: Người dùng có thể đăng ký nhiều thiết bị

## Yêu cầu hệ thống

### Frontend (Mobile App)

- Flutter SDK: >= 3.0.0
- Dart SDK: >= 3.0.0
- Android API Level: 21+ (Android 5.0+)
- iOS Deployment Target: 12.0+

### Backend

Chọn một trong các option sau:

- **.NET Core**: >= 6.0
- **NodeJS**: >= 16.0
- **Python/Django**: (có thể tự triển khai API tương tự)

### Infrastructure

- HTTPS endpoint cho API
- Database (PostgreSQL, MySQL, hoặc MongoDB)
- Redis (optional, cho cache)

## Cài đặt

### Bước 1: Thêm SDK vào dự án Flutter

Mở file `pubspec.yaml`:

```yaml
dependencies:
  flutter:
    sdk: flutter
  
  # 2FA SDK
  two_factor_auth_sdk:
    path: ../flutter_sdk/two_factor_auth_sdk
```

Chạy:

```bash
flutter pub get
```

### Bước 2: Cấu hình Android

Mở `android/app/src/main/AndroidManifest.xml`:

```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <!-- Thêm quyền truy cập Internet -->
    <uses-permission android:name="android.permission.INTERNET" />
    
    <application
        android:label="Stock Trading App"
        android:name="${applicationName}"
        android:icon="@mipmap/ic_launcher">
        <!-- ... -->
    </application>
</manifest>
```

### Bước 3: Cấu hình iOS

Không cần cấu hình đặc biệt cho iOS.

### Bước 4: Triển khai Backend

Chọn một trong các package backend có sẵn:

#### Option A: .NET Core

```bash
cd NetCore/PrivacyIdeaServer
dotnet restore
dotnet run
```

#### Option B: NodeJS

```bash
cd nodejs_package/two-factor-auth
npm install
npm start
```

## Kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────────────┐
│                     Ứng dụng Chứng khoán                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ Sử dụng
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Two Factor Auth SDK                        │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐   │
│  │   Device     │  │     OTP      │  │  Transaction    │   │
│  │   Manager    │  │   Manager    │  │  Auth Manager   │   │
│  └──────────────┘  └──────────────┘  └─────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ API Calls
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      2FA Backend API                         │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐   │
│  │   Device     │  │     OTP      │  │   Transaction   │   │
│  │   Service    │  │   Service    │  │   Service       │   │
│  └──────────────┘  └──────────────┘  └─────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ Store/Retrieve
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                         Database                             │
│  - Devices Table                                             │
│  - Pending Activations Table                                 │
│  - User Secrets Table                                        │
│  - Transaction Logs Table                                    │
└─────────────────────────────────────────────────────────────┘
```

## Triển khai Frontend (Flutter)

### 1. Khởi tạo SDK

Tạo file `lib/services/auth_service.dart`:

```dart
import 'package:two_factor_auth_sdk/two_factor_auth_sdk.dart';

class AuthService {
  static final AuthService _instance = AuthService._internal();
  factory AuthService() => _instance;
  AuthService._internal();

  late final TransactionAuthManager _authManager;
  
  void initialize(String serverUrl) {
    _authManager = TransactionAuthManager(serverUrl: serverUrl);
  }
  
  TransactionAuthManager get authManager => _authManager;
}
```

Khởi tạo trong `main.dart`:

```dart
void main() {
  // Khởi tạo auth service
  AuthService().initialize('https://your-api-server.com');
  
  runApp(const MyApp());
}
```

### 2. Màn hình đăng nhập với kích hoạt thiết bị

```dart
class LoginScreen extends StatefulWidget {
  @override
  _LoginScreenState createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _authService = AuthService();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();

  Future<void> _login() async {
    // 1. Đăng nhập với username/password (API của bạn)
    final loginSuccess = await yourApi.login(
      username: _usernameController.text,
      password: _passwordController.text,
    );

    if (!loginSuccess) {
      // Hiển thị lỗi đăng nhập
      return;
    }

    // 2. Kiểm tra thiết bị đã được kích hoạt chưa
    final isAuthorized = await _authService.authManager.isDeviceAuthorized();

    if (isAuthorized) {
      // Thiết bị đã kích hoạt, chuyển đến trang chính
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (context) => HomeScreen()),
      );
    } else {
      // Cần kích hoạt thiết bị
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (context) => DeviceActivationScreen(
            username: _usernameController.text,
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: EdgeInsets.all(16.0),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            TextField(
              controller: _usernameController,
              decoration: InputDecoration(labelText: 'Tên đăng nhập'),
            ),
            SizedBox(height: 16),
            TextField(
              controller: _passwordController,
              decoration: InputDecoration(labelText: 'Mật khẩu'),
              obscureText: true,
            ),
            SizedBox(height: 24),
            ElevatedButton(
              onPressed: _login,
              child: Text('Đăng nhập'),
            ),
          ],
        ),
      ),
    );
  }
}
```

### 3. Màn hình kích hoạt thiết bị

```dart
class DeviceActivationScreen extends StatefulWidget {
  final String username;

  DeviceActivationScreen({required this.username});

  @override
  _DeviceActivationScreenState createState() => _DeviceActivationScreenState();
}

class _DeviceActivationScreenState extends State<DeviceActivationScreen> {
  final _authService = AuthService();
  final _otpController = TextEditingController();
  String? _secret;
  bool _activationRequested = false;

  Future<void> _requestActivation() async {
    final result = await _authService.authManager.requestDeviceActivation(
      username: widget.username,
      issuer: 'StockTradingApp',
    );

    if (result['success']) {
      setState(() {
        _secret = result['secret'];
        _activationRequested = true;
      });
      
      // Thông báo người dùng xem OTP trên web
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('Kiểm tra giao diện web'),
          content: Text(
            'Vui lòng đăng nhập vào trang web giao dịch chứng khoán '
            'để xem mã OTP kích hoạt thiết bị.'
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('OK'),
            ),
          ],
        ),
      );
    }
  }

  Future<void> _activateDevice() async {
    final success = await _authService.authManager.activateDevice(
      otpCode: _otpController.text,
      secret: _secret!,
      username: widget.username,
      issuer: 'StockTradingApp',
    );

    if (success) {
      // Kích hoạt thành công
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (context) => HomeScreen()),
      );
    } else {
      // Hiển thị lỗi
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Mã OTP không đúng')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Kích hoạt thiết bị')),
      body: Padding(
        padding: EdgeInsets.all(16.0),
        child: Column(
          children: [
            if (!_activationRequested) ...[
              Text(
                'Kích hoạt thiết bị này để thực hiện giao dịch',
                style: TextStyle(fontSize: 16),
              ),
              SizedBox(height: 24),
              ElevatedButton(
                onPressed: _requestActivation,
                child: Text('Yêu cầu kích hoạt'),
              ),
            ] else ...[
              Text('Nhập mã OTP từ giao diện web'),
              SizedBox(height: 16),
              TextField(
                controller: _otpController,
                decoration: InputDecoration(
                  labelText: 'Mã OTP',
                  border: OutlineInputBorder(),
                ),
                keyboardType: TextInputType.number,
                maxLength: 6,
              ),
              SizedBox(height: 24),
              ElevatedButton(
                onPressed: _activateDevice,
                child: Text('Kích hoạt'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
```

### 4. Màn hình giao dịch với OTP

```dart
class TradingScreen extends StatefulWidget {
  @override
  _TradingScreenState createState() => _TradingScreenState();
}

class _TradingScreenState extends State<TradingScreen> {
  final _authService = AuthService();
  final _stockCodeController = TextEditingController();
  final _quantityController = TextEditingController();
  
  String? _currentOtp;
  int _remainingTime = 30;
  
  // Secret lấy từ server sau khi đăng nhập
  late String _userSecret;

  @override
  void initState() {
    super.initState();
    _loadUserSecret();
    _generateOtp();
    _startOtpTimer();
  }

  Future<void> _loadUserSecret() async {
    // Lấy secret từ server API của bạn
    _userSecret = await yourApi.getUserSecret();
  }

  Future<void> _generateOtp() async {
    final otp = await _authService.authManager.generateTransactionOTP(_userSecret);
    setState(() {
      _currentOtp = otp;
    });
  }

  void _startOtpTimer() {
    Future.delayed(Duration(seconds: 1), () {
      if (mounted) {
        final remaining = _authService.authManager.getOtpRemainingTime();
        setState(() {
          _remainingTime = remaining;
        });
        
        if (remaining <= 1) {
          _generateOtp();
        }
        
        _startOtpTimer();
      }
    });
  }

  Future<void> _executeTrade(String type) async {
    // 1. Tạo chữ ký giao dịch
    final signature = await _authService.authManager.createTransactionSignature(
      secret: _userSecret,
      transactionId: 'TXN_${DateTime.now().millisecondsSinceEpoch}',
      transactionType: type,
      amount: double.parse(_quantityController.text),
    );

    // 2. Gửi giao dịch lên server
    final result = await yourApi.executeTransaction(
      stockCode: _stockCodeController.text,
      quantity: int.parse(_quantityController.text),
      type: type,
      signature: signature,
    );

    // 3. Hiển thị kết quả
    if (result.success) {
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('Thành công'),
          content: Text('Giao dịch $type đã được thực hiện'),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('OK'),
            ),
          ],
        ),
      );
    } else {
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('Lỗi'),
          content: Text(result.message),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('OK'),
            ),
          ],
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Giao dịch')),
      body: Padding(
        padding: EdgeInsets.all(16.0),
        child: Column(
          children: [
            // Hiển thị OTP
            Card(
              child: Padding(
                padding: EdgeInsets.all(16.0),
                child: Column(
                  children: [
                    Text('Mã xác thực', style: TextStyle(fontSize: 14)),
                    SizedBox(height: 8),
                    Text(
                      _currentOtp ?? '------',
                      style: TextStyle(
                        fontSize: 32,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 4,
                      ),
                    ),
                    SizedBox(height: 8),
                    LinearProgressIndicator(value: _remainingTime / 30),
                    Text('$_remainingTime giây'),
                  ],
                ),
              ),
            ),
            
            SizedBox(height: 24),
            
            // Form giao dịch
            TextField(
              controller: _stockCodeController,
              decoration: InputDecoration(
                labelText: 'Mã CK',
                border: OutlineInputBorder(),
              ),
            ),
            SizedBox(height: 16),
            TextField(
              controller: _quantityController,
              decoration: InputDecoration(
                labelText: 'Số lượng',
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.number,
            ),
            SizedBox(height: 24),
            
            // Nút MUA/BÁN
            Row(
              children: [
                Expanded(
                  child: ElevatedButton(
                    onPressed: () => _executeTrade('MUA'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.green,
                      padding: EdgeInsets.all(16),
                    ),
                    child: Text('MUA'),
                  ),
                ),
                SizedBox(width: 16),
                Expanded(
                  child: ElevatedButton(
                    onPressed: () => _executeTrade('BÁN'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.red,
                      padding: EdgeInsets.all(16),
                    ),
                    child: Text('BÁN'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
```

## Triển khai Backend

### API Endpoints cần thiết

Backend cần cung cấp các endpoint sau:

#### 1. POST /api/device/request-activation

Yêu cầu kích hoạt thiết bị.

**Request:**
```json
{
  "deviceId": "device-12345",
  "deviceName": "iPhone 13",
  "platform": "iOS",
  "osVersion": "17.0",
  "model": "iPhone13,3",
  "username": "user@example.com",
  "issuer": "StockTradingApp"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Activation OTP generated",
  "otpCode": "123456",
  "secret": "JBSWY3DPEHPK3PXP",
  "deviceId": "device-12345"
}
```

**Implementation (.NET Core):**

```csharp
[HttpPost("request-activation")]
public async Task<ActionResult<ActivationResponse>> RequestActivation(
    [FromBody] ActivationRequest request)
{
    var result = await _deviceService.RequestDeviceActivationAsync(request);
    return Ok(result);
}
```

#### 2. POST /api/device/activate

Kích hoạt thiết bị với OTP.

**Request:**
```json
{
  "deviceId": "device-12345",
  "otpCode": "123456",
  "username": "user@example.com",
  "issuer": "StockTradingApp"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Device activated successfully",
  "activationToken": "token-uuid",
  "activatedAt": "2026-02-10T15:00:00Z"
}
```

#### 3. POST /api/transaction/execute

Thực hiện giao dịch với xác thực OTP.

**Request:**
```json
{
  "stockCode": "VNM",
  "quantity": 100,
  "type": "MUA",
  "signature": {
    "deviceId": "device-12345",
    "deviceName": "iPhone 13",
    "platform": "iOS",
    "otpCode": "654321",
    "transactionId": "TXN_12345",
    "transactionType": "MUA",
    "amount": "100",
    "timestamp": "2026-02-10T15:30:00Z"
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Transaction executed",
  "transactionId": "TXN_12345",
  "executedAt": "2026-02-10T15:30:01Z"
}
```

**Implementation (.NET Core):**

```csharp
[HttpPost("execute")]
public async Task<ActionResult> ExecuteTransaction(
    [FromBody] TransactionRequest request)
{
    // 1. Lấy user secret từ database
    var userSecret = await _userService.GetUserSecretAsync(request.Username);
    
    // 2. Xác thực OTP
    var isValid = _otpService.ValidateTotp(
        userSecret,
        request.Signature.OtpCode
    );
    
    if (!isValid)
    {
        return BadRequest(new { message = "Invalid OTP" });
    }
    
    // 3. Kiểm tra device đã kích hoạt
    var isActivated = await _deviceService.IsDeviceActivatedAsync(
        request.Signature.DeviceId
    );
    
    if (!isActivated)
    {
        return BadRequest(new { message = "Device not activated" });
    }
    
    // 4. Thực hiện giao dịch
    var result = await _tradingService.ExecuteTradeAsync(
        stockCode: request.StockCode,
        quantity: request.Quantity,
        type: request.Type,
        userId: GetCurrentUserId()
    );
    
    // 5. Log giao dịch
    await _auditService.LogTransactionAsync(new TransactionLog
    {
        TransactionId = request.Signature.TransactionId,
        DeviceId = request.Signature.DeviceId,
        UserId = GetCurrentUserId(),
        Type = request.Type,
        StockCode = request.StockCode,
        Quantity = request.Quantity,
        OtpUsed = request.Signature.OtpCode,
        ExecutedAt = DateTime.UtcNow
    });
    
    return Ok(result);
}
```

## Quy trình xác thực

### Luồng kích hoạt thiết bị

1. Người dùng đăng nhập với username/password
2. App kiểm tra thiết bị đã kích hoạt chưa
3. Nếu chưa, app yêu cầu kích hoạt (gọi API request-activation)
4. Server tạo OTP và hiển thị trên giao diện web
5. Người dùng nhập OTP vào app
6. App gửi OTP lên server để kích hoạt
7. Server xác thực OTP và kích hoạt thiết bị

### Luồng xác thực giao dịch

1. Người dùng nhập thông tin giao dịch
2. App tạo OTP từ user secret
3. App tạo chữ ký giao dịch (bao gồm OTP + device info)
4. App gửi giao dịch + chữ ký lên server
5. Server xác thực OTP
6. Server kiểm tra device đã kích hoạt
7. Server thực hiện giao dịch
8. Server log giao dịch cho audit

## Bảo mật

### 1. Lưu trữ Secret

**KHÔNG BAO GIỜ** lưu user secret trong app. Secret phải được lưu trữ an toàn trên server:

```csharp
// Mã hóa secret trước khi lưu vào database
public async Task SaveUserSecretAsync(string userId, string secret)
{
    var encryptedSecret = _encryption.Encrypt(secret);
    
    await _db.UserSecrets.AddAsync(new UserSecret
    {
        UserId = userId,
        EncryptedSecret = encryptedSecret,
        CreatedAt = DateTime.UtcNow
    });
    
    await _db.SaveChangesAsync();
}

// Giải mã khi cần sử dụng
public async Task<string> GetUserSecretAsync(string userId)
{
    var userSecret = await _db.UserSecrets
        .FirstOrDefaultAsync(x => x.UserId == userId);
        
    return _encryption.Decrypt(userSecret.EncryptedSecret);
}
```

### 2. HTTPS

**BẮT BUỘC** sử dụng HTTPS cho tất cả API calls:

```dart
// Trong app config
class AppConfig {
  static const String apiBaseUrl = 'https://api.yourdomain.com'; // HTTPS!
}
```

### 3. Certificate Pinning (Khuyến nghị)

```dart
// Thêm vào pubspec.yaml
dependencies:
  http_certificate_pinning: ^2.0.0

// Sử dụng
final certificatePinning = CertificatePinning();
await certificatePinning.init(
  list: [
    {
      'hostname': 'api.yourdomain.com',
      'sha': 'YOUR_CERTIFICATE_SHA256',
    },
  ],
);
```

### 4. Rate Limiting

Triển khai rate limiting trên server:

```csharp
// Giới hạn số lần yêu cầu kích hoạt
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("activation", opt =>
    {
        opt.PermitLimit = 5; // 5 lần
        opt.Window = TimeSpan.FromMinutes(10); // trong 10 phút
    });
});

// Áp dụng cho endpoint
[RateLimit("activation")]
[HttpPost("request-activation")]
public async Task<ActionResult> RequestActivation(...)
{
    // ...
}
```

### 5. Audit Logging

Log tất cả các giao dịch:

```csharp
public class TransactionLog
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string DeviceId { get; set; }
    public string TransactionType { get; set; }
    public string StockCode { get; set; }
    public decimal Quantity { get; set; }
    public string OtpUsed { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string IpAddress { get; set; }
}
```

## Test và Debug

### 1. Test kích hoạt thiết bị

```dart
void main() {
  testWidgets('Device activation flow', (WidgetTester tester) async {
    final authManager = TransactionAuthManager(
      serverUrl: 'http://localhost:5000',
    );

    // Request activation
    final result = await authManager.requestDeviceActivation(
      username: 'test@example.com',
      issuer: 'TestApp',
    );

    expect(result['success'], true);
    expect(result['otpCode'], isNotNull);
    expect(result['secret'], isNotNull);

    // Activate device
    final activated = await authManager.activateDevice(
      otpCode: result['otpCode'],
      secret: result['secret'],
      username: 'test@example.com',
      issuer: 'TestApp',
    );

    expect(activated, true);
  });
}
```

### 2. Test OTP generation

```dart
void main() {
  test('OTP generation', () {
    final otpManager = OtpManager();
    final secret = otpManager.generateSecret();
    
    // Generate OTP
    final otp1 = otpManager.generateTotp(secret);
    expect(otp1.length, 6);
    
    // Validate OTP
    final isValid = otpManager.validateTotp(secret, otp1);
    expect(isValid, true);
  });
}
```

### 3. Debug mode

Thêm logging để debug:

```dart
class TransactionAuthManager {
  final bool debugMode;
  
  TransactionAuthManager({
    this.serverUrl,
    this.debugMode = false,
  });
  
  Future<Map<String, dynamic>> requestDeviceActivation(...) async {
    if (debugMode) {
      print('Requesting activation for: $username');
    }
    
    final result = await _makeApiCall(...);
    
    if (debugMode) {
      print('Activation result: $result');
    }
    
    return result;
  }
}
```

## Production Checklist

Trước khi deploy lên production, kiểm tra:

- [ ] **HTTPS**: Tất cả API endpoint sử dụng HTTPS
- [ ] **Secret Storage**: User secret được mã hóa trong database
- [ ] **Rate Limiting**: Đã triển khai rate limiting
- [ ] **Audit Logging**: Tất cả giao dịch được log
- [ ] **Error Handling**: Xử lý lỗi đầy đủ và hiển thị thông báo thân thiện
- [ ] **Certificate Pinning**: Đã triển khai (khuyến nghị)
- [ ] **Biometric**: Thêm xác thực sinh trắc học (optional)
- [ ] **Backup**: Có quy trình backup database
- [ ] **Monitoring**: Có hệ thống monitoring và alerting
- [ ] **Load Testing**: Đã test với lượng user lớn
- [ ] **Security Audit**: Đã audit bảo mật
- [ ] **Documentation**: Tài liệu đầy đủ cho dev team
- [ ] **Disaster Recovery**: Có kế hoạch khôi phục khi có sự cố

## Support

Nếu cần hỗ trợ:

1. Xem tài liệu: [README_VI.md](README_VI.md)
2. Xem example: [example/stock_trading_example.dart](example/stock_trading_example.dart)
3. Mở issue: [GitHub Issues](https://github.com/TBCF-Company/2FAPrivate/issues)

## License

AGPL-3.0-or-later
