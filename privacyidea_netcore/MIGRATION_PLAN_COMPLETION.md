# Kế Hoạch Hoàn Thiện Migration PrivacyIDEA .NET Core 8

**Ngày tạo:** 02/04/2026  
**Phiên bản mục tiêu:** 4.0.0  
**Mục tiêu:** Hoàn thiện 100% migration và chuyển sang PostgreSQL

---

## 📋 TỔNG QUAN

### Tiến độ hiện tại: 70%
### Mục tiêu: 100%

### Các thay đổi chính:
1. Chuyển từ SQLite/SQL Server sang PostgreSQL
2. Hoàn thiện các Database Entities còn thiếu
3. Tăng Test Coverage lên 50%+
4. Hoàn thiện Event Handlers
5. Mở rộng CLI Commands

---

## 🗓️ PHASE 16-25: KẾ HOẠCH CHI TIẾT

### Phase 16: PostgreSQL Database Migration (Ưu tiên cao)

**Mục tiêu:** Chuyển đổi database sang PostgreSQL

| Task | Mô tả | Ước lượng |
|------|-------|-----------|
| 16.1 | Cài đặt Npgsql.EntityFrameworkCore.PostgreSQL | 0.5 ngày |
| 16.2 | Cập nhật DbContext cho PostgreSQL conventions | 1 ngày |
| 16.3 | Tạo migration scripts | 1 ngày |
| 16.4 | Cập nhật connection string và configuration | 0.5 ngày |
| 16.5 | Test và verify database operations | 1 ngày |

**Files cần sửa:**
- `PrivacyIDEA.Infrastructure.csproj` - Thêm Npgsql package
- `PrivacyIDEADbContext.cs` - Cấu hình PostgreSQL
- `appsettings.json` - Connection string
- `Program.cs` - Database configuration

---

### Phase 17: Database Entities Completion (Ưu tiên cao)

**Mục tiêu:** Hoàn thiện tất cả entities còn thiếu (44% → 90%)

| Task | Entity | Python Model | Ưu tiên |
|------|--------|--------------|---------|
| 17.1 | `AuthCache` | `cache.py` | Cao |
| 17.2 | `UserCache` | `cache.py` | Cao |
| 17.3 | `MachineResolver` | `machine.py` | Trung bình |
| 17.4 | `MachineResolverConfig` | `machine.py` | Trung bình |
| 17.5 | `MachineToken` | `machine.py` | Trung bình |
| 17.6 | `MachineTokenOptions` | `machine.py` | Trung bình |
| 17.7 | `CAConnector` | `caconnector.py` | Trung bình |
| 17.8 | `CAConnectorConfig` | `caconnector.py` | Trung bình |
| 17.9 | `PeriodicTask` | `periodictask.py` | Thấp |
| 17.10 | `PeriodicTaskOption` | `periodictask.py` | Thấp |
| 17.11 | `PeriodicTaskLastRun` | `periodictask.py` | Thấp |
| 17.12 | `EventCounter` | `eventcounter.py` | Thấp |
| 17.13 | `MonitoringStats` | `monitoringstats.py` | Thấp |
| 17.14 | `CustomUserAttribute` | `customuserattribute.py` | Trung bình |
| 17.15 | `TokenContainer` | `tokencontainer.py` | Thấp |
| 17.16 | `TokenContainerOwner` | `tokencontainer.py` | Thấp |
| 17.17 | `TokenContainerInfo` | `tokencontainer.py` | Thấp |
| 17.18 | `TokenContainerRealm` | `tokencontainer.py` | Thấp |
| 17.19 | `Tokengroup` | `tokengroup.py` | Thấp |
| 17.20 | `TokenTokengroup` | `tokengroup.py` | Thấp |
| 17.21 | `PasswordReset` | `token.py` | Cao |
| 17.22 | `Serviceid` | `serviceid.py` | Thấp |
| 17.23 | `ClientApplication` | `server.py` | Thấp |
| 17.24 | `Subscription` | `subscription.py` | Thấp |
| 17.25 | `PrivacyIDEAServer` | `server.py` | Thấp |
| 17.26 | `NodeName` | `server.py` | Thấp |

