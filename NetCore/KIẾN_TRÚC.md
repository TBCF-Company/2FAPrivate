# Sơ đồ Kiến trúc

## Kiến trúc Ứng dụng .NET Core 8

```
┌─────────────────────────────────────────────────────────────────┐
│                     PrivacyIDEA Server API                       │
│                      (.NET Core 8)                               │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                        HTTP Endpoints                            │
├─────────────────────────────────────────────────────────────────┤
│  GET    /privacyideaserver                                      │
│  POST   /privacyideaserver/{identifier}                         │
│  DELETE /privacyideaserver/{identifier}                         │
│  POST   /privacyideaserver/test_request                         │
│  GET    /info                                                    │
│  GET    /healthz                                                 │
│  GET    /swagger                                                 │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│              PrivacyIDEAServerController                         │
│                    (Tầng API)                                    │
├─────────────────────────────────────────────────────────────────┤
│  - Tạo/Cập nhật cấu hình máy chủ                                │
│  - Liệt kê tất cả máy chủ                                       │
│  - Xóa máy chủ                                                  │
│  - Kiểm tra kết nối máy chủ                                     │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│           IPrivacyIDEAServerService                             │
│              (Tầng Logic Nghiệp vụ)                             │
├─────────────────────────────────────────────────────────────────┤
│  Triển khai: PrivacyIDEAServerService                           │
│                                                                  │
│  - ListPrivacyIDEAServersAsync()                               │
│  - GetPrivacyIDEAServerAsync()                                 │
│  - AddPrivacyIDEAServerAsync()                                 │
│  - DeletePrivacyIDEAServerAsync()                              │
└─────────────────────────────────────────────────────────────────┘
                     │                      │
                     │                      │
        ┌────────────┘                      └────────────┐
        ▼                                                 ▼
┌─────────────────────────┐              ┌────────────────────────┐
│   PrivacyIDEAServer     │              │  PrivacyIDEAContext    │
│   (Logic Xác thực)      │              │  (Entity Framework)    │
├─────────────────────────┤              ├────────────────────────┤
│ - ValidateCheckAsync()  │              │ DbSet<Server>          │
│ - RequestAsync()        │              │ SaveChangesAsync()     │
└─────────────────────────┘              └────────────────────────┘
        │                                                 │
        ▼                                                 ▼
┌─────────────────────────┐              ┌────────────────────────┐
│  HttpClient/Factory     │              │  SQLite Database       │
│  (Request Ngoài)        │              │  (privacyidea.db)      │
├─────────────────────────┤              ├────────────────────────┤
│ POST /validate/check    │              │ Bảng:                  │
│ tới máy chủ từ xa       │              │  privacyideaserver     │
└─────────────────────────┘              └────────────────────────┘
```

## Trách nhiệm của các Thành phần

### Tầng Controllers
- **PrivacyIDEAServerController**: Xử lý các request và response HTTP
  - Xác thực request
  - Định dạng response
  - Xử lý lỗi
  - Logging

### Tầng Service
- **IPrivacyIDEAServerService**: Interface cho logic nghiệp vụ
- **PrivacyIDEAServerService**: Triển khai các thao tác CRUD
  - Các thao tác cơ sở dữ liệu
  - Quy tắc nghiệp vụ
  - Chuyển đổi dữ liệu

### Tầng Library
- **PrivacyIDEAServer**: Logic xác thực và giao tiếp cốt lõi
  - Request HTTP tới máy chủ từ xa
  - Xác thực chứng chỉ (TLS)
  - Phân tích response

### Tầng Dữ liệu
- **PrivacyIDEAContext**: EF Core DbContext
  - Quản lý kết nối cơ sở dữ liệu
  - Theo dõi thay đổi
  - Migration
- **PrivacyIDEAServerDB**: Mô hình cơ sở dữ liệu
  - Định nghĩa Entity
  - Quy tắc xác thực

## Luồng Dữ liệu

### Tạo một Máy chủ
```
Request từ Client
    ↓
Controller xác thực request
    ↓
Service tạo/cập nhật entity
    ↓
DbContext lưu vào cơ sở dữ liệu
    ↓
Controller trả về response cho client
```

### Kiểm tra Kết nối Máy chủ
```
Request từ Client
    ↓
Controller nhận tham số test
    ↓
PrivacyIDEAServer.RequestAsync()
    ↓
HttpClient gửi POST tới máy chủ từ xa
    ↓
Phân tích response
    ↓
Trả về thành công/thất bại cho client
```

## Ngăn xếp Công nghệ

| Tầng | Công nghệ |
|-------|-----------|
| Framework | ASP.NET Core 8.0 |
| Ngôn ngữ | C# 12 |
| ORM | Entity Framework Core 8.0 |
| Cơ sở dữ liệu | SQLite (có thể cấu hình) |
| Tài liệu API | Swagger/OpenAPI |
| HTTP Client | IHttpClientFactory |
| DI Container | ASP.NET Core DI tích hợp |
| Logging | ILogger / Microsoft.Extensions.Logging |

## So sánh với Phiên bản Python

| Thành phần | Python | .NET Core |
|-----------|--------|-----------|
| Web Framework | Flask | ASP.NET Core |
| ORM | SQLAlchemy | Entity Framework Core |
| HTTP Client | requests | HttpClient |
| Dependency Injection | Manual/Flask-Injector | Tích hợp sẵn |
| Tài liệu API | Manual/Swagger | Swagger/OpenAPI (tích hợp) |
| Hỗ trợ Async | asyncio (tùy chọn) | async/await (native) |
| An toàn Kiểu | Dynamic typing | Static typing |
