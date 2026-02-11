# SDK Flutter cho Xác thực 2 Yếu tố (2FA) - Dành cho Ứng dụng Chứng khoán

SDK Flutter toàn diện cho việc triển khai xác thực hai yếu tố với khả năng quản lý thiết bị. SDK này cung cấp nhận diện thiết bị, đăng ký, quản lý danh sách cho phép và kích hoạt thiết bị dựa trên OTP.

## Tính năng

- 🔐 **Nhận diện thiết bị**: Lấy ID thiết bị duy nhất cho Android và iOS
- 📱 **Quản lý thiết bị**: Danh sách cho phép và quản lý các thiết bị được ủy quyền
- 🔑 **Tạo OTP**: Tạo mã TOTP và HOTP
- ✅ **Xác thực OTP**: Xác thực mã OTP với hỗ trợ cửa sổ thời gian
- 🚀 **Kích hoạt thiết bị**: Luồng kích hoạt thiết bị dựa trên OTP
- 💾 **Lưu trữ cục bộ**: Lưu trữ thông tin thiết bị và danh sách cho phép
- 🌐 **Tích hợp API**: Tích hợp tùy chọn với API backend
- 💼 **Xác thực giao dịch**: Xác thực giao dịch chứng khoán an toàn

## Cài đặt

Thêm vào file `pubspec.yaml` của dự án:

```yaml
dependencies:
  two_factor_auth_sdk:
    path: ../flutter_sdk/two_factor_auth_sdk
```

Sau đó chạy:

```bash
flutter pub get
```

## Cấu hình nền tảng

### Android

Thêm vào `android/app/src/main/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.INTERNET" />
```

### iOS

Không cần cấu hình bổ sung.

## Sử dụng cho Ứng dụng Chứng khoán

### 1. Quản lý xác thực giao dịch

```dart
import 'package:two_factor_auth_sdk/two_factor_auth_sdk.dart';

// Tạo transaction authentication manager
final authManager = TransactionAuthManager(
  serverUrl: 'https://your-2fa-server.com',
);

// Kiểm tra thiết bị đã được ủy quyền chưa
final isAuthorized = await authManager.isDeviceAuthorized();

if (!isAuthorized) {
  // Hiển thị màn hình kích hoạt thiết bị
  // Xem ví dụ chi tiết bên dưới
}
```

### 2. Kích hoạt thiết bị lần đầu

```dart
// Bước 1: Yêu cầu kích hoạt từ server
final result = await authManager.requestDeviceActivation(
  username: 'user@example.com',
  issuer: 'StockTradingApp',
);

if (result['success']) {
  // Mã OTP sẽ được hiển thị trên giao diện web/admin
  print('Mã OTP: ${result['otpCode']}');
  
  // Bước 2: Người dùng nhập mã OTP từ web/admin
  final otpCode = '123456'; // Nhập từ người dùng
  
  // Bước 3: Kích hoạt thiết bị với mã OTP
  final success = await authManager.activateDevice(
    otpCode: otpCode,
    secret: result['secret'],
    username: 'user@example.com',
    issuer: 'StockTradingApp',
  );
  
  if (success) {
    print('Thiết bị đã được kích hoạt thành công!');
  }
}
```

### 3. Xác thực giao dịch chứng khoán

```dart
// Tạo mã OTP cho giao dịch
final otp = await authManager.generateTransactionOTP(userSecret);

// Hiển thị mã OTP cho người dùng
print('Mã OTP giao dịch: $otp');

// Lấy thời gian còn lại của mã OTP
final remainingTime = authManager.getOtpRemainingTime();
print('Mã hết hiệu lực sau: $remainingTime giây');

// Tạo chữ ký giao dịch (gửi lên server để xác thực)
final signature = await authManager.createTransactionSignature(
  secret: userSecret,
  transactionId: 'TXN_12345',
  transactionType: 'MUA', // hoặc 'BÁN'
  amount: 1000.0,
);

// Gửi chữ ký lên server để thực hiện giao dịch
// signature chứa: deviceId, deviceName, platform, otpCode, transactionId, etc.
```

### 4. Xác thực OTP trước khi thực hiện giao dịch

