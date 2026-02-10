# Stock Trading 2FA Example

Ví dụ ứng dụng giao dịch chứng khoán sử dụng SDK xác thực 2 yếu tố (2FA).

## Tính năng

- ✅ Kích hoạt thiết bị với OTP
- ✅ Tạo mã OTP tự động cho giao dịch
- ✅ Hiển thị đếm ngược thời gian OTP
- ✅ Xác thực giao dịch MUA/BÁN chứng khoán
- ✅ Tạo chữ ký giao dịch với Device ID
- ✅ Giao diện tiếng Việt

## Chạy ví dụ

```bash
# Di chuyển đến thư mục example
cd example

# Chạy ứng dụng
flutter run stock_trading_example.dart
```

## Cấu hình

Mở file `stock_trading_example.dart` và cập nhật URL server:

```dart
final _authManager = TransactionAuthManager(
  serverUrl: 'https://your-2fa-server.com', // Thay đổi URL này
);
```

Nếu không có server, SDK sẽ hoạt động ở chế độ local (không cần server).

## Luồng sử dụng

### 1. Kích hoạt thiết bị lần đầu

1. Mở ứng dụng
2. Nhập tên đăng nhập
3. Nhấn "Yêu cầu kích hoạt"
4. Nhập mã OTP được hiển thị (trong demo, mã sẽ hiển thị trực tiếp)
5. Nhấn "Kích hoạt thiết bị"

### 2. Thực hiện giao dịch

1. Sau khi kích hoạt, màn hình giao dịch sẽ hiển thị
2. Mã OTP sẽ tự động làm mới mỗi 30 giây
3. Nhập mã chứng khoán (VD: VNM, FPT, HPG)
4. Nhập số lượng cổ phiếu
5. Nhấn "MUA" hoặc "BÁN"
6. Xem thông tin giao dịch và chữ ký

## Màn hình

### Màn hình 1: Kích hoạt thiết bị

```
┌─────────────────────────────────┐
│     Xác thực thiết bị           │
│                                 │
│  [Tên đăng nhập: _________]    │
│                                 │
│  [Yêu cầu kích hoạt]            │
│                                 │
│  Mã OTP: 123456                 │
│  [Nhập mã OTP: _________]       │
│                                 │
│  [Kích hoạt thiết bị]           │
└─────────────────────────────────┘
```

### Màn hình 2: Giao dịch chứng khoán

```
┌─────────────────────────────────┐
│  Giao dịch chứng khoán     [i]  │
│                                 │
│  ┌─────────────────────────────┐│
│  │ Mã xác thực giao dịch (OTP) ││
│  │        654321               ││
│  │ ████████░░░░░░░░ 20s        ││
│  └─────────────────────────────┘│
│                                 │
│  Thông tin giao dịch            │
│  [Mã chứng khoán: VNM_____]    │
│  [Số lượng: 100___________]    │
│                                 │
│  [   MUA   ] [   BÁN   ]       │
│                                 │
│  ⚠ Lưu ý bảo mật                │
│  • Mã OTP thay đổi mỗi 30 giây │
│  • Mỗi giao dịch được xác thực  │
└─────────────────────────────────┘
```

## Tích hợp vào ứng dụng thực tế

### Bước 1: Thêm dependency

Thêm vào `pubspec.yaml`:

```yaml
dependencies:
  two_factor_auth_sdk:
    path: ../flutter_sdk/two_factor_auth_sdk
```

### Bước 2: Import SDK

```dart
import 'package:two_factor_auth_sdk/two_factor_auth_sdk.dart';
```

### Bước 3: Khởi tạo TransactionAuthManager

```dart
final authManager = TransactionAuthManager(
  serverUrl: 'https://your-api.com',
);
```

### Bước 4: Kiểm tra ủy quyền

```dart
final isAuthorized = await authManager.isDeviceAuthorized();
if (!isAuthorized) {
  // Hiển thị màn hình kích hoạt
}
```

### Bước 5: Xác thực giao dịch

```dart
// Tạo chữ ký giao dịch
final signature = await authManager.createTransactionSignature(
  secret: userSecret,
  transactionId: 'TXN_001',
  transactionType: 'MUA',
  amount: 1000.0,
);

// Gửi lên server
await api.executeTransaction(signature);
```

## Backend Integration

Ứng dụng này cần backend API để xác thực giao dịch. Repository cung cấp 2 package backend:

1. **.NET Core**: `NetCore/TwoFactorAuth.Core`
2. **NodeJS**: `nodejs_package/two-factor-auth`

Xem tài liệu trong các thư mục tương ứng để triển khai backend.

## Lưu ý

- Trong môi trường production, secret không được hardcode mà phải lấy từ server an toàn
- Nên thêm biometric authentication (vân tay, Face ID) cho bảo mật tăng cường
- Kiểm tra kết nối mạng trước khi thực hiện giao dịch
- Log tất cả các giao dịch để audit

## Tài liệu liên quan

- [README.md](../README.md) - Tài liệu SDK chính (English)
- [README_VI.md](../README_VI.md) - Tài liệu SDK (Tiếng Việt)
- [DEVICE_MANAGEMENT_README.md](../../DEVICE_MANAGEMENT_README.md) - Kiến trúc hệ thống

## License

AGPL-3.0-or-later