**Ước lượng:** 5-7 ngày

---

### Phase 18: Event Handlers Completion (Ưu tiên cao)

**Mục tiêu:** Hoàn thiện event handlers (50% → 90%)

| Task | Handler | Mô tả | Ưu tiên |
|------|---------|-------|---------|
| 18.1 | `RequestManglerHandler` | Modify request before processing | Cao |
| 18.2 | `ResponseManglerHandler` | Modify response after processing | Cao |
| 18.3 | `FederationHandler` | Forward to federated server | Trung bình |
| 18.4 | `CustomUserAttributeHandler` | Set custom user attributes | Trung bình |
| 18.5 | `ContainerHandler` | Container token operations | Thấp |
| 18.6 | `TokenGroupHandler` | Token group operations | Thấp |

**Ước lượng:** 3-4 ngày

---

### Phase 19: Testing Framework (Ưu tiên cao)

**Mục tiêu:** Tăng test coverage (6% → 50%)

| Task | Mô tả | Ước lượng |
|------|-------|-----------|
| 19.1 | Setup test infrastructure với PostgreSQL test container | 1 ngày |
| 19.2 | Unit tests cho tất cả Token types (27 tokens) | 3 ngày |
| 19.3 | Unit tests cho Core Services (9 services) | 2 ngày |
| 19.4 | Unit tests cho Resolvers (7 resolvers) | 2 ngày |
| 19.5 | Integration tests cho API Controllers | 3 ngày |
| 19.6 | Integration tests cho Database operations | 2 ngày |

**Target:** 150+ test cases

**Packages cần thêm:**
- `Testcontainers.PostgreSql`
- `FluentAssertions`
- `NSubstitute` hoặc `Moq`

**Ước lượng:** 13 ngày

---

### Phase 20: CLI Commands Extension (Ưu tiên trung bình)

**Mục tiêu:** Mở rộng CLI (28% → 70%)

| Task | Command | Mô tả |
|------|---------|-------|
| 20.1 | `pi-manage token import` | Import tokens từ file |
| 20.2 | `pi-manage token export` | Export tokens ra file |
| 20.3 | `pi-manage user list` | List users |
| 20.4 | `pi-manage user create` | Create user |
| 20.5 | `pi-manage event list` | List event handlers |
| 20.6 | `pi-manage event create` | Create event handler |
| 20.7 | `pi-manage smsgw list` | List SMS gateways |
| 20.8 | `pi-manage smsgw create` | Create SMS gateway |
| 20.9 | `pi-manage smtp list` | List SMTP servers |
| 20.10 | `pi-manage migrate` | Database migration |
| 20.11 | `pi-manage stats` | System statistics |
| 20.12 | `pi-manage health` | Health check |

**Ước lượng:** 4-5 ngày

---

### Phase 21: CA Connector & Certificate Management (Ưu tiên trung bình)

| Task | Mô tả | Ước lượng |
|------|-------|-----------|
| 21.1 | `ICAConnectorService` interface | 0.5 ngày |
| 21.2 | `LocalCAConnector` implementation | 1 ngày |
| 21.3 | `MicrosoftCAConnector` implementation | 1 ngày |
| 21.4 | `CAConnectorController` API | 1 ngày |
| 21.5 | Certificate token enhancements | 1 ngày |

**Ước lượng:** 4-5 ngày

---

### Phase 22: Advanced Token Features (Ưu tiên thấp)

| Task | Mô tả |
|------|-------|
| 22.1 | Token Groups management |
| 22.2 | Token Containers full support |
| 22.3 | Application-Specific Passwords |
| 22.4 | Token revocation lists |

**Ước lượng:** 3-4 ngày

---

