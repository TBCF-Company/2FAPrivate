# Hướng dẫn Triển khai PostgreSQL

## Yêu cầu Trước

- .NET 8.0 SDK hoặc mới hơn
- PostgreSQL 12 hoặc mới hơn
- Kiến thức cơ bản về ASP.NET Core và PostgreSQL

## Khởi động Nhanh

### 1. Cài đặt PostgreSQL

#### Ubuntu/Debian:
```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

#### macOS (sử dụng Homebrew):
```bash
brew install postgresql@16
brew services start postgresql@16
```

#### Windows:
Tải và cài đặt từ https://www.postgresql.org/download/windows/

### 2. Tạo Cơ sở dữ liệu và Người dùng

```bash
# Chuyển sang người dùng postgres
sudo -u postgres psql

# Trong console psql:
CREATE DATABASE privacyidea;
CREATE USER privacyidea_user WITH ENCRYPTED PASSWORD 'mật_khẩu_bảo_mật_của_bạn';
GRANT ALL PRIVILEGES ON DATABASE privacyidea TO privacyidea_user;

# Cấp quyền schema (PostgreSQL 15+)
\c privacyidea
GRANT ALL ON SCHEMA public TO privacyidea_user;

\q
```

### 3. Cấu hình Connection String

**Tùy chọn A: Biến Môi trường (Khuyến nghị cho Production)**

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=mật_khẩu_bảo_mật_của_bạn;Port=5432"
```

**Tùy chọn B: User Secrets (Development)**

```bash
cd NetCore/PrivacyIdeaServer
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=mật_khẩu_bảo_mật_của_bạn;Port=5432"
```

**Tùy chọn C: appsettings.json (Không Khuyến nghị)**

Chỉnh sửa `appsettings.json` và cập nhật connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=mật_khẩu_bảo_mật_của_bạn;Port=5432"
  }
}
```

⚠️ **Cảnh báo Bảo mật**: Không bao giờ commit thông tin xác thực vào version control!

### 4. Cài đặt EF Core Tools (nếu chưa cài đặt)

```bash
dotnet tool install --global dotnet-ef
```

### 5. Tạo và Áp dụng Migrations

```bash
cd NetCore/PrivacyIdeaServer

# Tạo migration ban đầu
dotnet ef migrations add InitialCreate

# Xem lại migration đã tạo trong Migrations/
# Sau đó áp dụng nó:
dotnet ef database update
```

### 6. Chạy Ứng dụng

```bash
dotnet run
```

Ứng dụng sẽ khởi động trên:
- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000
- **Swagger UI**: https://localhost:5001/swagger

## Cấu hình Nâng cao

### SSL/TLS Connection

Để kết nối với PostgreSQL qua SSL:

```
Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=password;Port=5432;SSL Mode=Require;Trust Server Certificate=true
```

Hoặc với chứng chỉ tùy chỉnh:
```
Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=password;Port=5432;SSL Mode=Require;Root Certificate=/path/to/ca-cert.pem
```

### Connection Pooling

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=password;Port=5432;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100"
  }
}
```

### Timeout Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=password;Port=5432;Timeout=30;Command Timeout=30"
  }
}
```

## Quản lý Migration

### Tạo Migration Mới

```bash
# Sau khi thay đổi models
dotnet ef migrations add TênMigrationCủaBạn
```

### Xem Danh sách Migrations

```bash
dotnet ef migrations list
```

### Áp dụng Migration

```bash
# Áp dụng tất cả migrations pending
dotnet ef database update

# Áp dụng đến một migration cụ thể
dotnet ef database update TênMigration
```

### Rollback Migration

```bash
# Rollback về migration trước đó
dotnet ef database update TênMigrationTrước

# Rollback tất cả migrations
dotnet ef database update 0
```

### Xóa Migration Cuối cùng

```bash
dotnet ef migrations remove
```

### Tạo SQL Script

```bash
# Tạo script SQL cho tất cả migrations
dotnet ef migrations script

# Tạo script từ migration này đến migration khác
dotnet ef migrations script FromMigration ToMigration

# Xuất ra file
dotnet ef migrations script > migration.sql
```

## Sao lưu và Khôi phục

### Sao lưu Cơ sở dữ liệu

```bash
# Sao lưu đầy đủ
pg_dump -U privacyidea_user -h localhost privacyidea > backup.sql

# Sao lưu với nén
pg_dump -U privacyidea_user -h localhost -F c privacyidea > backup.dump

# Chỉ sao lưu schema
pg_dump -U privacyidea_user -h localhost --schema-only privacyidea > schema.sql

# Chỉ sao lưu dữ liệu
pg_dump -U privacyidea_user -h localhost --data-only privacyidea > data.sql
```

### Khôi phục Cơ sở dữ liệu

```bash
# Khôi phục từ SQL file
psql -U privacyidea_user -h localhost privacyidea < backup.sql