```dart
// Khi người dùng nhập OTP để xác nhận giao dịch
final isValid = await authManager.validateTransaction(
  secret: userSecret,
  otpCode: userEnteredOtp,
);

if (isValid) {
  // Thực hiện giao dịch
  print('OTP hợp lệ, đang thực hiện giao dịch...');
} else {
  print('OTP không hợp lệ hoặc đã hết hạn');
}
```

## Ví dụ hoàn chỉnh

Xem file `example/stock_trading_example.dart` để có ví dụ ứng dụng hoàn chỉnh bao gồm:

- Màn hình kích hoạt thiết bị
- Màn hình giao dịch chứng khoán với OTP tự động làm mới
- Xác thực giao dịch MUA/BÁN
- Hiển thị thông tin thiết bị

### Chạy ví dụ

```bash
cd example
flutter run stock_trading_example.dart
```

## Luồng hoạt động

### Luồng kích hoạt thiết bị

```
┌─────────────────┐          ┌─────────────────┐
│  Thiết bị di    │          │  Giao diện Web  │
│  động (App)     │          │  /Admin         │
└────────┬────────┘          └────────┬────────┘
         │                            │
         │ 1. Yêu cầu kích hoạt       │
         ├──────────────────────────>│
         │    (Device ID + Thông tin) │
         │                            │
         │                    2. Tạo OTP
         │                    3. Hiển thị OTP
         │                            │
         │ 4. Người dùng xem OTP      │
         │    trên web và nhập vào app│
         │                            │
         │ 5. Gửi mã OTP              │
         ├──────────────────────────>│
         │                            │
         │                    6. Xác thực OTP
         │                    7. Kích hoạt thiết bị
         │                            │
         │ 8. Kích hoạt thành công    │
         │<──────────────────────────┤
         │                            │
```

### Luồng xác thực giao dịch

```
┌─────────────────┐          ┌─────────────────┐
│  App chứng      │          │  Server backend │
│  khoán          │          │  (API)          │
└────────┬────────┘          └────────┬────────┘
         │                            │
         │ 1. Người dùng đặt lệnh     │
         │    MUA/BÁN                 │
         │                            │
         │ 2. Tạo OTP từ secret       │
         │    + Device ID             │
         │                            │
         │ 3. Gửi giao dịch + OTP     │
         ├──────────────────────────>│
         │    (Transaction signature) │
         │                            │
         │                    4. Xác thực OTP
         │                    5. Kiểm tra Device ID
         │                    6. Thực hiện giao dịch
         │                            │
         │ 7. Kết quả giao dịch       │
         │<──────────────────────────┤
         │                            │
```

## API chính

### TransactionAuthManager

Quản lý xác thực giao dịch cho ứng dụng chứng khoán.

```dart
final authManager = TransactionAuthManager(
  serverUrl: 'https://your-server.com', // Tùy chọn
);

// Kiểm tra ủy quyền
await authManager.isDeviceAuthorized()

// Tạo OTP giao dịch
await authManager.generateTransactionOTP(secret)

// Xác thực giao dịch
await authManager.validateTransaction(secret: secret, otpCode: code)

// Tạo chữ ký giao dịch
await authManager.createTransactionSignature(...)

// Kích hoạt thiết bị
await authManager.requestDeviceActivation(...)
await authManager.activateDevice(...)
```

### DeviceManager

Quản lý thông tin và nhận diện thiết bị.

```dart
final deviceManager = DeviceManager();

// Lấy Device ID
final deviceId = await deviceManager.getDeviceId();

// Lấy thông tin thiết bị đầy đủ
final deviceInfo = await deviceManager.getDeviceInfo();

// Kiểm tra thiết bị có trong danh sách cho phép không
final isWhitelisted = await deviceManager.isCurrentDeviceWhitelisted();
```

### OtpManager

Tạo và xác thực mã OTP.

```dart
final otpManager = OtpManager();

// Tạo secret
final secret = otpManager.generateSecret();

// Tạo TOTP
final totp = otpManager.generateTotp(secret);

// Xác thực TOTP
final isValid = otpManager.validateTotp(secret, code);

// Lấy thời gian còn lại
final remaining = otpManager.getRemainingTime();
```

## Tích hợp Backend

SDK hỗ trợ tích hợp với backend API. Đặt tham số `serverUrl` khi tạo `TransactionAuthManager`:

```dart
final authManager = TransactionAuthManager(
  serverUrl: 'https://your-api-server.com',
);
```

