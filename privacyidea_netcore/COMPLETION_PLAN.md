# 📋 KẾ HOẠCH HOÀN THÀNH MIGRATION
# PrivacyIDEA Python → .NET Core 8

**Ngày tạo:** 02/04/2026  
**Mục tiêu:** Hoàn thành 100% migration (không bao gồm testing)  
**Tiến độ hiện tại:** 78%

---

## 🎯 MỤC TIÊU

Hoàn thành các thành phần còn thiếu:
- API Controllers: 59% → 100%
- Token Types: 75% → 100%
- Event Handlers: 50% → 100%
- User Resolvers: 78% → 100%
- CLI Commands: 28% → 80%
- SMS Providers: 86% → 100%

---

## 📅 PHASE 18: API Controllers Còn Thiếu

**Mục tiêu:** 59% → 100% (thêm 13 controllers)

### Task 18.1: CA Connector API (Ưu tiên cao)
```
File: src/PrivacyIDEA.Api/Controllers/CAConnectorController.cs
Python: privacyidea/api/caconnector.py
```
Endpoints:
- GET /caconnector - List all CA connectors
- GET /caconnector/{name} - Get CA connector details
- POST /caconnector/{name} - Create CA connector
- DELETE /caconnector/{name} - Delete CA connector
- GET /caconnector/{name}/cacerts - Get CA certificates
- POST /caconnector/{name}/request - Request certificate

### Task 18.2: Machine Resolver API
```
File: src/PrivacyIDEA.Api/Controllers/MachineResolverController.cs
Python: privacyidea/api/machineresolver.py
```
Endpoints:
- GET /machineresolver - List machine resolvers
- GET /machineresolver/{name} - Get resolver config
- POST /machineresolver/{name} - Create/update resolver
- DELETE /machineresolver/{name} - Delete resolver

### Task 18.3: Token Group API
```
File: src/PrivacyIDEA.Api/Controllers/TokenGroupController.cs
Python: privacyidea/api/tokengroup.py
```
Endpoints:
- GET /tokengroup - List token groups
- GET /tokengroup/{name} - Get group details
- POST /tokengroup/{name} - Create group
- DELETE /tokengroup/{name} - Delete group
- POST /tokengroup/{name}/token/{serial} - Add token to group
- DELETE /tokengroup/{name}/token/{serial} - Remove token

### Task 18.4: Subscription API
```
File: src/PrivacyIDEA.Api/Controllers/SubscriptionController.cs
Python: privacyidea/api/subscriptions.py
```
Endpoints:
- GET /subscription - Get subscription info
- POST /subscription - Upload subscription file
- DELETE /subscription - Remove subscription

### Task 18.5: PrivacyIDEA Server API (Federation)
```
File: src/PrivacyIDEA.Api/Controllers/PrivacyIDEAServerController.cs
Python: privacyidea/api/privacyideaserver.py
```
Endpoints:
- GET /privacyideaserver - List servers
- GET /privacyideaserver/{identifier} - Get server
- POST /privacyideaserver/{identifier} - Create server
- DELETE /privacyideaserver/{identifier} - Delete server
- POST /privacyideaserver/{identifier}/test - Test connection

### Task 18.6: Service ID API
```
File: src/PrivacyIDEA.Api/Controllers/ServiceIdController.cs
Python: privacyidea/api/serviceid.py
```
Endpoints:
- GET /serviceid - List service IDs
- POST /serviceid/{name} - Create service ID
- DELETE /serviceid/{name} - Delete service ID

### Task 18.7: Monitoring API
```
File: src/PrivacyIDEA.Api/Controllers/MonitoringController.cs
Python: privacyidea/api/monitoring.py
```
Endpoints:
- GET /monitoring - Get monitoring stats
- GET /monitoring/token - Token statistics
- GET /monitoring/eventcounter - Event counters

### Task 18.8: Client Type API
```
File: src/PrivacyIDEA.Api/Controllers/ClientTypeController.cs
Python: privacyidea/api/clienttype.py
```
Endpoints:
- GET /clienttype - List client types
- POST /clienttype - Register client

### Task 18.9: Info API
```
File: src/PrivacyIDEA.Api/Controllers/InfoController.cs
Python: privacyidea/api/info.py
```
Endpoints:
- GET /info - System information
- GET /info/version - Version info
- GET /info/plugins - Installed plugins

### Task 18.10: Token Type Info API
```
File: src/PrivacyIDEA.Api/Controllers/TokenTypeController.cs
Python: privacyidea/api/ttype.py
```
Endpoints:
- GET /ttype - List available token types
- GET /ttype/{type} - Get token type info

