# Migration Guide: In-Memory to Database Persistence

This guide explains how to migrate from in-memory device management to database-persisted storage.

## Overview

The device management system originally used in-memory storage (ConcurrentDictionary/Map), which loses all data during application restarts. This migration adds database persistence to prevent data loss during recovery scenarios.

## Why Migrate?

### Problems with In-Memory Storage
- ❌ All device activations lost on application restart
- ❌ Server recovery requires re-activation of all devices
- ❌ Deployment updates force users to re-activate
- ❌ No backup of device information
- ❌ Cannot scale horizontally (no shared state)

### Benefits of Database Persistence
- ✅ Device activations survive restarts
- ✅ Fast recovery without user intervention
- ✅ Seamless deployments and updates
- ✅ Database backups protect device data
- ✅ Can scale horizontally with shared database

## .NET/Blazor Migration

### Prerequisites

- PostgreSQL database (same database as PrivacyIDEA)
- Entity Framework Core (already included)
- Access to database for running migrations

### Step 1: Apply Database Migration

Navigate to the migrations directory:

```bash
cd NetCore/PrivacyIdeaServer/Migrations
```

Apply the migration using psql:

```bash
psql -U your_username -d privacyidea -f 001_AddDeviceManagement.sql
```

Or using environment variables:

```bash
export PGPASSWORD=your_password
psql -h localhost -U privacyidea_user -d privacyidea -f 001_AddDeviceManagement.sql
```

Verify tables were created:

```bash
psql -U your_username -d privacyidea -c "\dt device*"
```

You should see:
- `device`
- `pending_device_activation`

### Step 2: Service Registration (Already Done)

The `Program.cs` has been updated to use the database-backed service:

```csharp
// Old: In-memory storage
// builder.Services.AddSingleton<IDeviceManagementService, DeviceManagementService>();

// New: Database persistence
builder.Services.AddScoped<IDeviceManagementService, DatabaseDeviceManagementService>();
```

✅ **No code changes needed** - the service registration is already updated.

### Step 3: Deploy and Restart

1. **Build the application**:
   ```bash
   dotnet build --configuration Release
   ```

2. **Stop the application**:
   ```bash
   # If using systemd
   sudo systemctl stop privacyidea-server
   ```

3. **Deploy the new version**

4. **Start the application**:
   ```bash
   sudo systemctl start privacyidea-server
   ```

### Step 4: Verify Migration

Test device activation flow:

```bash
curl -X POST http://localhost:5000/api/device/request-activation \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "test-device-123",
    "deviceName": "Test Device",
    "platform": "iOS",
    "osVersion": "17.0",
    "model": "iPhone 13"
  }'
```

Restart the application and verify the device still exists:

```bash
curl http://localhost:5000/api/device/test-device-123/status
```

The device should still be present after restart. ✅

### Step 5: Optional - Set Up Cleanup Job

Add a background service to periodically clean up expired activations:

```csharp
public class DeviceCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    
    public DeviceCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            
            using var scope = _serviceProvider.CreateScope();
            var deviceService = scope.ServiceProvider
                .GetRequiredService<IDeviceManagementService>() as DatabaseDeviceManagementService;
            
            if (deviceService != null)
            {
                await deviceService.CleanupExpiredActivationsAsync();
            }
        }
    }
}

// Register in Program.cs
builder.Services.AddHostedService<DeviceCleanupService>();
```

## NodeJS Migration

### Option 1: PostgreSQL (Recommended)

#### Step 1: Install Dependencies

```bash
npm install pg
npm install @types/pg --save-dev
```

#### Step 2: Apply Database Migration

Use the same PostgreSQL migration as .NET:

```bash
psql -U your_username -d privacyidea -f NetCore/PrivacyIdeaServer/Migrations/001_AddDeviceManagement.sql
```

#### Step 3: Implement Database Service

Create `PostgresDeviceManagementService.ts` - see [DATABASE_INTEGRATION.md](../nodejs_package/DATABASE_INTEGRATION.md) for complete implementation.

#### Step 4: Update Service Registration

```typescript
// Old: In-memory storage
// const deviceService = new DeviceManagementService();

// New: Database persistence
const deviceService = new PostgresDeviceManagementService(
  process.env.DATABASE_URL || 'postgresql://user:password@localhost:5432/privacyidea'
);
```

#### Step 5: Deploy and Test

Same verification steps as .NET migration above.

### Option 2: MongoDB

#### Step 1: Install Dependencies

```bash
npm install mongodb
```

#### Step 2: No Migration Needed

MongoDB creates collections automatically.

#### Step 3: Implement Database Service

See [DATABASE_INTEGRATION.md](../nodejs_package/DATABASE_INTEGRATION.md) for `MongoDeviceManagementService` implementation.

#### Step 4: Set Up Indexes

