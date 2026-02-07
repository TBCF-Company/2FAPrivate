# NodeJS Database Integration Guide

This guide explains how to integrate database persistence for device management in NodeJS applications.

## Overview

The current `DeviceManagementService` in the NodeJS package uses in-memory storage (Map), which loses all data on application restart. This guide shows how to implement database persistence using MongoDB or PostgreSQL.

## Option 1: MongoDB Implementation

### Installation

```bash
npm install mongodb
```

### Implementation

```typescript
import { MongoClient, Db, Collection } from 'mongodb';
import { DeviceInfo, ActivationRequest, ActivationResponse } from '@tbcf/two-factor-auth';

interface PendingActivation {
  deviceId: string;
  secret: string;
  expiresAt: Date;
}

export class MongoDeviceManagementService {
  private db: Db;
  private devicesCollection: Collection<DeviceInfo>;
  private pendingCollection: Collection<PendingActivation>;
  
  constructor(mongoClient: MongoClient, dbName: string = 'privacyidea') {
    this.db = mongoClient.db(dbName);
    this.devicesCollection = this.db.collection('devices');
    this.pendingCollection = this.db.collection('pending_activations');
    
    // Create indexes
    this.setupIndexes();
  }
  
  private async setupIndexes() {
    await this.devicesCollection.createIndex({ deviceId: 1 }, { unique: true });
    await this.devicesCollection.createIndex({ isActivated: 1 });
    await this.pendingCollection.createIndex({ deviceId: 1 }, { unique: true });
    await this.pendingCollection.createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });
  }
  
  async requestDeviceActivation(request: ActivationRequest): Promise<ActivationResponse> {
    // Generate secret and OTP
    const secret = this.generateSecret();
    const otpCode = this.generateTotp(secret);
    
    // Store in database
    const expiresAt = new Date(Date.now() + 5 * 60 * 1000); // 5 minutes
    
    await this.pendingCollection.updateOne(
      { deviceId: request.deviceId },
      { $set: { deviceId: request.deviceId, secret, expiresAt } },
      { upsert: true }
    );
    
    await this.devicesCollection.updateOne(
      { deviceId: request.deviceId },
      { 
        $set: {
          deviceId: request.deviceId,
          deviceName: request.deviceName || 'Unknown Device',
          platform: request.platform || 'Unknown',
          osVersion: request.osVersion || 'Unknown',
          model: request.model || 'Unknown',
          registeredAt: new Date(),
          isActivated: false
        }
      },
      { upsert: true }
    );
    
    return {
      success: true,
      message: 'Activation OTP generated',
      secret,
      otpCode,
      deviceId: request.deviceId
    };
  }
  
  async getDeviceInfo(deviceId: string): Promise<DeviceInfo | null> {
    return await this.devicesCollection.findOne({ deviceId });
  }
  
  async getActivatedDevices(): Promise<DeviceInfo[]> {
    return await this.devicesCollection.find({ isActivated: true }).toArray();
  }
  
  async deactivateDevice(deviceId: string): Promise<boolean> {
    const result = await this.devicesCollection.updateOne(
      { deviceId },
      { 
        $set: { 
          isActivated: false,
          activatedAt: null,
          activationToken: null
        }
      }
    );
    return result.modifiedCount > 0;
  }
  
  // Helper methods
  private generateSecret(): string {
    // Implementation from OtpManager
  }
  
  private generateTotp(secret: string): string {
    // Implementation from OtpManager
  }
}
```

### Usage

```typescript
import { MongoClient } from 'mongodb';
import { MongoDeviceManagementService } from './MongoDeviceManagementService';

const client = new MongoClient('mongodb://localhost:27017');
await client.connect();

const deviceService = new MongoDeviceManagementService(client, 'privacyidea');

app.post('/api/device/request-activation', async (req, res) => {
  const result = await deviceService.requestDeviceActivation(req.body);
  res.json(result);
});
```

## Option 2: PostgreSQL Implementation

### Installation

```bash
npm install pg
npm install @types/pg --save-dev
```

### Database Schema