# Khôi phục từ dump file
pg_restore -U privacyidea_user -h localhost -d privacyidea backup.dump

# Xóa và tạo lại cơ sở dữ liệu trước khi khôi phục
dropdb -U privacyidea_user -h localhost privacyidea
createdb -U privacyidea_user -h localhost privacyidea
psql -U privacyidea_user -h localhost privacyidea < backup.sql
```

## Tối ưu Hiệu suất

### Tối ưu PostgreSQL

Chỉnh sửa `postgresql.conf`:

```ini
# Memory Settings
shared_buffers = 256MB                  # 25% RAM
effective_cache_size = 1GB              # 50% RAM
maintenance_work_mem = 128MB            # 10% RAM
work_mem = 16MB                         # RAM / max_connections / 16

# Connection Settings
max_connections = 100
superuser_reserved_connections = 3

# Write-Ahead Log
wal_buffers = 16MB
checkpoint_completion_target = 0.9
```

### Indexes

Tạo indexes cho các cột được truy vấn thường xuyên:

```sql
-- Index cho username lookups
CREATE INDEX idx_admin_username ON admin(username);
CREATE INDEX idx_token_serial ON token(serial);
CREATE INDEX idx_token_user ON token(user_id);

-- Index cho timestamps
CREATE INDEX idx_audit_timestamp ON audit(timestamp);
CREATE INDEX idx_token_created ON token(created_at);
```

### Analyze Tables

```sql
-- Cập nhật statistics cho query planner
ANALYZE;

-- Hoặc cho một bảng cụ thể
ANALYZE admin;
ANALYZE token;
```

### Vacuum

```sql
-- Dọn dẹp cơ sở dữ liệu
VACUUM;

-- Vacuum với analyze
VACUUM ANALYZE;

-- Full vacuum (cần downtime)
VACUUM FULL;
```

## Giám sát

### Xem Kết nối Hiện tại

```sql
SELECT 
    pid,
    usename,
    application_name,
    client_addr,
    state,
    query,
    query_start
FROM pg_stat_activity
WHERE datname = 'privacyidea';
```

### Xem Kích thước Cơ sở dữ liệu

```sql
SELECT 
    pg_size_pretty(pg_database_size('privacyidea')) AS database_size;
```

### Xem Kích thước Bảng

```sql
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### Xem Queries Chậm

```sql
SELECT 
    query,
    calls,
    total_exec_time,
    mean_exec_time,
    max_exec_time
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;
```

## Bảo mật

### 1. Sử dụng Strong Passwords

```bash
# Tạo mật khẩu ngẫu nhiên
openssl rand -base64 32
```

### 2. Giới hạn Quyền truy cập

```sql
-- Chỉ cấp quyền cần thiết
REVOKE ALL ON DATABASE privacyidea FROM PUBLIC;
GRANT CONNECT ON DATABASE privacyidea TO privacyidea_user;
GRANT ALL ON SCHEMA public TO privacyidea_user;
```

### 3. Cấu hình pg_hba.conf

```
# TYPE  DATABASE        USER              ADDRESS                 METHOD
local   privacyidea     privacyidea_user                          md5
host    privacyidea     privacyidea_user  127.0.0.1/32            md5
host    privacyidea     privacyidea_user  ::1/128                 md5
```

### 4. Bật SSL

Trong `postgresql.conf`:
```ini
ssl = on
ssl_cert_file = '/path/to/server.crt'
ssl_key_file = '/path/to/server.key'
```

## Xử lý Sự cố

### Không thể Kết nối

```bash
# Kiểm tra PostgreSQL đang chạy
sudo systemctl status postgresql

# Kiểm tra port
sudo netstat -plunt | grep 5432

# Kiểm tra logs
sudo tail -f /var/log/postgresql/postgresql-*.log
```

### Migration Thất bại

```bash
# Xem chi tiết lỗi
dotnet ef database update --verbose

# Reset database (CHÚ Ý: Xóa tất cả dữ liệu!)
dotnet ef database drop
dotnet ef database update
```

### Hiệu suất Kém

```sql
-- Bật query logging
ALTER DATABASE privacyidea SET log_statement = 'all';
ALTER DATABASE privacyidea SET log_duration = on;

-- Kiểm tra table bloat
SELECT 
    schemaname, 
    tablename, 
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size,
    n_dead_tup
FROM pg_stat_user_tables
ORDER BY n_dead_tup DESC;

-- Chạy VACUUM nếu cần
VACUUM ANALYZE;
```

## Tài liệu Tham khảo

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Npgsql Documentation](https://www.npgsql.org/doc/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Data Protection](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/)

## Hỗ trợ

Nếu bạn gặp vấn đề:
1. Kiểm tra logs ứng dụng
2. Kiểm tra PostgreSQL logs
3. Xem lại connection string
4. Kiểm tra quyền cơ sở dữ liệu
5. Đảm bảo migrations đã được áp dụng
