# Ví dụ Nghiệp vụ với Xác thực 2FA / Business Operation with 2FA Example

## Tổng quan / Overview

Đây là một ví dụ hoàn chỉnh về cách tích hợp xác thực hai yếu tố (2FA) vào một ứng dụng nghiệp vụ thực tế. Ví dụ này mô phỏng một ứng dụng ngân hàng với chức năng chuyển tiền, yêu cầu xác thực 2FA trước khi thực hiện giao dịch.

This is a complete example of how to integrate two-factor authentication (2FA) into a real business application. This example simulates a banking application with money transfer functionality that requires 2FA authentication before executing transactions.

## Kiến trúc / Architecture

### Components

```
┌─────────────────────────────────────────────────────────────┐
│                   Business Application                      │
│                  (BlazorWebApp)                             │
│  ┌───────────────────────────────────────────────────┐     │
│  │  BusinessOperationExample.razor                   │     │
│  │  - Money Transfer UI                              │     │
│  │  - 2FA Enrollment Flow                            │     │
│  │  - OTP Verification                               │     │
│  └─────────────────┬─────────────────────────────────┘     │
│                    │ HTTP API Calls                         │
│                    │                                        │
└────────────────────┼────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              2FA Authentication Server                       │
│              (PrivacyIdeaServer)                            │
│  ┌───────────────────────────────────────────────────┐     │
│  │  OTP API Controller                               │     │
│  │  - /api/otp/enroll    - Generate QR Code          │     │
│  │  - /api/otp/validate  - Verify OTP Code           │     │
│  │  - /api/otp/generate  - Generate Current OTP      │     │
│  └─────────────────┬─────────────────────────────────┘     │
│                    │                                        │
│  ┌─────────────────▼─────────────────────────────────┐     │
│  │  OtpTokenService                                  │     │
│  │  - Secret Generation                              │     │
│  │  - TOTP/HOTP Generation                           │     │
│  │  - OTP Validation                                 │     │
│  └───────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
                     ▲
                     │ Scan QR Code
                     │ Generate OTP
                     │
┌─────────────────────────────────────────────────────────────┐
│          Mobile Authenticator App                           │
│          (Flutter App or Google Authenticator)              │
│  - Scan QR Code                                             │
│  - Store Secret Key                                         │
│  - Generate 6-digit OTP every 30 seconds                    │
└─────────────────────────────────────────────────────────────┘
```

## Luồng hoạt động / Workflow

### 1. Thiết lập 2FA / 2FA Setup

**Vietnamese:**
1. Người dùng truy cập trang "Business Example"
2. Hệ thống kiểm tra xem người dùng đã thiết lập 2FA chưa
3. Nếu chưa, hiển thị nút "Thiết lập 2FA"
4. Người dùng nhập thông tin tài khoản và tên ứng dụng
5. Hệ thống gọi API `/api/otp/enroll` để tạo:
   - Secret key (khóa bí mật)
   - QR Code (mã QR)
   - Provisioning URI
6. Người dùng quét mã QR bằng ứng dụng Authenticator (Google Authenticator, Microsoft Authenticator, etc.)
7. Ứng dụng Authenticator lưu trữ secret key và bắt đầu tạo mã OTP

**English:**
1. User accesses "Business Example" page
2. System checks if user has set up 2FA
3. If not, displays "Set up 2FA" button
4. User enters account information and application name
5. System calls `/api/otp/enroll` API to generate:
   - Secret key
   - QR Code
   - Provisioning URI
6. User scans QR code with Authenticator app (Google Authenticator, Microsoft Authenticator, etc.)
7. Authenticator app stores secret key and starts generating OTP codes

### 2. Thực hiện Nghiệp vụ / Business Operation Execution

**Vietnamese:**
1. Người dùng điền thông tin giao dịch (tài khoản nguồn, đích, số tiền)
2. Người dùng nhấn nút "Tiếp tục"
3. Hệ thống hiển thị màn hình xác thực 2FA
4. Người dùng mở ứng dụng Authenticator và xem mã OTP 6 chữ số
5. Người dùng nhập mã OTP vào hệ thống
6. Hệ thống gọi API `/api/otp/validate` để xác thực mã OTP
7. Nếu mã OTP đúng:
   - Giao dịch được thực hiện
   - Hiển thị thông báo thành công và chi tiết giao dịch
8. Nếu mã OTP sai:
   - Hiển thị thông báo lỗi
   - Cho phép người dùng nhập lại

**English:**
1. User fills in transaction details (source account, destination, amount)
2. User clicks "Continue" button
3. System displays 2FA authentication screen
4. User opens Authenticator app and views 6-digit OTP code
5. User enters OTP code into the system
6. System calls `/api/otp/validate` API to verify OTP code
7. If OTP code is correct:
   - Transaction is executed
   - Success message and transaction details are displayed
8. If OTP code is incorrect:
   - Error message is displayed
   - User can try again

## API Endpoints

### 1. Enroll OTP Token

**Endpoint:** `POST /api/otp/enroll`

**Request:**
```json
{
  "username": "user@banking.com",
  "issuer": "Banking App 2FA",
  "isTotp": true,
  "digits": 6,
  "step": 30
}
```

