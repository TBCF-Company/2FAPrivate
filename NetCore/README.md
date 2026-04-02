# PrivacyIDEA Server - .NET Core 8 Port

> 📖 **[Tài liệu Tiếng Việt](TÀI_LIỆU_TIẾNG_VIỆT.md)** | **[Vietnamese Documentation](README_VI.md)**

This is a .NET Core 8 port of the Python privacyIDEA server application from the `privacyidea` folder. It implements the same 2FA (Two-Factor Authentication) server functionality using ASP.NET Core.

## Overview

This application provides a REST API for managing remote privacyIDEA server configurations. It allows you to:
- Create and update privacyIDEA server definitions
- List all server configurations
- Delete server configurations
- Test server connections

## Components

### Models
- **PrivacyIDEAServerDB**: Database model for server configurations
- **PrivacyIDEAContext**: Entity Framework DbContext for database operations

### Library (Lib)
- **PrivacyIDEAServer**: Core class for server operations and validation
- **IPrivacyIDEAServerService**: Service interface
- **PrivacyIDEAServerService**: Service implementation for CRUD operations

### Controllers
- **PrivacyIDEAServerController**: REST API endpoints

## API Endpoints

All endpoints follow the same structure as the Python Flask application:

### List Servers
```
GET /privacyideaserver
```
Returns a list of all configured privacyIDEA servers.

### Create/Update Server
```
POST /privacyideaserver/{identifier}
Content-Type: application/json

{
  "url": "https://server.example.com",
  "tls": true,
  "description": "Main authentication server"
}
```

### Delete Server
```
DELETE /privacyideaserver/{identifier}
```

### Test Server Connection
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

## Running the Application

### Prerequisites
- .NET 8.0 SDK or later

### Build
```bash
cd NetCore/PrivacyIdeaServer
dotnet restore
dotnet build
```

### Run
```bash
dotnet run
```

The application will start on `https://localhost:5001` (HTTPS) and `http://localhost:5000` (HTTP).

### Access Swagger UI
Navigate to `https://localhost:5001/swagger` to see the interactive API documentation.

## Database

The application uses SQLite by default for simplicity. The database file `privacyidea.db` will be created automatically in the application directory.

To use a different database (SQL Server, PostgreSQL, MySQL, etc.):
1. Update the connection string in `appsettings.json`
2. Install the appropriate Entity Framework provider NuGet package
3. Update the `UseSqlite()` call in `Program.cs` to use the correct provider

## Configuration

Configuration is stored in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=privacyidea.db"
  }
}
```

## License

This code is licensed under the GNU AFFERO GENERAL PUBLIC LICENSE Version 3 or later (AGPL-3.0-or-later), matching the original Python privacyIDEA project.

## Original Python Code

This is a port of the privacyIDEA server code located in:
- `privacyidea/privacyidea/lib/privacyideaserver.py` - Core library functions
- `privacyidea/privacyidea/api/privacyideaserver.py` - REST API endpoints
- `privacyidea/privacyidea/models.py` - Database models

The functionality has been preserved as closely as possible while adapting to .NET Core idioms and best practices.