### Các endpoint API cần thiết

Server backend cần cung cấp các endpoint sau:

- `POST /api/device/request-activation`: Yêu cầu kích hoạt thiết bị
- `POST /api/device/activate`: Kích hoạt thiết bị với mã OTP
- `GET /api/device/{deviceId}/status`: Lấy trạng thái kích hoạt thiết bị
- `GET /api/device/activated`: Lấy danh sách thiết bị đã kích hoạt
- `DELETE /api/device/{deviceId}`: Hủy kích hoạt thiết bị

Xem package NodeJS và .NET trong repository để triển khai backend.

## Bảo mật

### Các biện pháp bảo mật đã triển khai

- ✅ Sử dụng `Random.secure()` cho việc tạo Device ID và secret
- ✅ Mã OTP thay đổi mỗi 30 giây
- ✅ Xác thực với cửa sổ thời gian (±1 time step) để xử lý độ trễ
- ✅ Device ID được liên kết với mỗi giao dịch
- ✅ Lưu trữ cục bộ an toàn với SharedPreferences
- ✅ Kích hoạt OTP hết hạn sau 5 phút

### Khuyến nghị bảo mật

- 🔒 Lưu trữ user secret an toàn trên server (mã hóa database)
- 🔒 Sử dụng HTTPS cho tất cả giao tiếp API
- 🔒 Triển khai rate limiting trên các endpoint kích hoạt
- 🔒 Thêm xác thực/ủy quyền cho các endpoint quản lý thiết bị
- 🔒 Ghi log audit cho các lần kích hoạt thiết bị
- 🔒 Xem xét device certificate pinning
- 🔒 Xoay vòng activation secret định kỳ

## Xử lý lỗi thường gặp

### Lỗi 1: Thiết bị chưa kích hoạt

```dart
final isAuthorized = await authManager.isDeviceAuthorized();
if (!isAuthorized) {
  // Chuyển đến màn hình kích hoạt
  Navigator.push(context, MaterialPageRoute(
    builder: (context) => DeviceActivationScreen(),
  ));
}
```

### Lỗi 2: OTP hết hạn

```dart
final remainingTime = authManager.getOtpRemainingTime();
if (remainingTime < 5) {
  // Cảnh báo người dùng OTP sắp hết hạn
  showDialog(/* ... */);
}
```

### Lỗi 3: OTP không hợp lệ

```dart
final isValid = await authManager.validateTransaction(
  secret: secret,
  otpCode: userInput,
);

if (!isValid) {
  // Hiển thị thông báo lỗi
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text('Mã OTP không hợp lệ hoặc đã hết hạn')),
  );
}
```

## Câu hỏi thường gặp

### 1. Secret được lưu ở đâu?

Secret của người dùng nên được lưu trữ an toàn trên server backend, được mã hóa trong database. Ứng dụng mobile chỉ nhận secret khi cần tạo OTP.

### 2. OTP có tính năng gì đảm bảo an toàn?

- Mã OTP thay đổi mỗi 30 giây
- Mỗi OTP chỉ có thể sử dụng một lần
- OTP được liên kết với Device ID cụ thể
- Cửa sổ xác thực giới hạn (±30 giây)

### 3. Có thể sử dụng trên nhiều thiết bị không?

Có, mỗi thiết bị cần được kích hoạt riêng. Server sẽ quản lý danh sách các thiết bị được phép cho mỗi người dùng.

### 4. Làm sao nếu mất thiết bị?

Quản trị viên có thể hủy kích hoạt thiết bị bị mất qua giao diện web/admin bằng cách gọi API DELETE `/api/device/{deviceId}`.

### 5. SDK có hoạt động offline không?

SDK có thể tạo OTP offline nếu đã có secret, nhưng cần kết nối mạng để:
- Kích hoạt thiết bị lần đầu
- Gửi giao dịch lên server để xác thực

## Hỗ trợ

- **Tài liệu**: Xem file README.md chính và các ví dụ
- **Mã nguồn**: [GitHub Repository](https://github.com/TBCF-Company/2FAPrivate)
- **Issues**: Báo cáo lỗi qua GitHub Issues

## Giấy phép

AGPL-3.0-or-later

## Đóng góp

Chúng tôi hoan nghênh mọi đóng góp! Vui lòng gửi Pull Request hoặc mở Issue trên GitHub.
