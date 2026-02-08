# Tài liệu Tiếng Việt - Thư mục NetCore

Tài liệu này liệt kê tất cả các tài liệu tiếng Việt có sẵn trong thư mục NetCore của dự án PrivacyIDEA.

## 📚 Tài liệu Đã Dịch

### Tài liệu Chính

| File Tiếng Anh | File Tiếng Việt | Mô tả |
|----------------|-----------------|-------|
| README.md | README_VI.md | Hướng dẫn tổng quan về PrivacyIDEA Server .NET Core 8 |
| ARCHITECTURE.md | KIẾN_TRÚC.md | Sơ đồ và mô tả kiến trúc hệ thống |
| SUMMARY.md | TÓM_TẮT_HOÀN_THÀNH.md | Tóm tắt dự án (song ngữ) |

### Hướng dẫn Chuyển đổi

| File Tiếng Anh | File Tiếng Việt | Mô tả |
|----------------|-----------------|-------|
| COMPLETE_CONVERSION_GUIDE.md | HƯỚNG_DẪN_CHUYỂN_ĐỔI_HOÀN_CHỈNH.md | Hướng dẫn toàn diện về chuyển đổi Python sang C# |
| CONVERSION_GUIDE.md | HƯỚNG_DẪN_ÁNH_XẠ_CHUYỂN_ĐỔI.md | Ánh xạ chi tiết giữa mã Python và .NET Core |
| PYTHON_TO_CSHARP_CONVERSION_GUIDE.md | HƯỚNG_DẪN_CHUYỂN_ĐỔI_PYTHON_SANG_CSHARP.md | Hướng dẫn chuyển đổi từ Python sang C# |

### Hướng dẫn Khởi động Nhanh

| File Tiếng Anh | File Tiếng Việt | Mô tả |
|----------------|-----------------|-------|
| AUTH_REALM_QUICK_START.md | KHỞI_ĐỘNG_NHANH_AUTH_REALM.md | Hướng dẫn nhanh về xác thực và quản lý Realm |
| RESOLVER_QUICK_START.md | KHỞI_ĐỘNG_NHANH_RESOLVER.md | Hướng dẫn nhanh về sử dụng Resolvers |
| UTILS_QUICK_START.md | KHỞI_ĐỘNG_NHANH_UTILS.md | Hướng dẫn nhanh về các tiện ích đã chuyển đổi |

### Hướng dẫn Triển khai

| File Tiếng Anh | File Tiếng Việt | Mô tả |
|----------------|-----------------|-------|
| POSTGRESQL_DEPLOYMENT.md | TRIỂN_KHAI_POSTGRESQL.md | Hướng dẫn triển khai với PostgreSQL |

### Báo cáo

| File Tiếng Anh | File Tiếng Việt | Mô tả |
|----------------|-----------------|-------|
| - | BÁO_CÁO_CHUYỂN_ĐỔI.md | Báo cáo chi tiết về quá trình chuyển đổi |
| - | BÁO_CÁO_HOÀN_THÀNH_CHUYỂN_ĐỔI.md | Báo cáo hoàn thành chuyển đổi |

## 🚀 Bắt đầu Nhanh

### Nếu bạn mới bắt đầu
1. Đọc [README_VI.md](README_VI.md) để hiểu tổng quan về dự án
2. Xem [KIẾN_TRÚC.md](KIẾN_TRÚC.md) để hiểu kiến trúc hệ thống
3. Đọc [HƯỚNG_DẪN_CHUYỂN_ĐỔI_HOÀN_CHỈNH.md](HƯỚNG_DẪN_CHUYỂN_ĐỔI_HOÀN_CHỈNH.md) nếu muốn hiểu quá trình chuyển đổi

### Nếu bạn muốn triển khai
1. Đọc [TRIỂN_KHAI_POSTGRESQL.md](TRIỂN_KHAI_POSTGRESQL.md) để thiết lập cơ sở dữ liệu
2. Xem các hướng dẫn khởi động nhanh tùy theo nhu cầu:
   - [KHỞI_ĐỘNG_NHANH_AUTH_REALM.md](KHỞI_ĐỘNG_NHANH_AUTH_REALM.md) - Xác thực và Realm
   - [KHỞI_ĐỘNG_NHANH_RESOLVER.md](KHỞI_ĐỘNG_NHANH_RESOLVER.md) - Resolvers
   - [KHỞI_ĐỘNG_NHANH_UTILS.md](KHỞI_ĐỘNG_NHANH_UTILS.md) - Tiện ích

### Nếu bạn là nhà phát triển
1. Đọc [HƯỚNG_DẪN_ÁNH_XẠ_CHUYỂN_ĐỔI.md](HƯỚNG_DẪN_ÁNH_XẠ_CHUYỂN_ĐỔI.md) để hiểu ánh xạ mã
2. Tham khảo các báo cáo chuyển đổi:
   - [BÁO_CÁO_CHUYỂN_ĐỔI.md](BÁO_CÁO_CHUYỂN_ĐỔI.md)
   - [BÁO_CÁO_HOÀN_THÀNH_CHUYỂN_ĐỔI.md](BÁO_CÁO_HOÀN_THÀNH_CHUYỂN_ĐỔI.md)

