# PrivacyIDEA Server - Phiên bản .NET Core 8

Đây là phiên bản .NET Core 8 được chuyển đổi từ ứng dụng máy chủ Python privacyIDEA trong thư mục `privacyidea`. Nó triển khai cùng chức năng máy chủ xác thực hai yếu tố (Two-Factor Authentication) sử dụng ASP.NET Core.

## Tổng quan

Ứng dụng này cung cấp REST API để quản lý cấu hình máy chủ privacyIDEA từ xa. Nó cho phép bạn:
- Tạo và cập nhật định nghĩa máy chủ privacyIDEA
- Liệt kê tất cả cấu hình máy chủ
- Xóa cấu hình máy chủ
- Kiểm tra kết nối máy chủ

## Các thành phần

### Models (Mô hình)
- **PrivacyIDEAServerDB**: Mô hình cơ sở dữ liệu cho cấu hình máy chủ
- **PrivacyIDEAContext**: Entity Framework DbContext cho các thao tác cơ sở dữ liệu

### Library (Thư viện)
- **PrivacyIDEAServer**: Class cốt lõi cho các thao tác máy chủ và xác thực
- **IPrivacyIDEAServerService**: Interface của service
- **PrivacyIDEAServerService**: Triển khai service cho các thao tác CRUD

### Controllers (Bộ điều khiển)
- **PrivacyIDEAServerController**: Các endpoint REST API

## API Endpoints

Tất cả các endpoint tuân theo cấu trúc giống như ứng dụng Python Flask:

### Liệt kê Máy chủ
```
GET /privacyideaserver
```
Trả về danh sách tất cả các máy chủ privacyIDEA đã cấu hình.

### Tạo/Cập nhật Máy chủ
```
POST /privacyideaserver/{identifier}
Content-Type: application/json

{
  "url": "https://server.example.com",
  "tls": true,
  "description": "Máy chủ xác thực chính"
}
```

### Xóa Máy chủ
```
DELETE /privacyideaserver/{identifier}
```

### Kiểm tra Kết nối Máy chủ
```
POST /privacyideaserver/test_request
Content-Type: application/json

{
  "identifier": "test-server",
  "url": "https://server.example.com",
  "tls": true,
  "username": "testuser",
  "password": "testpass123"
}
```

## Chạy Ứng dụng

### Yêu cầu
- .NET 8.0 SDK hoặc mới hơn

### Build
```bash
cd NetCore/PrivacyIdeaServer
dotnet restore
dotnet build
```

### Chạy
```bash
dotnet run
```

Ứng dụng sẽ khởi động trên `https://localhost:5001` (HTTPS) và `http://localhost:5000` (HTTP).

### Truy cập Swagger UI
Điều hướng đến `https://localhost:5001/swagger` để xem tài liệu API tương tác.

## Cơ sở dữ liệu

Ứng dụng sử dụng SQLite theo mặc định để đơn giản. File cơ sở dữ liệu `privacyidea.db` sẽ được tạo tự động trong thư mục ứng dụng.

Để sử dụng cơ sở dữ liệu khác (SQL Server, PostgreSQL, MySQL, v.v.):
1. Cập nhật connection string trong `appsettings.json`
2. Cài đặt gói NuGet provider Entity Framework phù hợp
3. Cập nhật lệnh gọi `UseSqlite()` trong `Program.cs` để sử dụng provider đúng

## Cấu hình

Cấu hình được lưu trong `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=privacyidea.db"
  }
}
```

## Giấy phép

Mã này được cấp phép theo GNU AFFERO GENERAL PUBLIC LICENSE Version 3 hoặc mới hơn (AGPL-3.0-or-later), khớp với dự án Python privacyIDEA gốc.

## Mã Python gốc

Đây là phiên bản chuyển đổi của mã máy chủ privacyIDEA nằm tại:
- `privacyidea/privacyidea/lib/privacyideaserver.py` - Các hàm thư viện cốt lõi
- `privacyidea/privacyidea/api/privacyideaserver.py` - Các endpoint REST API
- `privacyidea/privacyidea/models.py` - Mô hình cơ sở dữ liệu

Chức năng đã được bảo toàn gần nhất có thể trong khi thích ứng với các thành ngữ và thực hành tốt nhất của .NET Core.