**Response:**
```json
{
  "secret": "JBSWY3DPEHPK3PXP",
  "provisioningUri": "otpauth://totp/Banking%20App%202FA:user@banking.com?secret=JBSWY3DPEHPK3PXP&issuer=Banking%20App%202FA&algorithm=SHA1&digits=6&period=30",
  "qrCodeDataUrl": "otpauth://totp/Banking%20App%202FA:user@banking.com?secret=JBSWY3DPEHPK3PXP&issuer=Banking%20App%202FA&algorithm=SHA1&digits=6&period=30"
}
```

### 2. Validate OTP Code

**Endpoint:** `POST /api/otp/validate`

**Request:**
```json
{
  "secret": "JBSWY3DPEHPK3PXP",
  "code": "123456",
  "isTotp": true,
  "digits": 6,
  "step": 30
}
```

**Response:**
```json
{
  "isValid": true,
  "message": "OTP code is valid"
}
```

### 3. Generate Current OTP (for testing)

**Endpoint:** `POST /api/otp/generate`

**Request:**
```json
{
  "secret": "JBSWY3DPEHPK3PXP",
  "isTotp": true,
  "digits": 6,
  "step": 30
}
```

**Response:**
```json
{
  "code": "123456",
  "timestamp": "2026-02-06T13:51:23.552Z"
}
```

## Cài đặt và Chạy / Installation and Running

### Bước 1: Chạy PrivacyIdeaServer (2FA API)

```bash
cd NetCore/PrivacyIdeaServer
dotnet restore
dotnet build
dotnet run
```

Server sẽ chạy tại / Server will run at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

Swagger UI: `https://localhost:5001/swagger`

### Bước 2: Chạy BlazorWebApp (Business Application)

```bash
cd NetCore/BlazorWebApp
dotnet restore
dotnet build
dotnet run
```

App sẽ chạy tại / App will run at:
- HTTPS: `https://localhost:7001` (hoặc/or check console output)
- HTTP: `http://localhost:5000`

### Bước 3: Sử dụng / Usage

1. Mở trình duyệt và truy cập BlazorWebApp / Open browser and access BlazorWebApp
2. Click vào menu "Business Example"
3. Làm theo hướng dẫn trên trang / Follow the instructions on the page

## Mobile Authenticator Apps

Bạn có thể sử dụng các ứng dụng sau để quét mã QR / You can use the following apps to scan QR code:

### iOS:
- Google Authenticator
- Microsoft Authenticator
- Authy
- 1Password

### Android:
- Google Authenticator
- Microsoft Authenticator
- Authy
- andOTP
- Flutter App (trong repo này / in this repo)

### Flutter App trong Repo này / Flutter App in this Repo

```bash
cd flutter
flutter pub get
flutter run
```

## Tính năng Bảo mật / Security Features

### 1. TOTP (Time-based One-Time Password)
- Mã OTP thay đổi mỗi 30 giây / OTP code changes every 30 seconds
- Sử dụng thuật toán SHA-1 / Uses SHA-1 algorithm
- 6 chữ số / 6 digits

### 2. Time Window
- Cho phép sai lệch thời gian ±1 step (±30 giây) / Allows time drift of ±1 step (±30 seconds)
- Giúp xử lý các trường hợp đồng hồ không đồng bộ / Handles clock synchronization issues

### 3. Secret Key Storage
- Secret key được tạo ngẫu nhiên / Secret key is randomly generated
- Độ dài 20 bytes (160 bits) / Length of 20 bytes (160 bits)
- Mã hóa Base32 / Base32 encoded

### 4. Transaction Verification
- Hiển thị thông tin giao dịch trước khi xác thực / Shows transaction details before authentication
- Yêu cầu mã OTP mới cho mỗi giao dịch / Requires fresh OTP for each transaction
- Không cho phép thực hiện giao dịch khi OTP sai / Prevents transaction execution with invalid OTP

## Lưu ý khi Triển khai Thực tế / Production Deployment Notes

### 1. Secret Key Storage
**Không nên:**
- Lưu secret key trong session hoặc local storage
- Truyền secret key qua URL

**Nên:**
- Lưu secret key trong database (mã hóa)
- Liên kết secret key với user ID
- Sử dụng HTTPS cho mọi API call

### 2. Rate Limiting
- Giới hạn số lần thử OTP sai (ví dụ: 5 lần / 15 phút)
- Khóa tài khoản tạm thời sau nhiều lần thử sai

### 3. Audit Logging
- Ghi log mọi lần đăng ký 2FA
- Ghi log mọi lần xác thực OTP (thành công và thất bại)
- Ghi log mọi giao dịch quan trọng

### 4. Backup Codes
- Tạo mã backup khi người dùng thiết lập 2FA
- Cho phép người dùng khôi phục tài khoản khi mất thiết bị

### 5. Multi-factor Options
- Hỗ trợ nhiều phương thức 2FA (SMS, Email, Hardware tokens)
- Cho phép người dùng chọn phương thức ưu tiên

## Tài liệu Tham khảo / References

- [RFC 6238 - TOTP: Time-Based One-Time Password Algorithm](https://tools.ietf.org/html/rfc6238)
- [RFC 4226 - HOTP: An HMAC-Based One-Time Password Algorithm](https://tools.ietf.org/html/rfc4226)
- [Google Authenticator Key URI Format](https://github.com/google/google-authenticator/wiki/Key-Uri-Format)
- [OtpNet Library](https://github.com/kspearrin/Otp.NET)

## License

AGPL-3.0-or-later

## Tác giả / Author

Converted from Python privacyIDEA to .NET/Blazor
