# PrivacyIDEA .NET Core 8 - Kế Hoạch Hoàn Thiện Migration

## Tổng Quan

Dự án đã đạt **~65% hoàn thiện**. Tài liệu này mô tả kế hoạch chuyển đổi các thành phần còn lại.

### Trạng Thái Hiện Tại

| Thành phần | Đã làm | Còn lại | % |
|------------|--------|---------|---|
| Token Types | 27/35 | 8 | 77% |
| Resolvers | 7/8 | 1 | 88% |
| Event Handlers | 6/12 | 6 | 50% |
| SMS Providers | 6/7 | 1 | 86% |
| API Endpoints | 5/33 | 28 | 15% |
| Database Models | 23/48 | 25 | 48% |
| CLI Commands | 11/40+ | 30+ | 27% |
| Core Services | 4/15+ | 11+ | ~27% |

---

## Phase 11: Critical API - Authentication & User Management

**Ưu tiên: 🔴 CRITICAL**  
**Thời gian ước tính: 3-4 ngày**

### 11.1 Auth Controller & Middleware

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/AuthController.cs`
- `src/PrivacyIDEA.Api/Middleware/AuthenticationMiddleware.cs`
- `src/PrivacyIDEA.Api/Middleware/AuthorizationMiddleware.cs`
- `src/PrivacyIDEA.Core/Services/AuthService.cs`
- `src/PrivacyIDEA.Core/Interfaces/IAuthService.cs`

**Endpoints cần implement:**
```
POST /auth                    - Get authentication token (admin login)
GET  /auth                    - Get current auth info
DELETE /auth                  - Logout/revoke token
GET  /auth/rights             - Get user rights
```

**Chức năng:**
- [ ] JWT token generation và validation
- [ ] Admin authentication với password
- [ ] User authentication (2FA flow)
- [ ] Role-based authorization (admin, user, helpdesk)
- [ ] API token support
- [ ] Session management

### 11.2 User Controller & Service

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/UserController.cs`
- `src/PrivacyIDEA.Core/Services/UserService.cs`
- `src/PrivacyIDEA.Core/Interfaces/IUserService.cs`

**Endpoints cần implement:**
```
GET    /user                  - List users
GET    /user/<userid>         - Get user details
POST   /user                  - Create user (if editable resolver)
PUT    /user/<userid>         - Update user
DELETE /user/<userid>         - Delete user
GET    /user/attribute/<userid> - Get user attributes
```

**Chức năng:**
- [ ] User lookup across resolvers
- [ ] User attribute management
- [ ] User CRUD (for editable resolvers)
- [ ] User search/filter

---

## Phase 12: Management APIs

**Ưu tiên: 🟠 HIGH**  
**Thời gian ước tính: 4-5 ngày**

### 12.1 Resolver Management

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/ResolverController.cs`
- `src/PrivacyIDEA.Core/Services/ResolverService.cs`

**Endpoints:**
```
GET    /resolver              - List resolvers
GET    /resolver/<name>       - Get resolver config
POST   /resolver/<name>       - Create/update resolver
DELETE /resolver/<name>       - Delete resolver
POST   /resolver/test         - Test resolver connection
```

### 12.2 Policy Management

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/PolicyController.cs`
- `src/PrivacyIDEA.Core/Services/PolicyManagementService.cs`

**Endpoints:**
```
GET    /policy                - List policies
GET    /policy/<name>         - Get policy
POST   /policy/<name>         - Create/update policy
DELETE /policy/<name>         - Delete policy
POST   /policy/enable/<name>  - Enable policy
POST   /policy/disable/<name> - Disable policy
GET    /policy/defs           - Get policy definitions
```

### 12.3 Event Handler Management

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/EventController.cs`
- `src/PrivacyIDEA.Core/Services/EventManagementService.cs`

**Endpoints:**
```
GET    /event                 - List event handlers
GET    /event/<id>            - Get event handler
POST   /event                 - Create event handler
PUT    /event/<id>            - Update event handler
DELETE /event/<id>            - Delete event handler
POST   /event/enable/<id>     - Enable handler
POST   /event/disable/<id>    - Disable handler
GET    /event/available       - List available events
GET    /event/actions         - List available actions
GET    /event/conditions      - List available conditions
```

### 12.4 Realm Management (Extended)

**File cập nhật:**
- `src/PrivacyIDEA.Api/Controllers/RealmController.cs`
- `src/PrivacyIDEA.Core/Services/RealmService.cs`

**Endpoints thêm:**
```
POST   /realm/<name>          - Create realm
DELETE /realm/<name>          - Delete realm
POST   /realm/default/<name>  - Set default realm
DELETE /realm/default         - Clear default realm
```

---

## Phase 13: Server Configuration APIs

**Ưu tiên: 🟠 HIGH**  
**Thời gian ước tính: 3-4 ngày**

### 13.1 SMS Gateway Management

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/SmsGatewayController.cs`
- `src/PrivacyIDEA.Core/Services/SmsGatewayService.cs`