**Ước tính:** 3-4 ngày

---

## 📅 PHASE 19: Event Handlers Còn Thiếu

**Mục tiêu:** 50% → 100% (thêm 6 handlers)

### Task 19.1: Federation Event Handler
```
File: src/PrivacyIDEA.Core/EventHandlers/FederationEventHandler.cs
Python: privacyidea/lib/eventhandler/federationhandler.py
```
- Forward events to federated PrivacyIDEA servers
- Sync tokens across servers

### Task 19.2: Request Mangler Handler
```
File: src/PrivacyIDEA.Core/EventHandlers/RequestManglerHandler.cs
Python: privacyidea/lib/eventhandler/requestmangler.py
```
- Modify incoming requests
- Add/remove parameters
- Transform values

### Task 19.3: Response Mangler Handler
```
File: src/PrivacyIDEA.Core/EventHandlers/ResponseManglerHandler.cs
Python: privacyidea/lib/eventhandler/responsemangler.py
```
- Modify outgoing responses
- Add custom fields
- Transform response data

### Task 19.4: Custom User Attribute Handler
```
File: src/PrivacyIDEA.Core/EventHandlers/CustomUserAttributeHandler.cs
Python: privacyidea/lib/eventhandler/customuserattributehandler.py
```
- Set/delete custom user attributes
- Based on event conditions

### Task 19.5: Container Handler
```
File: src/PrivacyIDEA.Core/EventHandlers/ContainerHandler.cs
Python: privacyidea/lib/eventhandler/containerhandler.py
```
- Manage token containers
- Add/remove tokens from containers

### Task 19.6: Token Group Handler
```
File: src/PrivacyIDEA.Core/EventHandlers/TokenGroupHandler.cs
Python: privacyidea/lib/eventhandler/tokengrouphandler.py
```
- Manage token group membership
- Add/remove tokens based on events

**Ước tính:** 2 ngày

---

## 📅 PHASE 20: Token Types Còn Thiếu

**Mục tiêu:** 75% → 100% (thêm 9 token types)

### Task 20.1: Daplug Token
```
File: src/PrivacyIDEA.Core/Tokens/DaplugToken.cs
Python: privacyidea/lib/tokens/daplugtoken.py
```
- Daplug hardware token support
- HOTP-based with specific modes

### Task 20.2: TiQR Token
```
File: src/PrivacyIDEA.Core/Tokens/TiqrToken.cs
Python: privacyidea/lib/tokens/tiqrtoken.py
```
- TiQR mobile authentication
- OCRA challenge-response

### Task 20.3: VASCO Token
```
File: src/PrivacyIDEA.Core/Tokens/VascoToken.cs
Python: privacyidea/lib/tokens/vascotoken.py
```
- VASCO Digipass support
- Proprietary OTP algorithm

### Task 20.4: SPass Token
```
File: src/PrivacyIDEA.Core/Tokens/SpassToken.cs
Python: privacyidea/lib/tokens/spasstoken.py
```
- Simple Password token
- Static password with PIN

**Ước tính:** 1-2 ngày

---

## 📅 PHASE 21: User Resolver Còn Thiếu

**Mục tiêu:** 78% → 100% (thêm 1 resolver)

### Task 21.1: Keycloak Resolver
```
File: src/PrivacyIDEA.Core/Resolvers/KeycloakResolver.cs
Python: privacyidea/lib/resolvers/KeycloakResolver.py
```
Features:
- Keycloak user federation
- OAuth2/OIDC integration
- User attribute mapping
- Group membership

**Ước tính:** 1 ngày

---

## 📅 PHASE 22: SMS Provider Còn Thiếu

**Mục tiêu:** 86% → 100% (thêm 1 provider)

### Task 22.1: Script SMS Provider
```
File: src/PrivacyIDEA.Core/SmsProviders/ScriptSmsProvider.cs
Python: privacyidea/lib/smsprovider/ScriptSMSProvider.py
```
Features:
- Execute external script
- Pass phone number and message
- Capture return code

**Ước tính:** 0.5 ngày

---

## 📅 PHASE 23: CLI Commands Mở Rộng

**Mục tiêu:** 28% → 80%

### Task 23.1: Policy Commands
```
Commands:
- pi-manage policy list
- pi-manage policy create
- pi-manage policy delete
- pi-manage policy enable/disable
```

### Task 23.2: Event Handler Commands
```
Commands:
- pi-manage event list
- pi-manage event create
- pi-manage event delete
- pi-manage event enable/disable
```

