# Architecture Diagram

## .NET Core 8 Application Architecture

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
│                    (API Layer)                                   │
├─────────────────────────────────────────────────────────────────┤
│  - Create/Update server configurations                          │
│  - List all servers                                             │
│  - Delete servers                                               │
│  - Test server connections                                      │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│           IPrivacyIDEAServerService                             │
│              (Business Logic Layer)                             │
├─────────────────────────────────────────────────────────────────┤
│  Implementation: PrivacyIDEAServerService                       │
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
│   (Validation Logic)    │              │  (Entity Framework)    │
├─────────────────────────┤              ├────────────────────────┤
│ - ValidateCheckAsync()  │              │ DbSet<Server>          │
│ - RequestAsync()        │              │ SaveChangesAsync()     │
└─────────────────────────┘              └────────────────────────┘
        │                                                 │
        ▼                                                 ▼
┌─────────────────────────┐              ┌────────────────────────┐
│  HttpClient/Factory     │              │  SQLite Database       │
│  (External Requests)    │              │  (privacyidea.db)      │
├─────────────────────────┤              ├────────────────────────┤
│ POST /validate/check    │              │ Table:                 │
│ to remote servers       │              │  privacyideaserver     │
└─────────────────────────┘              └────────────────────────┘
```

## Component Responsibilities

### Controllers Layer
- **PrivacyIDEAServerController**: Handles HTTP requests and responses
  - Request validation
  - Response formatting
  - Error handling
  - Logging

### Service Layer
- **IPrivacyIDEAServerService**: Interface for business logic
- **PrivacyIDEAServerService**: Implementation of CRUD operations
  - Database operations
  - Business rules
  - Data transformation

### Library Layer
- **PrivacyIDEAServer**: Core validation and communication logic
  - HTTP requests to remote servers
  - Certificate validation (TLS)
  - Response parsing

### Data Layer
- **PrivacyIDEAContext**: EF Core DbContext
  - Database connection management
  - Change tracking
  - Migrations
- **PrivacyIDEAServerDB**: Database model
  - Entity definition
  - Validation rules

## Data Flow

### Creating a Server
```
Client Request
    ↓
Controller validates request
    ↓
Service creates/updates entity
    ↓
DbContext saves to database
    ↓
Controller returns response to client
```

### Testing a Server Connection
```
Client Request
    ↓
Controller receives test parameters
    ↓
PrivacyIDEAServer.RequestAsync()
    ↓
HttpClient sends POST to remote server
    ↓
Parse response
    ↓
Return success/failure to client
```

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8.0 |
| Language | C# 12 |
| ORM | Entity Framework Core 8.0 |
| Database | SQLite (configurable) |
| API Docs | Swagger/OpenAPI |
| HTTP Client | IHttpClientFactory |
| DI Container | Built-in ASP.NET Core DI |
| Logging | ILogger / Microsoft.Extensions.Logging |

## Comparison with Python Version

| Component | Python | .NET Core |
|-----------|--------|-----------|
| Web Framework | Flask | ASP.NET Core |
| ORM | SQLAlchemy | Entity Framework Core |
| HTTP Client | requests | HttpClient |
| Dependency Injection | Manual/Flask-Injector | Built-in |
| API Documentation | Manual/Swagger | Swagger/OpenAPI (built-in) |
| Async Support | asyncio (optional) | async/await (native) |
| Type Safety | Dynamic typing | Static typing |