**Endpoints:**
```
GET    /smsgateway            - List SMS gateways
GET    /smsgateway/<name>     - Get gateway config
POST   /smsgateway/<name>     - Create/update gateway
DELETE /smsgateway/<name>     - Delete gateway
POST   /smsgateway/test       - Test gateway
```

### 13.2 SMTP Server Management

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/SmtpServerController.cs`
- `src/PrivacyIDEA.Core/Services/SmtpService.cs`
- `src/PrivacyIDEA.Core/Services/EmailService.cs`

**Endpoints:**
```
GET    /smtpserver            - List SMTP servers
GET    /smtpserver/<id>       - Get SMTP server
POST   /smtpserver            - Create SMTP server
PUT    /smtpserver/<id>       - Update SMTP server
DELETE /smtpserver/<id>       - Delete SMTP server
POST   /smtpserver/test       - Test SMTP connection
```

### 13.3 RADIUS Server Management

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/RadiusServerController.cs`
- `src/PrivacyIDEA.Core/Services/RadiusService.cs`

**Endpoints:**
```
GET    /radiusserver          - List RADIUS servers
GET    /radiusserver/<id>     - Get RADIUS server
POST   /radiusserver          - Create RADIUS server
PUT    /radiusserver/<id>     - Update RADIUS server
DELETE /radiusserver/<id>     - Delete RADIUS server
POST   /radiusserver/test     - Test RADIUS connection
```

---

## Phase 14: Registration & Recovery

**Ưu tiên: 🟠 HIGH**  
**Thời gian ước tính: 2-3 ngày**

### 14.1 User Registration

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/RegisterController.cs`
- `src/PrivacyIDEA.Core/Services/RegistrationService.cs`

**Endpoints:**
```
POST   /register              - Register new user
GET    /register/status/<token> - Check registration status
POST   /register/confirm      - Confirm registration
```

### 14.2 Account Recovery

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/RecoverController.cs`
- `src/PrivacyIDEA.Core/Services/RecoveryService.cs`

**Endpoints:**
```
POST   /recover               - Initiate recovery
POST   /recover/reset         - Reset credentials
GET    /recover/status/<token> - Check recovery status
```

**Entity cần thêm:**
- `src/PrivacyIDEA.Domain/Entities/PasswordReset.cs`

---

## Phase 15: Remaining Event Handlers

**Ưu tiên: 🟡 MEDIUM**  
**Thời gian ước tính: 2 ngày**

### File tạo mới:
- `src/PrivacyIDEA.Core/EventHandlers/ContainerHandler.cs`
- `src/PrivacyIDEA.Core/EventHandlers/CustomUserAttributesHandler.cs`
- `src/PrivacyIDEA.Core/EventHandlers/FederationHandler.cs`
- `src/PrivacyIDEA.Core/EventHandlers/RequestManglerHandler.cs`
- `src/PrivacyIDEA.Core/EventHandlers/ResponseManglerHandler.cs`

### Chức năng:
- [ ] ContainerEventHandler - Token container management events
- [ ] CustomUserAttributesHandler - Set/modify user attributes
- [ ] FederationEventHandler - Forward to federated servers
- [ ] RequestManglerHandler - Modify incoming requests
- [ ] ResponseManglerHandler - Modify outgoing responses

---

## Phase 16: Remaining Tokens & Resolvers

**Ưu tiên: 🟡 MEDIUM**  
**Thời gian ước tính: 2 ngày**

### 16.1 Remaining Token Types

**File tạo mới:**
- `src/PrivacyIDEA.Core/Tokens/ApplicationSpecificPasswordToken.cs`

**File cập nhật (WebAuthn helpers):**
- `src/PrivacyIDEA.Core/Security/WebAuthn/CoseAlgorithm.cs`
- `src/PrivacyIDEA.Core/Security/WebAuthn/AttestationType.cs`
- `src/PrivacyIDEA.Core/Security/WebAuthn/WebAuthnHelpers.cs`

### 16.2 Keycloak Resolver

**File tạo mới:**
- `src/PrivacyIDEA.Core/Resolvers/KeycloakResolver.cs`

### 16.3 SMS Providers