### Task 23.3: Audit Commands
```
Commands:
- pi-manage audit list [--user] [--date] [--action]
- pi-manage audit export [--format csv|json]
- pi-manage audit rotate
```

### Task 23.4: Server Config Commands
```
Commands:
- pi-manage smtpserver list|create|delete
- pi-manage radiusserver list|create|delete
- pi-manage smsgateway list|create|delete
```

### Task 23.5: Database Commands
```
Commands:
- pi-manage db migrate
- pi-manage db upgrade
- pi-manage db backup
- pi-manage db restore
```

### Task 23.6: Import/Export Commands
```
Commands:
- pi-manage token import [--file]
- pi-manage token export [--file]
- pi-manage config export
- pi-manage config import
```

**Ước tính:** 2-3 ngày

---

## 📅 PHASE 24: Services Bổ Sung

### Task 24.1: ICAConnectorService
```
File: src/PrivacyIDEA.Core/Interfaces/ICAConnectorService.cs
File: src/PrivacyIDEA.Core/Services/CAConnectorService.cs
```
- Manage CA connectors
- Request/revoke certificates
- Support local CA, Microsoft CA

### Task 24.2: ITokenGroupService
```
File: src/PrivacyIDEA.Core/Interfaces/ITokenGroupService.cs
File: src/PrivacyIDEA.Core/Services/TokenGroupService.cs
```
- Manage token groups
- Add/remove tokens from groups

### Task 24.3: IContainerService
```
File: src/PrivacyIDEA.Core/Interfaces/IContainerService.cs
File: src/PrivacyIDEA.Core/Services/ContainerService.cs
```
- Manage token containers
- Container lifecycle

### Task 24.4: IMonitoringService
```
File: src/PrivacyIDEA.Core/Interfaces/IMonitoringService.cs
File: src/PrivacyIDEA.Core/Services/MonitoringService.cs
```
- Collect statistics
- Event counters

**Ước tính:** 2 ngày

---

## 📊 TỔNG HỢP

| Phase | Nội dung | Ước tính |
|-------|----------|----------|
| Phase 18 | API Controllers (13) | 3-4 ngày |
| Phase 19 | Event Handlers (6) | 2 ngày |
| Phase 20 | Token Types (4) | 1-2 ngày |
| Phase 21 | Keycloak Resolver | 1 ngày |
| Phase 22 | Script SMS Provider | 0.5 ngày |
| Phase 23 | CLI Commands | 2-3 ngày |
| Phase 24 | Services | 2 ngày |

**Tổng cộng: 11.5 - 14.5 ngày làm việc**

---

## ✅ CHECKLIST HOÀN THÀNH

### API Controllers (Phase 18)
- [ ] CAConnectorController
- [ ] MachineResolverController
- [ ] TokenGroupController
- [ ] SubscriptionController
- [ ] PrivacyIDEAServerController
- [ ] ServiceIdController
- [ ] MonitoringController
- [ ] ClientTypeController
- [ ] InfoController
- [ ] TokenTypeController

### Event Handlers (Phase 19)
- [ ] FederationEventHandler
- [ ] RequestManglerHandler
- [ ] ResponseManglerHandler
- [ ] CustomUserAttributeHandler
- [ ] ContainerHandler
- [ ] TokenGroupHandler

### Token Types (Phase 20)
- [ ] DaplugToken
- [ ] TiqrToken
- [ ] VascoToken
- [ ] SpassToken

### Resolvers (Phase 21)
- [ ] KeycloakResolver

### SMS Providers (Phase 22)
- [ ] ScriptSmsProvider

### CLI Commands (Phase 23)
- [ ] Policy commands
- [ ] Event commands
- [ ] Audit commands
- [ ] Server config commands
- [ ] Database commands
- [ ] Import/Export commands

### Services (Phase 24)
- [ ] CAConnectorService
- [ ] TokenGroupService
- [ ] ContainerService
- [ ] MonitoringService

---

## 🚀 THỨ TỰ THỰC HIỆN ĐỀ XUẤT

1. **Phase 24** (Services) - Tạo foundation cho các phase khác
2. **Phase 18** (API Controllers) - Nhiều nhất, cần làm sớm
3. **Phase 19** (Event Handlers) - Phụ thuộc services
4. **Phase 20** (Token Types) - Độc lập
5. **Phase 21** (Keycloak) - Độc lập
6. **Phase 22** (SMS) - Nhanh
7. **Phase 23** (CLI) - Cuối cùng

---

*Kế hoạch được tạo bởi PrivacyIDEA Migration Tool*
