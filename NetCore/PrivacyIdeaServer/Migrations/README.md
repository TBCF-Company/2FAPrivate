# Database Migrations

This directory contains database migration scripts for PrivacyIDEA Server.

## Applying Migrations

### Using PostgreSQL

```bash
psql -U your_username -d your_database -f 001_AddDeviceManagement.sql
```

Or using environment variables:

```bash
export PGPASSWORD=your_password
psql -h localhost -U your_username -d privacyidea -f 001_AddDeviceManagement.sql
```

## Migration History

### 001_AddDeviceManagement.sql
- **Date**: 2026-02-07
- **Purpose**: Add device management tables for 2FA device activation
- **Tables Created**:
  - `device` - Stores registered devices with activation status
  - `pending_device_activation` - Stores temporary activation requests (5-minute expiry)
- **Reason**: Prevent data loss during application restarts and recovery scenarios by persisting device information to database

## Notes

- All migrations use `IF NOT EXISTS` clauses to be idempotent
- Migrations are designed for PostgreSQL database
- Timestamps use UTC timezone
- Indexes are created for frequently queried columns

## Database Schema

### Device Table
Stores all registered devices, both activated and pending.

| Column | Type | Description |
|--------|------|-------------|
| Id | SERIAL | Primary key |
| DeviceId | VARCHAR(255) | Unique device identifier (from client) |
| DeviceName | VARCHAR(255) | Human-readable device name |
| Platform | VARCHAR(50) | Device platform (iOS, Android, Web) |
| OsVersion | VARCHAR(100) | Operating system version |
| Model | VARCHAR(255) | Device model information |
| RegisteredAt | TIMESTAMP | Registration timestamp (UTC) |
| IsActivated | BOOLEAN | Activation status |
| ActivatedAt | TIMESTAMP | Activation timestamp (UTC) |
| ActivationToken | VARCHAR(255) | Unique activation token (UUID) |

### PendingDeviceActivation Table
Stores temporary activation requests that expire after 5 minutes.

| Column | Type | Description |
|--------|------|-------------|
| Id | SERIAL | Primary key |
| DeviceId | VARCHAR(255) | Device identifier |
| Secret | VARCHAR(500) | Base32-encoded OTP secret |
| CreatedAt | TIMESTAMP | Creation timestamp (UTC) |
| ExpiresAt | TIMESTAMP | Expiration timestamp (UTC) |

## Cleanup

The `DatabaseDeviceManagementService` includes a `CleanupExpiredActivationsAsync()` method that should be called periodically to remove expired pending activations. Consider setting up a background service to run this cleanup every few minutes.