**File tạo mới:**
- `src/PrivacyIDEA.Core/SmsProviders/SmtpSmsProvider.cs`
- `src/PrivacyIDEA.Core/SmsProviders/ScriptSmsProvider.cs`
- `src/PrivacyIDEA.Core/SmsProviders/SipgateSmsProvider.cs`

---

## Phase 17: Extended CLI Commands

**Ưu tiên: 🟠 HIGH**  
**Thời gian ước tính: 3-4 ngày**

### File cập nhật:
- `src/PrivacyIDEA.Cli/Program.cs` (thêm commands)

### Commands cần implement:

**Admin Management:**
```bash
pi-manage admin delete --username <name>
pi-manage admin change --username <name> --email <email> --password
```

**Realm Management:**
```bash
pi-manage realm create --name <name> --resolver <resolver>
pi-manage realm delete --name <name>
pi-manage realm set-default --name <name>
pi-manage realm clear-default
```

**Resolver Management:**
```bash
pi-manage resolver list
pi-manage resolver create --name <name> --type <type> --config <json>
pi-manage resolver delete --name <name>
pi-manage resolver test --name <name>
```

**Policy Management:**
```bash
pi-manage policy list
pi-manage policy create --name <name> --scope <scope> --action <action>
pi-manage policy delete --name <name>
pi-manage policy enable --name <name>
pi-manage policy disable --name <name>
```

**Token Management:**
```bash
pi-manage token import --file <file> --type <type>
pi-manage token export --serial <serial> --format <format>
```

**Backup & Restore:**
```bash
pi-manage backup create --output <file>
pi-manage backup restore --input <file>
```

**Config Management:**
```bash
pi-manage config export --output <file>
pi-manage config import --input <file>
pi-manage config set --key <key> --value <value>
pi-manage config get --key <key>
```

---

## Phase 18: Database Entities

**Ưu tiên: 🟡 MEDIUM**  
**Thời gian ước tính: 2-3 ngày**

### Entities cần thêm:

**Caching:**
```csharp
// src/PrivacyIDEA.Domain/Entities/AuthCache.cs
public class AuthCache { ... }

// src/PrivacyIDEA.Domain/Entities/UserCache.cs
public class UserCache { ... }
```

**Machine Tokens:**
```csharp
// src/PrivacyIDEA.Domain/Entities/MachineResolver.cs
public class MachineResolver { ... }
public class MachineResolverConfig { ... }

// src/PrivacyIDEA.Domain/Entities/MachineToken.cs
public class MachineToken { ... }
public class MachineTokenOptions { ... }
```

**Token Containers:**
```csharp
// src/PrivacyIDEA.Domain/Entities/TokenContainer.cs
public class TokenContainer { ... }
public class TokenContainerOwner { ... }
public class TokenContainerStates { ... }
public class TokenContainerInfo { ... }
public class TokenContainerRealm { ... }
public class TokenContainerTemplate { ... }
public class TokenContainerToken { ... }
```

**Token Groups:**
```csharp
// src/PrivacyIDEA.Domain/Entities/TokenGroup.cs
public class TokenGroup { ... }
public class TokenTokenGroup { ... }
```

**Monitoring:**
```csharp
// src/PrivacyIDEA.Domain/Entities/MonitoringStats.cs
public class MonitoringStats { ... }

// src/PrivacyIDEA.Domain/Entities/EventCounter.cs
public class EventCounter { ... }
```

**Periodic Tasks:**
```csharp
// src/PrivacyIDEA.Domain/Entities/PeriodicTask.cs
public class PeriodicTask { ... }
public class PeriodicTaskOption { ... }
public class PeriodicTaskLastRun { ... }
```

**Other:**
```csharp
// src/PrivacyIDEA.Domain/Entities/CustomUserAttribute.cs
public class CustomUserAttribute { ... }

// src/PrivacyIDEA.Domain/Entities/CAConnector.cs
public class CAConnector { ... }
public class CAConnectorConfig { ... }

// src/PrivacyIDEA.Domain/Entities/Subscription.cs
public class Subscription { ... }
public class ClientApplication { ... }

// src/PrivacyIDEA.Domain/Entities/PrivacyIDEAServer.cs
public class PrivacyIDEAServer { ... }

// src/PrivacyIDEA.Domain/Entities/Serviceid.cs
public class ServiceId { ... }
```

---

## Phase 19: Infrastructure & Middleware

**Ưu tiên: 🟡 MEDIUM**  
**Thời gian ước tính: 3-4 ngày**

### 19.1 Caching Layer

**File tạo mới:**
- `src/PrivacyIDEA.Infrastructure/Caching/ICacheService.cs`
- `src/PrivacyIDEA.Infrastructure/Caching/MemoryCacheService.cs`
- `src/PrivacyIDEA.Infrastructure/Caching/RedisCacheService.cs`