## 📖 Nội dung Chi tiết

### README_VI.md
- Tổng quan về PrivacyIDEA Server .NET Core 8
- Các thành phần của hệ thống
- API Endpoints
- Hướng dẫn chạy ứng dụng
- Cấu hình cơ sở dữ liệu

### KIẾN_TRÚC.md
- Sơ đồ kiến trúc hệ thống
- Trách nhiệm của các thành phần
- Luồng dữ liệu
- Ngăn xếp công nghệ
- So sánh với phiên bản Python

### HƯỚNG_DẪN_CHUYỂN_ĐỔI_HOÀN_CHỈNH.md
- Chiến lược chuyển đổi từng giai đoạn
- Ánh xạ thư viện Python → C#/.NET
- Mẫu chuyển đổi mã
- Sự khác biệt chính giữa Python và C#
- Các thực hành tốt nhất
- Checklist chuyển đổi

### HƯỚNG_DẪN_ÁNH_XẠ_CHUYỂN_ĐỔI.md
- Ánh xạ file giữa Python và .NET Core
- So sánh mã chi tiết
- Sự khác biệt về đặc điểm ngôn ngữ
- Sự khác biệt về framework
- Tương thích API

### KHỞI_ĐỘNG_NHANH_AUTH_REALM.md
- Các file đã chuyển đổi
- Yêu cầu package
- Cấu hình
- Ví dụ sử dụng cho xác thực Admin, cache, Realm, và Resolver
- Bảo mật và testing
- Triển khai production

### KHỞI_ĐỘNG_NHANH_RESOLVER.md
- Các loại Resolver có sẵn (Passwd, SQL, LDAP, HTTP, SCIM, Entra ID, Keycloak)
- Các thao tác thông dụng
- Cấu hình trong ASP.NET Core
- Ánh xạ thuộc tính
- Các thực hành tốt nhất
- Testing và xử lý sự cố

### KHỞI_ĐỘNG_NHANH_UTILS.md
- Mã hóa chuỗi
- Tiện ích thời gian
- Xử lý mạng & IP
- Xác thực
- Tạo mã QR
- Tiện ích Token
- Hash & Mã hóa
- Tiện ích File & Path
- Xử lý JSON
- Tiện ích HTTP
- Tích hợp với ASP.NET Core

### TRIỂN_KHAI_POSTGRESQL.md
- Cài đặt PostgreSQL
- Tạo cơ sở dữ liệu và người dùng
- Cấu hình connection string
- Quản lý migration
- Sao lưu và khôi phục
- Tối ưu hiệu suất
- Giám sát
- Bảo mật
- Xử lý sự cố

### BÁO_CÁO_CHUYỂN_ĐỔI.md
- Tổng quan dự án chuyển đổi
- Những gì đã hoàn thành
- Database Models đã chuyển đổi
- Core Libraries đã chuyển đổi
- NuGet packages đã thêm
- Chi tiết từng giai đoạn

### BÁO_CÁO_HOÀN_THÀNH_CHUYỂN_ĐỔI.md
- Tóm tắt hoàn thành
- Các thành phần đã chuyển đổi
- Testing và validation
- Các bước tiếp theo

## 🔧 Công nghệ Sử dụng

- **.NET Core**: 8.0
- **Ngôn ngữ**: C# 12
- **Database ORM**: Entity Framework Core 8.0
- **Database**: PostgreSQL, SQLite (có thể thay đổi)
- **API**: ASP.NET Core Web API
- **Documentation**: Swagger/OpenAPI

## 📝 Thuật ngữ Kỹ thuật

| Tiếng Anh | Tiếng Việt |
|-----------|------------|
| Authentication | Xác thực |
| Authorization | Ủy quyền |
| Resolver | Resolver (giữ nguyên) |
| Realm | Realm (giữ nguyên) |
| Token | Token (giữ nguyên) |
| Database | Cơ sở dữ liệu |
| Migration | Migration (giữ nguyên) |
| Controller | Bộ điều khiển/Controller |
| Service | Service/Dịch vụ |
| Model | Mô hình |
| Endpoint | Endpoint (giữ nguyên) |
| Configuration | Cấu hình |
| Deployment | Triển khai |

## 🤝 Đóng góp

Nếu bạn muốn đóng góp vào tài liệu tiếng Việt:
1. Kiểm tra các file tài liệu hiện có
2. Đảm bảo thuật ngữ kỹ thuật nhất quán
3. Giữ cấu trúc tương tự như bản tiếng Anh
4. Sử dụng ví dụ phù hợp với ngữ cảnh Việt Nam khi có thể

## 📞 Liên hệ & Hỗ trợ

Nếu bạn có câu hỏi hoặc cần hỗ trợ:
- Xem tài liệu tiếng Anh tương ứng để biết thêm chi tiết
- Kiểm tra các ví dụ code trong tài liệu
- Tham khảo các file implementation trong thư mục source code

## 📄 Giấy phép

Tất cả tài liệu tuân theo giấy phép AGPL-3.0-or-later, giống như dự án gốc.

---

**Lưu ý**: Tài liệu tiếng Việt được dịch từ tài liệu tiếng Anh gốc. Nếu có sự khác biệt, tài liệu tiếng Anh được ưu tiên.