```sql
CREATE TABLE devices (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(255) NOT NULL UNIQUE,
    device_name VARCHAR(255) NOT NULL,
    platform VARCHAR(50) NOT NULL,
    os_version VARCHAR(100) NOT NULL,
    model VARCHAR(255) NOT NULL,
    registered_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_activated BOOLEAN NOT NULL DEFAULT FALSE,
    activated_at TIMESTAMP,
    activation_token VARCHAR(255)
);

CREATE INDEX idx_devices_device_id ON devices(device_id);
CREATE INDEX idx_devices_is_activated ON devices(is_activated);

CREATE TABLE pending_device_activations (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(255) NOT NULL UNIQUE,
    secret VARCHAR(500) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL
);

CREATE INDEX idx_pending_device_id ON pending_device_activations(device_id);
CREATE INDEX idx_pending_expires_at ON pending_device_activations(expires_at);
```

### Implementation

```typescript
import { Pool } from 'pg';
import { DeviceInfo, ActivationRequest, ActivationResponse } from '@tbcf/two-factor-auth';

export class PostgresDeviceManagementService {
  private pool: Pool;
  
  constructor(connectionString: string) {
    this.pool = new Pool({ connectionString });
  }
  
  async requestDeviceActivation(request: ActivationRequest): Promise<ActivationResponse> {
    const client = await this.pool.connect();
    
    try {
      await client.query('BEGIN');
      
      // Generate secret and OTP
      const secret = this.generateSecret();
      const otpCode = this.generateTotp(secret);
      const expiresAt = new Date(Date.now() + 5 * 60 * 1000);
      
      // Store pending activation
      await client.query(
        `INSERT INTO pending_device_activations (device_id, secret, expires_at)
         VALUES ($1, $2, $3)
         ON CONFLICT (device_id) 
         DO UPDATE SET secret = $2, expires_at = $3, created_at = CURRENT_TIMESTAMP`,
        [request.deviceId, secret, expiresAt]
      );
      
      // Store device info
      await client.query(
        `INSERT INTO devices (device_id, device_name, platform, os_version, model)
         VALUES ($1, $2, $3, $4, $5)
         ON CONFLICT (device_id)
         DO UPDATE SET device_name = $2, platform = $3, os_version = $4, model = $5, registered_at = CURRENT_TIMESTAMP`,
        [
          request.deviceId,
          request.deviceName || 'Unknown Device',
          request.platform || 'Unknown',
          request.osVersion || 'Unknown',
          request.model || 'Unknown'
        ]
      );
      
      await client.query('COMMIT');
      
      return {
        success: true,
        message: 'Activation OTP generated',
        secret,
        otpCode,
        deviceId: request.deviceId
      };
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }
  
  async getDeviceInfo(deviceId: string): Promise<DeviceInfo | null> {
    const result = await this.pool.query(
      'SELECT * FROM devices WHERE device_id = $1',
      [deviceId]
    );
    
    if (result.rows.length === 0) return null;
    
    return this.mapRowToDeviceInfo(result.rows[0]);
  }
  
  async getActivatedDevices(): Promise<DeviceInfo[]> {
    const result = await this.pool.query(
      'SELECT * FROM devices WHERE is_activated = true'
    );
    
    return result.rows.map(this.mapRowToDeviceInfo);
  }
  
  async deactivateDevice(deviceId: string): Promise<boolean> {
    const result = await this.pool.query(
      `UPDATE devices 
       SET is_activated = false, activated_at = NULL, activation_token = NULL
       WHERE device_id = $1`,
      [deviceId]
    );
    
    return result.rowCount > 0;
  }
  
  async cleanupExpiredActivations(): Promise<void> {
    await this.pool.query(
      'DELETE FROM pending_device_activations WHERE expires_at < CURRENT_TIMESTAMP'
    );
  }
  
  // Helper method to map database row to DeviceInfo object
  // Note: This is synchronous as it only performs object mapping (no async operations)
  private mapRowToDeviceInfo(row: any): DeviceInfo {
    return {
      deviceId: row.device_id,
      deviceName: row.device_name,
      platform: row.platform,
      osVersion: row.os_version,
      model: row.model,
      registeredAt: row.registered_at,
      isActivated: row.is_activated,
      activatedAt: row.activated_at,
      activationToken: row.activation_token
    };
  }
  
  // Helper methods
  private generateSecret(): string {
    // Implementation from OtpManager
  }
  
  private generateTotp(secret: string): string {
    // Implementation from OtpManager
  }
}
```