```typescript
await devicesCollection.createIndex({ deviceId: 1 }, { unique: true });
await devicesCollection.createIndex({ isActivated: 1 });
await pendingCollection.createIndex({ deviceId: 1 }, { unique: true });
await pendingCollection.createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });
```

#### Step 5: Update Service Registration

```typescript
const mongoClient = new MongoClient('mongodb://localhost:27017');
await mongoClient.connect();

const deviceService = new MongoDeviceManagementService(mongoClient, 'privacyidea');
```

## Rollback Plan

If you need to rollback to in-memory storage:

### .NET

1. Update `Program.cs`:
   ```csharp
   builder.Services.AddSingleton<IDeviceManagementService, DeviceManagementService>();
   ```

2. Restart application

3. **Note**: All devices in database will be inaccessible but data is preserved

### NodeJS

1. Revert service registration to use `DeviceManagementService`

2. Restart application

### Database Cleanup (Optional)

If you want to remove the database tables:

```sql
DROP TABLE IF EXISTS pending_device_activation;
DROP TABLE IF EXISTS device;
```

## Data Migration Notes

### No Data Loss

The migration is **forward-compatible**:
- New database-backed service creates tables if they don't exist
- Existing in-memory devices are lost (expected during migration)
- New device activations are persisted to database

### Coordinating the Migration

**Recommended Approach**: Deploy during maintenance window

1. Notify users of maintenance window
2. All active devices will need to re-activate after migration
3. Apply database migration
4. Deploy new version
5. Verify functionality
6. Resume normal operations

**Alternative**: Gradual migration (not recommended)
- Old and new versions cannot share device state
- Results in inconsistent behavior

## Monitoring

After migration, monitor:

1. **Database Connections**: Ensure connection pool is properly sized
   ```sql
   SELECT count(*) FROM pg_stat_activity WHERE datname = 'privacyidea';
   ```

2. **Device Table Growth**:
   ```sql
   SELECT count(*) FROM device;
   SELECT count(*) FROM device WHERE "IsActivated" = true;
   ```

3. **Expired Activations** (should be cleaned up):
   ```sql
   SELECT count(*) FROM pending_device_activation 
   WHERE "ExpiresAt" < CURRENT_TIMESTAMP;
   ```

4. **Application Logs**: Check for database errors

## Troubleshooting

### Issue: Database Connection Errors

**Symptom**: Application fails to start or device operations fail

**Solution**:
1. Verify database connection string in `appsettings.json`
2. Check database is running: `pg_isready -h localhost`
3. Verify user has permissions: `GRANT ALL ON DATABASE privacyidea TO your_user;`

### Issue: Tables Not Created

**Symptom**: Error about missing tables

**Solution**:
1. Run migration manually: `psql -f 001_AddDeviceManagement.sql`
2. Verify tables exist: `\dt device*`
3. Check user has CREATE permission

### Issue: Performance Problems

**Symptom**: Slow device operations

**Solution**:
1. Verify indexes were created:
   ```sql
   SELECT indexname FROM pg_indexes WHERE tablename = 'device';
   ```
2. Increase connection pool size in `appsettings.json`
3. Monitor query performance with `EXPLAIN ANALYZE`

### Issue: Expired Activations Not Cleaned

**Symptom**: Large number of expired entries in `pending_device_activation`

**Solution**:
1. Run manual cleanup:
   ```sql
   DELETE FROM pending_device_activation WHERE "ExpiresAt" < CURRENT_TIMESTAMP;
   ```
2. Set up background cleanup service (see Step 5 above)
3. Consider PostgreSQL's built-in job scheduler

## Best Practices

1. **Backup Before Migration**: Take a database backup before applying migrations
2. **Test in Staging**: Test the migration in a staging environment first
3. **Monitor Closely**: Watch logs and database metrics after deployment
4. **Document Changes**: Keep track of which version includes database persistence
5. **Coordinate with Users**: Inform users they may need to re-activate devices

## Support

For questions or issues with migration:
- Check [DATABASE_INTEGRATION.md](../nodejs_package/DATABASE_INTEGRATION.md) for NodeJS examples
- Check [Migrations README](../NetCore/PrivacyIdeaServer/Migrations/README.md) for schema details
- Open an issue on GitHub

## Version Compatibility

- **Minimum .NET Version**: .NET 8.0
- **Minimum EF Core Version**: 8.0.23
- **PostgreSQL Version**: 12+ (tested with 14)
- **MongoDB Version**: 4.4+ (for NodeJS)

## Summary

✅ Database migration adds persistence without breaking changes  
✅ Same API interface - no client-side changes needed  
✅ Recovery-safe - survives restarts and deployments  
✅ Backward compatible - can rollback if needed  
✅ Scalable - supports horizontal scaling with shared database
