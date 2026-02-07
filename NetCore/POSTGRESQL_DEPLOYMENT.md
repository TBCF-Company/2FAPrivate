# PostgreSQL Deployment Guide

## Prerequisites

- .NET 8.0 SDK or later
- PostgreSQL 12 or later
- Basic knowledge of ASP.NET Core and PostgreSQL

## Quick Start

### 1. Install PostgreSQL

#### Ubuntu/Debian:
```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

#### macOS (using Homebrew):
```bash
brew install postgresql@16
brew services start postgresql@16
```

#### Windows:
Download and install from https://www.postgresql.org/download/windows/

### 2. Create Database and User

```bash
# Switch to postgres user
sudo -u postgres psql

# In psql console:
CREATE DATABASE privacyidea;
CREATE USER privacyidea_user WITH ENCRYPTED PASSWORD 'your_secure_password_here';
GRANT ALL PRIVILEGES ON DATABASE privacyidea TO privacyidea_user;

# Grant schema privileges (PostgreSQL 15+)
\c privacyidea
GRANT ALL ON SCHEMA public TO privacyidea_user;

\q
```

### 3. Configure Connection String

**Option A: Environment Variable (Recommended for Production)**

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=your_secure_password_here;Port=5432"
```

**Option B: User Secrets (Development)**

```bash
cd NetCore/PrivacyIdeaServer
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=your_secure_password_here;Port=5432"
```

**Option C: appsettings.json (Not Recommended)**

Edit `appsettings.json` and update the connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=your_secure_password_here;Port=5432"
  }
}
```

⚠️ **Security Warning**: Never commit credentials to version control!

### 4. Install EF Core Tools (if not already installed)

```bash
dotnet tool install --global dotnet-ef
```

### 5. Create and Apply Migrations

```bash
cd NetCore/PrivacyIdeaServer

# Create initial migration
dotnet ef migrations add InitialCreate

# Review the generated migration in Migrations/
# Then apply it:
dotnet ef database update
```

### 6. Run the Application

```bash
dotnet run
```

The application will start on:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

Visit https://localhost:5001/swagger for API documentation.

## Production Deployment

### 1. Build for Production

```bash
cd NetCore/PrivacyIdeaServer
dotnet publish -c Release -o /path/to/publish
```

### 2. Set Environment Variables

Create a systemd service file or use your hosting environment's configuration:

```bash
# /etc/environment or systemd service file
ConnectionStrings__DefaultConnection="Host=your-db-server.com;Database=privacyidea;Username=privacyidea_user;Password=STRONG_PASSWORD;Port=5432;SSL Mode=Require"
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000;https://0.0.0.0:5001
```

### 3. Use Secrets Management

**Azure Key Vault:**
```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
```

**AWS Secrets Manager:**
```bash
dotnet add package AWSSDK.SecretsManager
```

**HashiCorp Vault:**
```bash
dotnet add package VaultSharp
```

### 4. SSL/TLS Configuration

For PostgreSQL SSL connection:
```
Host=your-db-server.com;Database=privacyidea;Username=privacyidea_user;Password=PASSWORD;Port=5432;SSL Mode=Require;Trust Server Certificate=false;Root Certificate=/path/to/ca.crt
```

### 5. Reverse Proxy Setup

#### Nginx:
```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

#### Apache:
```apache
<VirtualHost *:80>
    ServerName your-domain.com
    
    ProxyPreserveHost On
    ProxyPass / http://localhost:5000/
    ProxyPassReverse / http://localhost:5000/
    
    RequestHeader set X-Forwarded-Proto "http"
    RequestHeader set X-Forwarded-Port "80"
</VirtualHost>
```

## Database Maintenance

### Backup

```bash
# Full backup
pg_dump -h localhost -U privacyidea_user privacyidea > backup_$(date +%Y%m%d).sql

# Compressed backup
pg_dump -h localhost -U privacyidea_user privacyidea | gzip > backup_$(date +%Y%m%d).sql.gz
```