**NuGet packages:**
```xml
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
<PackageReference Include="StackExchange.Redis" Version="2.7.10" />
```

### 19.2 Rate Limiting

**File tạo mới:**
- `src/PrivacyIDEA.Api/Middleware/RateLimitingMiddleware.cs`
- `src/PrivacyIDEA.Core/Services/RateLimitService.cs`

### 19.3 Background Jobs

**File tạo mới:**
- `src/PrivacyIDEA.Infrastructure/Jobs/TokenCleanupJob.cs`
- `src/PrivacyIDEA.Infrastructure/Jobs/AuditRotationJob.cs`
- `src/PrivacyIDEA.Infrastructure/Jobs/ChallengeCleanupJob.cs`

**NuGet packages:**
```xml
<PackageReference Include="Hangfire.Core" Version="1.8.6" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.6" />
```

### 19.4 Logging

**File tạo mới:**
- `src/PrivacyIDEA.Api/Middleware/RequestLoggingMiddleware.cs`

**NuGet packages:**
```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

### 19.5 API Versioning

**File cập nhật:**
- `src/PrivacyIDEA.Api/Program.cs`

**NuGet packages:**
```xml
<PackageReference Include="Asp.Versioning.Mvc" Version="8.0.0" />
<PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.0.0" />
```

---

## Phase 20: Monitoring & Health

**Ưu tiên: 🟢 LOW**  
**Thời gian ước tính: 1-2 ngày**

### 20.1 Health Checks

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/HealthController.cs`
- `src/PrivacyIDEA.Infrastructure/HealthChecks/DatabaseHealthCheck.cs`
- `src/PrivacyIDEA.Infrastructure/HealthChecks/ResolverHealthCheck.cs`

**Endpoints:**
```
GET /health                   - Basic health check
GET /health/ready             - Readiness check
GET /health/live              - Liveness check
GET /health/details           - Detailed health info
```

### 20.2 Monitoring Endpoints

**File tạo mới:**
- `src/PrivacyIDEA.Api/Controllers/MonitoringController.cs`
- `src/PrivacyIDEA.Core/Services/MonitoringService.cs`

**Endpoints:**
```
GET /monitoring/tokens        - Token statistics
GET /monitoring/users         - User statistics
GET /monitoring/auths         - Authentication statistics
GET /monitoring/system        - System metrics
```

---

## Phase 21: Machine Tokens & Applications

**Ưu tiên: 🟢 LOW**  
**Thời gian ước tính: 2-3 ngày**

### File tạo mới:
- `src/PrivacyIDEA.Api/Controllers/MachineController.cs`
- `src/PrivacyIDEA.Api/Controllers/MachineResolverController.cs`
- `src/PrivacyIDEA.Api/Controllers/ApplicationController.cs`
- `src/PrivacyIDEA.Core/Services/MachineService.cs`
- `src/PrivacyIDEA.Core/Services/ApplicationService.cs`

### Endpoints:
```
GET    /machine               - List machines
POST   /machine/<name>        - Create machine token
DELETE /machine/<name>        - Delete machine token
GET    /machine/authitem      - Get auth items for machine

GET    /application           - List applications
POST   /application/<name>    - Create application
DELETE /application/<name>    - Delete application
```

---

## Phase 22: Token Containers & Groups

**Ưu tiên: 🟢 LOW**  
**Thời gian ước tính: 2 ngày**

### File tạo mới:
- `src/PrivacyIDEA.Api/Controllers/ContainerController.cs`
- `src/PrivacyIDEA.Api/Controllers/TokenGroupController.cs`
- `src/PrivacyIDEA.Core/Services/ContainerService.cs`
- `src/PrivacyIDEA.Core/Services/TokenGroupService.cs`

### Endpoints:
```
GET    /container             - List containers
POST   /container             - Create container
PUT    /container/<id>        - Update container
DELETE /container/<id>        - Delete container

GET    /tokengroup            - List token groups
POST   /tokengroup            - Create group
DELETE /tokengroup/<name>     - Delete group
POST   /tokengroup/<name>/add - Add token to group
POST   /tokengroup/<name>/remove - Remove token from group
```

---

## Phase 23: CA Connectors & Periodic Tasks

**Ưu tiên: 🟢 LOW**  
**Thời gian ước tính: 2 ngày**

### CA Connectors:
- `src/PrivacyIDEA.Api/Controllers/CAConnectorController.cs`
- `src/PrivacyIDEA.Core/Services/CAConnectorService.cs`