### Phase 23: Federation & HA (Ưu tiên thấp)

| Task | Mô tả |
|------|-------|
| 23.1 | PrivacyIDEA Server federation |
| 23.2 | Node management |
| 23.3 | Subscription management |
| 23.4 | Load balancing support |

**Ước lượng:** 5-6 ngày

---

### Phase 24: Monitoring & Observability (Ưu tiên trung bình)

| Task | Mô tả |
|------|-------|
| 24.1 | Prometheus metrics endpoint |
| 24.2 | Grafana dashboard templates |
| 24.3 | Structured logging với Serilog |
| 24.4 | Distributed tracing với OpenTelemetry |
| 24.5 | Health checks với AspNetCore.Diagnostics |

**Packages:**
- `prometheus-net.AspNetCore`
- `Serilog.AspNetCore`
- `OpenTelemetry.Extensions.Hosting`

**Ước lượng:** 3-4 ngày

---

### Phase 25: Documentation & Deployment (Ưu tiên cao)

| Task | Mô tả |
|------|-------|
| 25.1 | API documentation với Swagger/ReDoc |
| 25.2 | README.md hoàn chỉnh |
| 25.3 | Docker & Docker Compose |
| 25.4 | Kubernetes manifests |
| 25.5 | CI/CD pipeline (GitHub Actions) |
| 25.6 | Migration guide từ Python |

**Ước lượng:** 4-5 ngày

---

## 📊 TIMELINE TỔNG HỢP

| Phase | Tên | Thời gian | Tích lũy |
|-------|-----|-----------|----------|
| 16 | PostgreSQL Migration | 4 ngày | 4 ngày |
| 17 | Database Entities | 7 ngày | 11 ngày |
| 18 | Event Handlers | 4 ngày | 15 ngày |
| 19 | Testing | 13 ngày | 28 ngày |
| 20 | CLI Extension | 5 ngày | 33 ngày |
| 21 | CA Connector | 5 ngày | 38 ngày |
| 22 | Advanced Tokens | 4 ngày | 42 ngày |
| 23 | Federation | 6 ngày | 48 ngày |
| 24 | Monitoring | 4 ngày | 52 ngày |
| 25 | Documentation | 5 ngày | 57 ngày |

**Tổng thời gian ước lượng:** 57 ngày (~3 tháng)

---

## 🎯 MILESTONES

| Milestone | Target | Mục tiêu % |
|-----------|--------|------------|
| M1: PostgreSQL Ready | Phase 16 hoàn thành | 75% |
| M2: Core Complete | Phase 17-18 hoàn thành | 85% |
| M3: Test Coverage 50% | Phase 19 hoàn thành | 90% |
| M4: Production Ready | Phase 20-25 hoàn thành | 100% |

---

## 📦 DEPENDENCIES & PACKAGES

### PostgreSQL
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Npgsql" Version="8.0.0" />
```

### Testing
```xml
<PackageReference Include="Testcontainers.PostgreSql" Version="3.7.0" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="NSubstitute" Version="5.1.0" />
<PackageReference Include="Bogus" Version="35.4.0" />
```

### Monitoring
```xml
<PackageReference Include="prometheus-net.AspNetCore" Version="8.2.1" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
```

---

## ✅ CHECKLIST HOÀN THÀNH

- [ ] Phase 16: PostgreSQL Migration
- [ ] Phase 17: Database Entities (26 entities)
- [ ] Phase 18: Event Handlers (6 handlers)
- [ ] Phase 19: Testing (150+ tests)
- [ ] Phase 20: CLI Commands (12 commands)
- [ ] Phase 21: CA Connector
- [ ] Phase 22: Advanced Tokens
- [ ] Phase 23: Federation
- [ ] Phase 24: Monitoring
- [ ] Phase 25: Documentation

---

*Kế hoạch này được tạo để hoàn thiện 100% migration từ PrivacyIDEA Python sang .NET Core 8 với PostgreSQL database.*