### Restore

```bash
# Restore from backup
psql -h localhost -U privacyidea_user privacyidea < backup_20260207.sql

# Restore from compressed backup
gunzip -c backup_20260207.sql.gz | psql -h localhost -U privacyidea_user privacyidea
```

### Migration Management

```bash
# List migrations
dotnet ef migrations list

# Generate SQL script for all migrations
dotnet ef migrations script -o migration.sql

# Rollback to specific migration
dotnet ef database update MigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove
```

## Performance Tuning

### PostgreSQL Configuration

Edit `/etc/postgresql/16/main/postgresql.conf`:

```conf
# Memory
shared_buffers = 256MB
effective_cache_size = 1GB
work_mem = 16MB
maintenance_work_mem = 64MB

# Connections
max_connections = 200

# Write-Ahead Log
wal_buffers = 16MB
checkpoint_completion_target = 0.9

# Query Planner
random_page_cost = 1.1  # For SSD
effective_io_concurrency = 200
```

Restart PostgreSQL:
```bash
sudo systemctl restart postgresql
```

### Connection Pooling

In connection string:
```
Host=localhost;Database=privacyidea;Username=privacyidea_user;Password=PASSWORD;Port=5432;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100
```

### Indexes

Key indexes are automatically created by EF Core migrations, but you may want to add custom indexes:

```sql
-- Index on token serial (most common query)
CREATE INDEX idx_token_serial ON token(serial);

-- Index on token type and active status
CREATE INDEX idx_token_type_active ON token(tokentype, active);

-- Index on user lookups
CREATE INDEX idx_token_owner ON token_owner(user_id);
```

## Monitoring

### Application Health Check

```bash
curl https://localhost:5001/healthz
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2026-02-07T12:00:00Z"
}
```

### Database Connection Check

```bash
psql -h localhost -U privacyidea_user -d privacyidea -c "SELECT version();"
```

### Logging

Configure logging in `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "File": {
      "Path": "/var/log/privacyidea/app.log",
      "RollingInterval": "Day"
    }
  }
}
```

## Security Best Practices

1. **Never store credentials in code or configuration files**
2. **Always use SSL/TLS for database connections in production**
3. **Use strong passwords (minimum 16 characters, mixed case, numbers, symbols)**
4. **Regularly update dependencies**: `dotnet list package --vulnerable`
5. **Enable audit logging** for all authentication attempts
6. **Implement rate limiting** to prevent brute force attacks
7. **Use firewall rules** to restrict database access
8. **Regular backups** with encryption
9. **Monitor for suspicious activity**
10. **Keep PostgreSQL and .NET runtime up to date**

## Troubleshooting

### Connection Refused

```bash
# Check if PostgreSQL is running
sudo systemctl status postgresql

# Check if PostgreSQL is listening
sudo netstat -nlp | grep 5432

# Check PostgreSQL logs
sudo tail -f /var/log/postgresql/postgresql-16-main.log
```

### Authentication Failed

```bash
# Check pg_hba.conf
sudo nano /etc/postgresql/16/main/pg_hba.conf

# Should have a line like:
# host    privacyidea    privacyidea_user    127.0.0.1/32    md5

# Reload configuration
sudo systemctl reload postgresql
```

### Migration Errors

```bash
# Reset database (CAUTION: Destroys all data!)
dotnet ef database drop --force
dotnet ef database update

# Check migration status
dotnet ef migrations list
```

### Performance Issues

```bash
# Check slow queries
psql -U privacyidea_user -d privacyidea

SELECT query, calls, total_time, mean_time
FROM pg_stat_statements
ORDER BY mean_time DESC
LIMIT 10;

# Analyze table statistics
ANALYZE token;
VACUUM ANALYZE token;
```

## Support

- Documentation: See POSTGRESQL_MIGRATION_SUMMARY.md
- Issues: GitHub Issues
- Security: Report privately to security@example.com

## License

AGPL-3.0-or-later