### Usage

```typescript
import { PostgresDeviceManagementService } from './PostgresDeviceManagementService';

const deviceService = new PostgresDeviceManagementService(
  'postgresql://user:password@localhost:5432/privacyidea'
);

app.post('/api/device/request-activation', async (req, res) => {
  const result = await deviceService.requestDeviceActivation(req.body);
  res.json(result);
});
```

## Option 3: TypeORM (Multi-Database)

TypeORM supports multiple databases and provides a clean abstraction layer.

### Installation

```bash
npm install typeorm reflect-metadata
npm install pg # or mysql2, or sqlite3
npm install @types/node --save-dev
```

### Entity Definitions

```typescript
import { Entity, PrimaryGeneratedColumn, Column, Index, CreateDateColumn } from 'typeorm';

@Entity('devices')
@Index(['deviceId'], { unique: true })
@Index(['isActivated'])
export class DeviceEntity {
  @PrimaryGeneratedColumn()
  id: number;

  @Column({ length: 255, unique: true })
  deviceId: string;

  @Column({ length: 255 })
  deviceName: string;

  @Column({ length: 50 })
  platform: string;

  @Column({ length: 100 })
  osVersion: string;

  @Column({ length: 255 })
  model: string;

  @CreateDateColumn()
  registeredAt: Date;

  @Column({ default: false })
  isActivated: boolean;

  @Column({ nullable: true })
  activatedAt?: Date;

  @Column({ length: 255, nullable: true })
  activationToken?: string;
}

@Entity('pending_device_activations')
@Index(['deviceId'], { unique: true })
@Index(['expiresAt'])
export class PendingActivationEntity {
  @PrimaryGeneratedColumn()
  id: number;

  @Column({ length: 255, unique: true })
  deviceId: string;

  @Column({ length: 500 })
  secret: string;

  @CreateDateColumn()
  createdAt: Date;

  @Column()
  expiresAt: Date;
}
```

## Best Practices

1. **Connection Pooling**: Use connection pools for better performance
2. **Transactions**: Use transactions for operations that modify multiple tables
3. **Indexes**: Ensure proper indexes on frequently queried columns
4. **Cleanup**: Schedule periodic cleanup of expired activations
5. **Error Handling**: Implement proper error handling and logging
6. **Security**: Use parameterized queries to prevent SQL injection

## Periodic Cleanup

Set up a scheduled job to clean up expired activations:

```typescript
import { CronJob } from 'cron';

// Run cleanup every 5 minutes
new CronJob('*/5 * * * *', async () => {
  await deviceService.cleanupExpiredActivations();
  console.log('Cleaned up expired activations');
}).start();
```

## Migration from In-Memory

To migrate from the in-memory implementation:

1. Choose your database solution
2. Set up the database and create tables
3. Replace `DeviceManagementService` with your database implementation
4. Test the new implementation
5. Deploy and monitor
6. Existing devices in memory will be lost, so coordinate the migration during a maintenance window

## Testing

Always test database implementations thoroughly:

```typescript
describe('PostgresDeviceManagementService', () => {
  it('should persist device across restarts', async () => {
    const service1 = new PostgresDeviceManagementService(connectionString);
    await service1.requestDeviceActivation({ deviceId: 'test-123', ... });
    
    // Simulate restart
    const service2 = new PostgresDeviceManagementService(connectionString);
    const device = await service2.getDeviceInfo('test-123');
    
    expect(device).toBeDefined();
    expect(device.deviceId).toBe('test-123');
  });
});
```

## Support

For questions or issues with database integration, please open an issue on GitHub.