### Periodic Tasks:
- `src/PrivacyIDEA.Api/Controllers/PeriodicTaskController.cs`
- `src/PrivacyIDEA.Core/Services/PeriodicTaskService.cs`

---

## Phase 24: Testing

**Ưu tiên: 🟠 HIGH**  
**Thời gian ước tính: 5-7 ngày**

### Unit Tests:
```
tests/PrivacyIDEA.Core.Tests/
├── Services/
│   ├── AuthServiceTests.cs
│   ├── UserServiceTests.cs
│   ├── PolicyServiceTests.cs
│   └── ...
├── Tokens/
│   ├── HotpTokenTests.cs (expand)
│   ├── TotpTokenTests.cs
│   ├── WebAuthnTokenTests.cs
│   └── ...
└── Resolvers/
    ├── LdapResolverTests.cs
    ├── SqlResolverTests.cs
    └── ...
```

### Integration Tests:
```
tests/PrivacyIDEA.Api.Tests/
├── Controllers/
│   ├── ValidateControllerTests.cs
│   ├── TokenControllerTests.cs
│   ├── AuthControllerTests.cs
│   └── ...
└── Integration/
    ├── AuthenticationFlowTests.cs
    ├── TokenEnrollmentTests.cs
    └── ...
```

---

## Phase 25: Documentation

**Ưu tiên: 🟡 MEDIUM**  
**Thời gian ước tính: 2-3 ngày**

### Files cần tạo:
- `docs/API.md` - API documentation
- `docs/DEPLOYMENT.md` - Deployment guide
- `docs/CONFIGURATION.md` - Configuration reference
- `docs/MIGRATION.md` - Migration from Python guide
- `docs/DEVELOPMENT.md` - Development setup

### OpenAPI/Swagger:
- Configure Swagger UI
- Generate OpenAPI spec
- XML documentation comments

---

## Tổng Kết Thời Gian Ước Tính

| Phase | Mô tả | Thời gian |
|-------|-------|-----------|
| 11 | Auth & User API | 3-4 ngày |
| 12 | Management APIs | 4-5 ngày |
| 13 | Server Config APIs | 3-4 ngày |
| 14 | Registration & Recovery | 2-3 ngày |
| 15 | Remaining Event Handlers | 2 ngày |
| 16 | Remaining Tokens & Resolvers | 2 ngày |
| 17 | Extended CLI Commands | 3-4 ngày |
| 18 | Database Entities | 2-3 ngày |
| 19 | Infrastructure & Middleware | 3-4 ngày |
| 20 | Monitoring & Health | 1-2 ngày |
| 21 | Machine Tokens | 2-3 ngày |
| 22 | Token Containers & Groups | 2 ngày |
| 23 | CA & Periodic Tasks | 2 ngày |
| 24 | Testing | 5-7 ngày |
| 25 | Documentation | 2-3 ngày |
| **TOTAL** | | **~38-51 ngày** |

---

## Thứ Tự Ưu Tiên Triển Khai

### Sprint 1 (Tuần 1-2): Critical
- [ ] Phase 11: Auth & User API
- [ ] Phase 14: Registration & Recovery

### Sprint 2 (Tuần 3-4): High Priority
- [ ] Phase 12: Management APIs (Resolver, Policy, Event)
- [ ] Phase 13: Server Config APIs

### Sprint 3 (Tuần 5-6): Core Completion
- [ ] Phase 17: Extended CLI Commands
- [ ] Phase 19: Infrastructure & Middleware
- [ ] Phase 24: Testing (start)

### Sprint 4 (Tuần 7-8): Medium Priority
- [ ] Phase 15: Remaining Event Handlers
- [ ] Phase 16: Remaining Tokens & Resolvers
- [ ] Phase 18: Database Entities

### Sprint 5 (Tuần 9-10): Low Priority & Finalization
- [ ] Phase 20: Monitoring & Health
- [ ] Phase 21-23: Machine Tokens, Containers, CA
- [ ] Phase 24: Testing (complete)
- [ ] Phase 25: Documentation

---

## Checklist Hoàn Thành

### Critical (Phải có để production-ready)
- [ ] JWT Authentication
- [ ] User API endpoints
- [ ] Rate limiting
- [ ] Error handling & logging
- [ ] Unit tests (>70% coverage)

### High Priority (Nên có)
- [ ] All management APIs
- [ ] Extended CLI
- [ ] Backup/restore
- [ ] Integration tests

### Medium Priority (Có thì tốt)
- [ ] All event handlers
- [ ] All tokens
- [ ] Caching layer
- [ ] API versioning

### Low Priority (Có thể làm sau)
- [ ] Machine tokens
- [ ] Token containers
- [ ] CA connectors
- [ ] HSM support
