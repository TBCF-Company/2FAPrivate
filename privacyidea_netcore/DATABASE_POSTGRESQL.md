# PrivacyIDEA PostgreSQL Database Schema

**Version:** 4.0.0  
**Database:** PostgreSQL 15+  
**Character Set:** UTF-8

---

## 📋 MỤC LỤC

1. [Tổng quan cấu trúc](#1-tổng-quan-cấu-trúc)
2. [Sơ đồ quan hệ](#2-sơ-đồ-quan-hệ)
3. [Chi tiết các bảng](#3-chi-tiết-các-bảng)
4. [SQL khởi tạo](#4-sql-khởi-tạo)
5. [Indexes](#5-indexes)
6. [Migrations](#6-migrations)

---

## 1. TỔNG QUAN CẤU TRÚC

### Danh sách bảng (47 tables)

| Nhóm | Bảng | Mô tả |
|------|------|-------|
| **Token** | token, tokeninfo, tokenowner, tokenrealm | Core token data |
| **Realm** | realm, resolver, resolverconfig, resolverrealm | User sources |
| **Policy** | policy, policycondition | Access control |
| **Admin** | admin | Admin users |
| **Audit** | audit | Audit logging |
| **Config** | config | System configuration |
| **Event** | eventhandler, eventhandlercondition, eventhandleroption | Event handling |
| **SMS** | smsgateway, smsgatewayoption | SMS providers |
| **SMTP** | smtpserver | Email servers |
| **RADIUS** | radiusserver | RADIUS servers |
| **Challenge** | challenge | OTP challenges |
| **Cache** | authcache, usercache | Caching |
| **Machine** | machineresolver, machineresolverconfig, machinetoken, machinetokenoption | Machine tokens |
| **CA** | caconnector, caconnectorconfig | Certificate Authority |
| **Container** | tokencontainer, tokencontainerowner, tokencontainerinfo, tokencontainerrealm, tokencontainerstates | Token containers |
| **Group** | tokengroup, tokentokengroup | Token groups |
| **Periodic** | periodictask, periodictaskoption, periodictasklastrun | Scheduled tasks |
| **Monitor** | monitoringstats, eventcounter | Monitoring |
| **Other** | subscription, privacyideaserver, nodename, serviceid, clientapplication, passwordreset, customuserattribute | Misc |

---

## 2. SƠ ĐỒ QUAN HỆ

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   realm     │────<│resolverrealm│>────│  resolver   │
└─────────────┘     └─────────────┘     └─────────────┘
       │                                       │
       │                                       │
       ▼                                       ▼
┌─────────────┐                        ┌─────────────────┐
│ tokenrealm  │                        │ resolverconfig  │
└─────────────┘                        └─────────────────┘
       │
       │
       ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   token     │────<│ tokenowner  │     │  tokeninfo  │
└─────────────┘     └─────────────┘     └─────────────┘
       │                                       │
       │                                       │
       ▼                                       ▼
┌─────────────┐                        ┌─────────────────┐
│  challenge  │                        │    policy       │
└─────────────┘                        └─────────────────┘
                                              │
                                              ▼
                                       ┌─────────────────┐
                                       │policycondition  │
                                       └─────────────────┘
```

---

## 3. CHI TIẾT CÁC BẢNG

### 3.1 Token Tables

#### `token`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Token ID |
| serial | VARCHAR(40) | UNIQUE NOT NULL | Serial number |
| tokentype | VARCHAR(30) | NOT NULL DEFAULT 'hotp' | Token type |
| description | VARCHAR(80) | | Description |
| key_enc | VARCHAR(2800) | | Encrypted OTP key |
| key_iv | VARCHAR(32) | | IV for key encryption |
| pin_hash | VARCHAR(512) | | Hashed PIN |
| pin_seed | VARCHAR(32) | | PIN seed |
| user_pin | VARCHAR(512) | | User PIN (encrypted) |
| user_pin_iv | VARCHAR(32) | | User PIN IV |
| so_pin | VARCHAR(512) | | SO PIN |
| so_pin_iv | VARCHAR(32) | | SO PIN IV |
| otplen | INTEGER | DEFAULT 6 | OTP length |
| count | INTEGER | DEFAULT 0 | OTP counter |
| count_window | INTEGER | DEFAULT 10 | Counter window |
| sync_window | INTEGER | DEFAULT 1000 | Sync window |
| failcount | INTEGER | DEFAULT 0 | Failed attempts |
| maxfail | INTEGER | DEFAULT 10 | Max failures |
| active | BOOLEAN | DEFAULT true | Is active |
| revoked | BOOLEAN | DEFAULT false | Is revoked |
| locked | BOOLEAN | DEFAULT false | Is locked |
| rollout_state | VARCHAR(10) | | Rollout state |
| created_at | TIMESTAMP | DEFAULT NOW() | Created timestamp |
| updated_at | TIMESTAMP | | Updated timestamp |

#### `tokeninfo`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Info ID |
| token_id | INTEGER | FK → token(id) ON DELETE CASCADE | Token reference |
| key | VARCHAR(255) | NOT NULL | Info key |
| value | TEXT | | Info value |
| type | VARCHAR(100) | | Value type |
| description | VARCHAR(2000) | | Description |

#### `tokenowner`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Owner ID |
| token_id | INTEGER | FK → token(id) ON DELETE CASCADE | Token reference |
| resolver_id | INTEGER | FK → resolver(id) | Resolver reference |
| realm_id | INTEGER | FK → realm(id) | Realm reference |
| user_id | VARCHAR(320) | NOT NULL | User identifier |

#### `tokenrealm`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | ID |
| token_id | INTEGER | FK → token(id) ON DELETE CASCADE | Token reference |
| realm_id | INTEGER | FK → realm(id) | Realm reference |

### 3.2 Realm & Resolver Tables

#### `realm`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Realm ID |
| name | VARCHAR(255) | UNIQUE NOT NULL | Realm name |
| default_realm | BOOLEAN | DEFAULT false | Is default |
| option | VARCHAR(40) | | Options |

#### `resolver`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Resolver ID |
| name | VARCHAR(255) | UNIQUE NOT NULL | Resolver name |
| rtype | VARCHAR(255) | NOT NULL | Resolver type |

#### `resolverconfig`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Config ID |
| resolver_id | INTEGER | FK → resolver(id) ON DELETE CASCADE | Resolver reference |
| key | VARCHAR(255) | NOT NULL | Config key |
| value | TEXT | | Config value |
| type | VARCHAR(100) | DEFAULT 'string' | Value type |
| description | VARCHAR(2000) | | Description |

#### `resolverrealm`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | ID |
| resolver_id | INTEGER | FK → resolver(id) ON DELETE CASCADE | Resolver reference |
| realm_id | INTEGER | FK → realm(id) ON DELETE CASCADE | Realm reference |
| priority | INTEGER | DEFAULT 1 | Priority |

### 3.3 Policy Tables

#### `policy`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Policy ID |
| name | VARCHAR(64) | UNIQUE NOT NULL | Policy name |
| scope | VARCHAR(32) | NOT NULL | Policy scope |
| action | TEXT | | Policy action |
| active | BOOLEAN | DEFAULT true | Is active |
| check_all_resolvers | BOOLEAN | DEFAULT false | Check all resolvers |
| realm | TEXT | | Target realms |
| adminrealm | TEXT | | Admin realms |
| adminuser | TEXT | | Admin users |
| resolver | TEXT | | Target resolvers |
| user | TEXT | | Target users |
| client | TEXT | | Client IPs |
| time | TEXT | | Time conditions |
| pinode | TEXT | | PI nodes |
| priority | INTEGER | DEFAULT 1 | Priority |

#### `policycondition`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Condition ID |
| policy_id | INTEGER | FK → policy(id) ON DELETE CASCADE | Policy reference |
| section | VARCHAR(255) | | Section |
| key | VARCHAR(255) | NOT NULL | Condition key |
| comparator | VARCHAR(255) | | Comparator |
| value | TEXT | | Condition value |
| active | BOOLEAN | DEFAULT true | Is active |

### 3.4 Admin Table

#### `admin`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Admin ID |
| username | VARCHAR(120) | UNIQUE NOT NULL | Username |
| email | VARCHAR(255) | | Email |
| password | VARCHAR(255) | NOT NULL | Password hash |
| active | BOOLEAN | DEFAULT true | Is active |
| created_at | TIMESTAMP | DEFAULT NOW() | Created timestamp |

### 3.5 Audit Table

#### `audit`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Audit ID |
| date | TIMESTAMP | DEFAULT NOW() | Timestamp |
| signature | TEXT | | Signature |
| action | VARCHAR(50) | | Action |
| success | BOOLEAN | | Was successful |
| serial | VARCHAR(40) | | Token serial |
| token_type | VARCHAR(30) | | Token type |
| user | VARCHAR(320) | | User |
| realm | VARCHAR(255) | | Realm |
| administrator | VARCHAR(255) | | Admin user |
| action_detail | TEXT | | Action details |
| info | TEXT | | Additional info |
| privacyidea_server | VARCHAR(255) | | Server node |
| client | VARCHAR(50) | | Client IP |
| loglevel | VARCHAR(10) | | Log level |
| clearance_level | INTEGER | DEFAULT 0 | Clearance level |
| policies | TEXT | | Applied policies |
| resolver | VARCHAR(255) | | Resolver used |
| thread_id | VARCHAR(20) | | Thread ID |
| startdate | TIMESTAMP | | Start time |
| duration | DECIMAL(20,7) | | Duration |

### 3.6 Event Handler Tables

#### `eventhandler`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Handler ID |
| name | VARCHAR(64) | NOT NULL | Handler name |
| ordering | INTEGER | DEFAULT 0 | Order |
| active | BOOLEAN | DEFAULT true | Is active |
| event | TEXT | NOT NULL | Events (comma-separated) |
| handlermodule | VARCHAR(255) | NOT NULL | Handler module |
| position | VARCHAR(10) | DEFAULT 'post' | Position (pre/post) |

#### `eventhandlercondition`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Condition ID |
| eventhandler_id | INTEGER | FK → eventhandler(id) ON DELETE CASCADE | Handler reference |
| key | VARCHAR(255) | NOT NULL | Condition key |
| value | TEXT | | Condition value |
| comparator | VARCHAR(255) | | Comparator |

#### `eventhandleroption`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Option ID |
| eventhandler_id | INTEGER | FK → eventhandler(id) ON DELETE CASCADE | Handler reference |
| key | VARCHAR(255) | NOT NULL | Option key |
| value | TEXT | | Option value |
| type | VARCHAR(100) | | Value type |
| description | VARCHAR(2000) | | Description |

### 3.7 SMS & SMTP Tables

#### `smsgateway`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Gateway ID |
| identifier | VARCHAR(255) | UNIQUE NOT NULL | Gateway identifier |
| description | VARCHAR(1000) | | Description |
| providermodule | VARCHAR(1024) | NOT NULL | Provider module |

#### `smsgatewayoption`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Option ID |
| gateway_id | INTEGER | FK → smsgateway(id) ON DELETE CASCADE | Gateway reference |
| key | VARCHAR(255) | NOT NULL | Option key |
| value | TEXT | | Option value |
| type | VARCHAR(100) | DEFAULT 'string' | Value type |

#### `smtpserver`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Server ID |
| identifier | VARCHAR(255) | UNIQUE NOT NULL | Server identifier |
| server | VARCHAR(255) | NOT NULL | SMTP server |
| port | INTEGER | DEFAULT 25 | SMTP port |
| username | VARCHAR(255) | | Auth username |
| password | VARCHAR(255) | | Auth password |
| sender | VARCHAR(255) | NOT NULL | Sender email |
| tls | BOOLEAN | DEFAULT false | Use TLS |
| timeout | INTEGER | DEFAULT 10 | Timeout seconds |
| description | VARCHAR(1000) | | Description |

#### `radiusserver`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Server ID |
| identifier | VARCHAR(255) | UNIQUE NOT NULL | Server identifier |
| server | VARCHAR(255) | NOT NULL | RADIUS server |
| port | INTEGER | DEFAULT 1812 | RADIUS port |
| secret | VARCHAR(255) | NOT NULL | Shared secret |
| timeout | INTEGER | DEFAULT 5 | Timeout |
| retries | INTEGER | DEFAULT 3 | Retries |
| description | VARCHAR(1000) | | Description |
| dictionary | TEXT | | Dictionary path |

### 3.8 Challenge Table

#### `challenge`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Challenge ID |
| transaction_id | VARCHAR(64) | UNIQUE NOT NULL | Transaction ID |
| serial | VARCHAR(40) | NOT NULL | Token serial |
| data | TEXT | | Challenge data |
| challenge | VARCHAR(512) | | Challenge value |
| session | VARCHAR(512) | | Session data |
| otp_valid | BOOLEAN | DEFAULT false | OTP validated |
| otp_received | BOOLEAN | DEFAULT false | OTP received |
| timestamp | TIMESTAMP | DEFAULT NOW() | Created timestamp |
| expiration | TIMESTAMP | | Expiration time |

### 3.9 Cache Tables

#### `authcache`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Cache ID |
| first_auth | TIMESTAMP | DEFAULT NOW() | First auth time |
| last_auth | TIMESTAMP | | Last auth time |
| username | VARCHAR(64) | NOT NULL | Username |
| realm | VARCHAR(64) | | Realm |
| resolver | VARCHAR(64) | | Resolver |
| client_ip | VARCHAR(40) | | Client IP |
| user_agent | VARCHAR(255) | | User agent |
| auth_count | INTEGER | DEFAULT 1 | Auth count |

#### `usercache`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Cache ID |
| username | VARCHAR(64) | NOT NULL | Username |
| resolver | VARCHAR(64) | NOT NULL | Resolver |
| user_id | VARCHAR(320) | | User ID |
| timestamp | TIMESTAMP | DEFAULT NOW() | Cache time |
| user_data | JSONB | | Cached user data |

### 3.10 Machine Tables

#### `machineresolver`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Resolver ID |
| name | VARCHAR(255) | UNIQUE NOT NULL | Resolver name |
| rtype | VARCHAR(255) | NOT NULL | Resolver type |

#### `machineresolverconfig`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Config ID |
| machineresolver_id | INTEGER | FK → machineresolver(id) ON DELETE CASCADE | Resolver reference |
| key | VARCHAR(255) | NOT NULL | Config key |
| value | TEXT | | Config value |
| type | VARCHAR(100) | | Value type |

#### `machinetoken`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | ID |
| token_id | INTEGER | FK → token(id) ON DELETE CASCADE | Token reference |
| machineresolver_id | INTEGER | FK → machineresolver(id) | Resolver reference |
| machine_id | VARCHAR(255) | NOT NULL | Machine ID |
| application | VARCHAR(64) | NOT NULL | Application |

#### `machinetokenoption`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Option ID |
| machinetoken_id | INTEGER | FK → machinetoken(id) ON DELETE CASCADE | Machine token reference |
| key | VARCHAR(255) | NOT NULL | Option key |
| value | TEXT | | Option value |

### 3.11 CA Connector Tables

#### `caconnector`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Connector ID |
| name | VARCHAR(255) | UNIQUE NOT NULL | Connector name |
| catype | VARCHAR(255) | NOT NULL | CA type |

#### `caconnectorconfig`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Config ID |
| caconnector_id | INTEGER | FK → caconnector(id) ON DELETE CASCADE | Connector reference |
| key | VARCHAR(255) | NOT NULL | Config key |
| value | TEXT | | Config value |
| type | VARCHAR(100) | | Value type |
| description | VARCHAR(2000) | | Description |

### 3.12 Config Table

#### `config`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Config ID |
| key | VARCHAR(255) | UNIQUE NOT NULL | Config key |
| value | TEXT | | Config value |
| type | VARCHAR(100) | DEFAULT 'string' | Value type |
| description | VARCHAR(2000) | | Description |

### 3.13 Periodic Task Tables

#### `periodictask`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Task ID |
| name | VARCHAR(64) | UNIQUE NOT NULL | Task name |
| active | BOOLEAN | DEFAULT true | Is active |
| interval | VARCHAR(255) | NOT NULL | Cron interval |
| nodes | TEXT | | Target nodes |
| taskmodule | VARCHAR(255) | NOT NULL | Task module |
| ordering | INTEGER | DEFAULT 0 | Order |
| retry_if_failed | BOOLEAN | DEFAULT true | Retry on failure |

#### `periodictaskoption`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Option ID |
| periodictask_id | INTEGER | FK → periodictask(id) ON DELETE CASCADE | Task reference |
| key | VARCHAR(255) | NOT NULL | Option key |
| value | TEXT | | Option value |

#### `periodictasklastrun`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | ID |
| periodictask_id | INTEGER | FK → periodictask(id) ON DELETE CASCADE | Task reference |
| node | VARCHAR(255) | NOT NULL | Node name |
| timestamp | TIMESTAMP | DEFAULT NOW() | Last run time |
| success | BOOLEAN | | Was successful |
| duration | DECIMAL(10,3) | | Duration seconds |
| result | TEXT | | Result message |

### 3.14 Monitoring Tables

#### `eventcounter`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Counter ID |
| counter_name | VARCHAR(80) | UNIQUE NOT NULL | Counter name |
| counter_value | INTEGER | DEFAULT 0 | Counter value |
| reset_date | TIMESTAMP | | Last reset |
| node | VARCHAR(255) | | Node name |

#### `monitoringstats`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Stats ID |
| timestamp | TIMESTAMP | DEFAULT NOW() | Stat timestamp |
| stats_key | VARCHAR(255) | NOT NULL | Stat key |
| stats_value | INTEGER | | Stat value |
| node | VARCHAR(255) | | Node name |

### 3.15 Token Container Tables

#### `tokencontainer`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Container ID |
| serial | VARCHAR(40) | UNIQUE NOT NULL | Serial number |
| type | VARCHAR(30) | NOT NULL | Container type |
| description | VARCHAR(80) | | Description |
| created_at | TIMESTAMP | DEFAULT NOW() | Created timestamp |

#### `tokencontainerowner`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Owner ID |
| container_id | INTEGER | FK → tokencontainer(id) ON DELETE CASCADE | Container reference |
| resolver_id | INTEGER | FK → resolver(id) | Resolver reference |
| realm_id | INTEGER | FK → realm(id) | Realm reference |
| user_id | VARCHAR(320) | NOT NULL | User ID |

#### `tokencontainerinfo`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Info ID |
| container_id | INTEGER | FK → tokencontainer(id) ON DELETE CASCADE | Container reference |
| key | VARCHAR(255) | NOT NULL | Info key |
| value | TEXT | | Info value |

#### `tokencontainerrealm`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | ID |
| container_id | INTEGER | FK → tokencontainer(id) ON DELETE CASCADE | Container reference |
| realm_id | INTEGER | FK → realm(id) | Realm reference |

#### `tokencontainerstates`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | State ID |
| container_id | INTEGER | FK → tokencontainer(id) ON DELETE CASCADE | Container reference |
| state | VARCHAR(30) | NOT NULL | State name |
| timestamp | TIMESTAMP | DEFAULT NOW() | State timestamp |

#### `tokencontainertoken`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | ID |
| container_id | INTEGER | FK → tokencontainer(id) ON DELETE CASCADE | Container reference |
| token_id | INTEGER | FK → token(id) ON DELETE CASCADE | Token reference |

### 3.16 Token Group Tables

#### `tokengroup`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Group ID |
| name | VARCHAR(255) | UNIQUE NOT NULL | Group name |
| description | VARCHAR(80) | | Description |

#### `tokentokengroup`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | ID |
| token_id | INTEGER | FK → token(id) ON DELETE CASCADE | Token reference |
| tokengroup_id | INTEGER | FK → tokengroup(id) ON DELETE CASCADE | Group reference |

### 3.17 Other Tables

#### `subscription`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Subscription ID |
| application | VARCHAR(50) | | Application |
| for_name | VARCHAR(100) | | For name |
| for_address | TEXT | | For address |
| for_email | VARCHAR(255) | | For email |
| for_phone | VARCHAR(50) | | For phone |
| for_url | VARCHAR(255) | | For URL |
| for_comment | TEXT | | Comment |
| by_name | VARCHAR(100) | | By name |
| by_email | VARCHAR(255) | | By email |
| by_address | TEXT | | By address |
| by_phone | VARCHAR(50) | | By phone |
| by_url | VARCHAR(255) | | By URL |
| date_from | DATE | | Valid from |
| date_till | DATE | | Valid until |
| num_users | INTEGER | | Max users |
| num_tokens | INTEGER | | Max tokens |
| num_clients | INTEGER | | Max clients |
| level | VARCHAR(30) | | Level |
| signature | TEXT | | Signature |

#### `privacyideaserver`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Server ID |
| identifier | VARCHAR(255) | UNIQUE NOT NULL | Identifier |
| url | VARCHAR(255) | NOT NULL | Server URL |
| description | VARCHAR(2000) | | Description |
| tls | BOOLEAN | DEFAULT true | Use TLS |

#### `nodename`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Node ID |
| name | VARCHAR(255) | UNIQUE NOT NULL | Node name |
| last_seen | TIMESTAMP | | Last seen time |

#### `serviceid`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Service ID |
| name | VARCHAR(64) | UNIQUE NOT NULL | Service name |
| description | VARCHAR(2000) | | Description |

#### `clientapplication`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Client ID |
| ip | VARCHAR(40) | NOT NULL | Client IP |
| hostname | VARCHAR(255) | | Hostname |
| clienttype | VARCHAR(30) | | Client type |
| lastseen | TIMESTAMP | | Last seen |
| node | VARCHAR(255) | | Node |

#### `passwordreset`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Reset ID |
| recoverycode | VARCHAR(255) | NOT NULL | Recovery code |
| username | VARCHAR(64) | NOT NULL | Username |
| realm | VARCHAR(64) | | Realm |
| resolver | VARCHAR(64) | | Resolver |
| email | VARCHAR(255) | | Email |
| timestamp | TIMESTAMP | DEFAULT NOW() | Created time |
| expiration | TIMESTAMP | | Expiration time |

#### `customuserattribute`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Attribute ID |
| user_id | VARCHAR(320) | NOT NULL | User ID |
| resolver | VARCHAR(64) | NOT NULL | Resolver |
| realm | VARCHAR(64) | | Realm |
| key | VARCHAR(255) | NOT NULL | Attribute key |
| value | TEXT | | Attribute value |
| type | VARCHAR(100) | | Value type |

---

## 4. SQL KHỞI TẠO

```sql
-- =============================================
-- PrivacyIDEA PostgreSQL Database Initialization
-- Version: 4.0.0
-- =============================================

-- Create database (run as superuser)
-- CREATE DATABASE privacyidea WITH ENCODING 'UTF8' LC_COLLATE 'en_US.UTF-8' LC_CTYPE 'en_US.UTF-8';

-- Connect to database
-- \c privacyidea

-- =============================================
-- 1. TOKEN TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS token (
    id SERIAL PRIMARY KEY,
    serial VARCHAR(40) UNIQUE NOT NULL,
    tokentype VARCHAR(30) NOT NULL DEFAULT 'hotp',
    description VARCHAR(80) DEFAULT '',
    key_enc VARCHAR(2800),
    key_iv VARCHAR(32),
    pin_hash VARCHAR(512),
    pin_seed VARCHAR(32),
    user_pin VARCHAR(512),
    user_pin_iv VARCHAR(32),
    so_pin VARCHAR(512),
    so_pin_iv VARCHAR(32),
    otplen INTEGER DEFAULT 6,
    count INTEGER DEFAULT 0,
    count_window INTEGER DEFAULT 10,
    sync_window INTEGER DEFAULT 1000,
    failcount INTEGER DEFAULT 0,
    maxfail INTEGER DEFAULT 10,
    active BOOLEAN DEFAULT true,
    revoked BOOLEAN DEFAULT false,
    locked BOOLEAN DEFAULT false,
    rollout_state VARCHAR(10),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS realm (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) UNIQUE NOT NULL,
    default_realm BOOLEAN DEFAULT false,
    option VARCHAR(40)
);

CREATE TABLE IF NOT EXISTS resolver (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) UNIQUE NOT NULL,
    rtype VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS resolverconfig (
    id SERIAL PRIMARY KEY,
    resolver_id INTEGER REFERENCES resolver(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT,
    type VARCHAR(100) DEFAULT 'string',
    description VARCHAR(2000)
);

CREATE TABLE IF NOT EXISTS resolverrealm (
    id SERIAL PRIMARY KEY,
    resolver_id INTEGER REFERENCES resolver(id) ON DELETE CASCADE,
    realm_id INTEGER REFERENCES realm(id) ON DELETE CASCADE,
    priority INTEGER DEFAULT 1,
    UNIQUE(resolver_id, realm_id)
);

CREATE TABLE IF NOT EXISTS tokeninfo (
    id SERIAL PRIMARY KEY,
    token_id INTEGER REFERENCES token(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT,
    type VARCHAR(100),
    description VARCHAR(2000)
);

CREATE TABLE IF NOT EXISTS tokenowner (
    id SERIAL PRIMARY KEY,
    token_id INTEGER REFERENCES token(id) ON DELETE CASCADE,
    resolver_id INTEGER REFERENCES resolver(id),
    realm_id INTEGER REFERENCES realm(id),
    user_id VARCHAR(320) NOT NULL
);

CREATE TABLE IF NOT EXISTS tokenrealm (
    id SERIAL PRIMARY KEY,
    token_id INTEGER REFERENCES token(id) ON DELETE CASCADE,
    realm_id INTEGER REFERENCES realm(id),
    UNIQUE(token_id, realm_id)
);

-- =============================================
-- 2. POLICY TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS policy (
    id SERIAL PRIMARY KEY,
    name VARCHAR(64) UNIQUE NOT NULL,
    scope VARCHAR(32) NOT NULL,
    action TEXT,
    active BOOLEAN DEFAULT true,
    check_all_resolvers BOOLEAN DEFAULT false,
    realm TEXT,
    adminrealm TEXT,
    adminuser TEXT,
    resolver TEXT,
    "user" TEXT,
    client TEXT,
    "time" TEXT,
    pinode TEXT,
    priority INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS policycondition (
    id SERIAL PRIMARY KEY,
    policy_id INTEGER REFERENCES policy(id) ON DELETE CASCADE,
    section VARCHAR(255),
    key VARCHAR(255) NOT NULL,
    comparator VARCHAR(255),
    value TEXT,
    active BOOLEAN DEFAULT true
);

-- =============================================
-- 3. ADMIN TABLE
-- =============================================

CREATE TABLE IF NOT EXISTS admin (
    id SERIAL PRIMARY KEY,
    username VARCHAR(120) UNIQUE NOT NULL,
    email VARCHAR(255),
    password VARCHAR(255) NOT NULL,
    active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW()
);

-- =============================================
-- 4. AUDIT TABLE
-- =============================================

CREATE TABLE IF NOT EXISTS audit (
    id SERIAL PRIMARY KEY,
    date TIMESTAMP DEFAULT NOW(),
    signature TEXT,
    action VARCHAR(50),
    success BOOLEAN,
    serial VARCHAR(40),
    token_type VARCHAR(30),
    "user" VARCHAR(320),
    realm VARCHAR(255),
    administrator VARCHAR(255),
    action_detail TEXT,
    info TEXT,
    privacyidea_server VARCHAR(255),
    client VARCHAR(50),
    loglevel VARCHAR(10),
    clearance_level INTEGER DEFAULT 0,
    policies TEXT,
    resolver VARCHAR(255),
    thread_id VARCHAR(20),
    startdate TIMESTAMP,
    duration DECIMAL(20,7)
);

-- =============================================
-- 5. CONFIG TABLE
-- =============================================

CREATE TABLE IF NOT EXISTS config (
    id SERIAL PRIMARY KEY,
    key VARCHAR(255) UNIQUE NOT NULL,
    value TEXT,
    type VARCHAR(100) DEFAULT 'string',
    description VARCHAR(2000)
);

-- =============================================
-- 6. EVENT HANDLER TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS eventhandler (
    id SERIAL PRIMARY KEY,
    name VARCHAR(64) NOT NULL,
    ordering INTEGER DEFAULT 0,
    active BOOLEAN DEFAULT true,
    event TEXT NOT NULL,
    handlermodule VARCHAR(255) NOT NULL,
    position VARCHAR(10) DEFAULT 'post'
);

CREATE TABLE IF NOT EXISTS eventhandlercondition (
    id SERIAL PRIMARY KEY,
    eventhandler_id INTEGER REFERENCES eventhandler(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT,
    comparator VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS eventhandleroption (
    id SERIAL PRIMARY KEY,
    eventhandler_id INTEGER REFERENCES eventhandler(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT,
    type VARCHAR(100),
    description VARCHAR(2000)
);

-- =============================================
-- 7. SMS GATEWAY TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS smsgateway (
    id SERIAL PRIMARY KEY,
    identifier VARCHAR(255) UNIQUE NOT NULL,
    description VARCHAR(1000),
    providermodule VARCHAR(1024) NOT NULL
);

CREATE TABLE IF NOT EXISTS smsgatewayoption (
    id SERIAL PRIMARY KEY,
    gateway_id INTEGER REFERENCES smsgateway(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT,
    type VARCHAR(100) DEFAULT 'string'
);

-- =============================================
-- 8. SMTP SERVER TABLE
-- =============================================

CREATE TABLE IF NOT EXISTS smtpserver (
    id SERIAL PRIMARY KEY,
    identifier VARCHAR(255) UNIQUE NOT NULL,
    server VARCHAR(255) NOT NULL,
    port INTEGER DEFAULT 25,
    username VARCHAR(255),
    password VARCHAR(255),
    sender VARCHAR(255) NOT NULL,
    tls BOOLEAN DEFAULT false,
    timeout INTEGER DEFAULT 10,
    description VARCHAR(1000)
);

-- =============================================
-- 9. RADIUS SERVER TABLE
-- =============================================

CREATE TABLE IF NOT EXISTS radiusserver (
    id SERIAL PRIMARY KEY,
    identifier VARCHAR(255) UNIQUE NOT NULL,
    server VARCHAR(255) NOT NULL,
    port INTEGER DEFAULT 1812,
    secret VARCHAR(255) NOT NULL,
    timeout INTEGER DEFAULT 5,
    retries INTEGER DEFAULT 3,
    description VARCHAR(1000),
    dictionary TEXT
);

-- =============================================
-- 10. CHALLENGE TABLE
-- =============================================

CREATE TABLE IF NOT EXISTS challenge (
    id SERIAL PRIMARY KEY,
    transaction_id VARCHAR(64) UNIQUE NOT NULL,
    serial VARCHAR(40) NOT NULL,
    data TEXT,
    challenge VARCHAR(512),
    session VARCHAR(512),
    otp_valid BOOLEAN DEFAULT false,
    otp_received BOOLEAN DEFAULT false,
    timestamp TIMESTAMP DEFAULT NOW(),
    expiration TIMESTAMP
);

-- =============================================
-- 11. CACHE TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS authcache (
    id SERIAL PRIMARY KEY,
    first_auth TIMESTAMP DEFAULT NOW(),
    last_auth TIMESTAMP,
    username VARCHAR(64) NOT NULL,
    realm VARCHAR(64),
    resolver VARCHAR(64),
    client_ip VARCHAR(40),
    user_agent VARCHAR(255),
    auth_count INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS usercache (
    id SERIAL PRIMARY KEY,
    username VARCHAR(64) NOT NULL,
    resolver VARCHAR(64) NOT NULL,
    user_id VARCHAR(320),
    timestamp TIMESTAMP DEFAULT NOW(),
    user_data JSONB
);

-- =============================================
-- 12. MACHINE TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS machineresolver (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) UNIQUE NOT NULL,
    rtype VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS machineresolverconfig (
    id SERIAL PRIMARY KEY,
    machineresolver_id INTEGER REFERENCES machineresolver(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT,
    type VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS machinetoken (
    id SERIAL PRIMARY KEY,
    token_id INTEGER REFERENCES token(id) ON DELETE CASCADE,
    machineresolver_id INTEGER REFERENCES machineresolver(id),
    machine_id VARCHAR(255) NOT NULL,
    application VARCHAR(64) NOT NULL
);

CREATE TABLE IF NOT EXISTS machinetokenoption (
    id SERIAL PRIMARY KEY,
    machinetoken_id INTEGER REFERENCES machinetoken(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT
);

-- =============================================
-- 13. CA CONNECTOR TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS caconnector (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) UNIQUE NOT NULL,
    catype VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS caconnectorconfig (
    id SERIAL PRIMARY KEY,
    caconnector_id INTEGER REFERENCES caconnector(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT,
    type VARCHAR(100),
    description VARCHAR(2000)
);

-- =============================================
-- 14. PERIODIC TASK TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS periodictask (
    id SERIAL PRIMARY KEY,
    name VARCHAR(64) UNIQUE NOT NULL,
    active BOOLEAN DEFAULT true,
    interval VARCHAR(255) NOT NULL,
    nodes TEXT,
    taskmodule VARCHAR(255) NOT NULL,
    ordering INTEGER DEFAULT 0,
    retry_if_failed BOOLEAN DEFAULT true
);

CREATE TABLE IF NOT EXISTS periodictaskoption (
    id SERIAL PRIMARY KEY,
    periodictask_id INTEGER REFERENCES periodictask(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT
);

CREATE TABLE IF NOT EXISTS periodictasklastrun (
    id SERIAL PRIMARY KEY,
    periodictask_id INTEGER REFERENCES periodictask(id) ON DELETE CASCADE,
    node VARCHAR(255) NOT NULL,
    timestamp TIMESTAMP DEFAULT NOW(),
    success BOOLEAN,
    duration DECIMAL(10,3),
    result TEXT
);

-- =============================================
-- 15. MONITORING TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS eventcounter (
    id SERIAL PRIMARY KEY,
    counter_name VARCHAR(80) UNIQUE NOT NULL,
    counter_value INTEGER DEFAULT 0,
    reset_date TIMESTAMP,
    node VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS monitoringstats (
    id SERIAL PRIMARY KEY,
    timestamp TIMESTAMP DEFAULT NOW(),
    stats_key VARCHAR(255) NOT NULL,
    stats_value INTEGER,
    node VARCHAR(255)
);

-- =============================================
-- 16. TOKEN CONTAINER TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS tokencontainer (
    id SERIAL PRIMARY KEY,
    serial VARCHAR(40) UNIQUE NOT NULL,
    type VARCHAR(30) NOT NULL,
    description VARCHAR(80),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS tokencontainerowner (
    id SERIAL PRIMARY KEY,
    container_id INTEGER REFERENCES tokencontainer(id) ON DELETE CASCADE,
    resolver_id INTEGER REFERENCES resolver(id),
    realm_id INTEGER REFERENCES realm(id),
    user_id VARCHAR(320) NOT NULL
);

CREATE TABLE IF NOT EXISTS tokencontainerinfo (
    id SERIAL PRIMARY KEY,
    container_id INTEGER REFERENCES tokencontainer(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT
);

CREATE TABLE IF NOT EXISTS tokencontainerrealm (
    id SERIAL PRIMARY KEY,
    container_id INTEGER REFERENCES tokencontainer(id) ON DELETE CASCADE,
    realm_id INTEGER REFERENCES realm(id)
);

CREATE TABLE IF NOT EXISTS tokencontainerstates (
    id SERIAL PRIMARY KEY,
    container_id INTEGER REFERENCES tokencontainer(id) ON DELETE CASCADE,
    state VARCHAR(30) NOT NULL,
    timestamp TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS tokencontainertoken (
    id SERIAL PRIMARY KEY,
    container_id INTEGER REFERENCES tokencontainer(id) ON DELETE CASCADE,
    token_id INTEGER REFERENCES token(id) ON DELETE CASCADE
);

-- =============================================
-- 17. TOKEN GROUP TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS tokengroup (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) UNIQUE NOT NULL,
    description VARCHAR(80)
);

CREATE TABLE IF NOT EXISTS tokentokengroup (
    id SERIAL PRIMARY KEY,
    token_id INTEGER REFERENCES token(id) ON DELETE CASCADE,
    tokengroup_id INTEGER REFERENCES tokengroup(id) ON DELETE CASCADE,
    UNIQUE(token_id, tokengroup_id)
);

-- =============================================
-- 18. OTHER TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS subscription (
    id SERIAL PRIMARY KEY,
    application VARCHAR(50),
    for_name VARCHAR(100),
    for_address TEXT,
    for_email VARCHAR(255),
    for_phone VARCHAR(50),
    for_url VARCHAR(255),
    for_comment TEXT,
    by_name VARCHAR(100),
    by_email VARCHAR(255),
    by_address TEXT,
    by_phone VARCHAR(50),
    by_url VARCHAR(255),
    date_from DATE,
    date_till DATE,
    num_users INTEGER,
    num_tokens INTEGER,
    num_clients INTEGER,
    level VARCHAR(30),
    signature TEXT
);

CREATE TABLE IF NOT EXISTS privacyideaserver (
    id SERIAL PRIMARY KEY,
    identifier VARCHAR(255) UNIQUE NOT NULL,
    url VARCHAR(255) NOT NULL,
    description VARCHAR(2000),
    tls BOOLEAN DEFAULT true
);

CREATE TABLE IF NOT EXISTS nodename (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) UNIQUE NOT NULL,
    last_seen TIMESTAMP
);

CREATE TABLE IF NOT EXISTS serviceid (
    id SERIAL PRIMARY KEY,
    name VARCHAR(64) UNIQUE NOT NULL,
    description VARCHAR(2000)
);

CREATE TABLE IF NOT EXISTS clientapplication (
    id SERIAL PRIMARY KEY,
    ip VARCHAR(40) NOT NULL,
    hostname VARCHAR(255),
    clienttype VARCHAR(30),
    lastseen TIMESTAMP,
    node VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS passwordreset (
    id SERIAL PRIMARY KEY,
    recoverycode VARCHAR(255) NOT NULL,
    username VARCHAR(64) NOT NULL,
    realm VARCHAR(64),
    resolver VARCHAR(64),
    email VARCHAR(255),
    timestamp TIMESTAMP DEFAULT NOW(),
    expiration TIMESTAMP
);

CREATE TABLE IF NOT EXISTS customuserattribute (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(320) NOT NULL,
    resolver VARCHAR(64) NOT NULL,
    realm VARCHAR(64),
    key VARCHAR(255) NOT NULL,
    value TEXT,
    type VARCHAR(100)
);

-- =============================================
-- COMMENTS
-- =============================================

COMMENT ON TABLE token IS 'Token data storage';
COMMENT ON TABLE realm IS 'Authentication realms';
COMMENT ON TABLE resolver IS 'User resolvers (LDAP, SQL, etc.)';
COMMENT ON TABLE policy IS 'Access control policies';
COMMENT ON TABLE admin IS 'Administrator accounts';
COMMENT ON TABLE audit IS 'Audit log entries';
COMMENT ON TABLE eventhandler IS 'Event handler configurations';
COMMENT ON TABLE smsgateway IS 'SMS gateway configurations';
COMMENT ON TABLE challenge IS 'OTP challenge storage';
```

---

## 5. INDEXES

```sql
-- =============================================
-- PERFORMANCE INDEXES
-- =============================================

-- Token indexes
CREATE INDEX idx_token_serial ON token(serial);
CREATE INDEX idx_token_tokentype ON token(tokentype);
CREATE INDEX idx_token_active ON token(active);
CREATE INDEX idx_token_created ON token(created_at);

-- TokenInfo indexes
CREATE INDEX idx_tokeninfo_token_key ON tokeninfo(token_id, key);

-- TokenOwner indexes
CREATE INDEX idx_tokenowner_user ON tokenowner(user_id);
CREATE INDEX idx_tokenowner_token ON tokenowner(token_id);

-- Policy indexes
CREATE INDEX idx_policy_scope ON policy(scope);
CREATE INDEX idx_policy_active ON policy(active);

-- Audit indexes
CREATE INDEX idx_audit_date ON audit(date);
CREATE INDEX idx_audit_user ON audit("user");
CREATE INDEX idx_audit_serial ON audit(serial);
CREATE INDEX idx_audit_action ON audit(action);
CREATE INDEX idx_audit_success ON audit(success);

-- Challenge indexes
CREATE INDEX idx_challenge_serial ON challenge(serial);
CREATE INDEX idx_challenge_expiration ON challenge(expiration);

-- AuthCache indexes
CREATE INDEX idx_authcache_username ON authcache(username, realm);
CREATE INDEX idx_authcache_last_auth ON authcache(last_auth);

-- UserCache indexes
CREATE INDEX idx_usercache_username ON usercache(username, resolver);

-- Monitoring indexes
CREATE INDEX idx_monitoringstats_key ON monitoringstats(stats_key);
CREATE INDEX idx_monitoringstats_timestamp ON monitoringstats(timestamp);

-- =============================================
-- PARTIAL INDEXES (for common queries)
-- =============================================

CREATE INDEX idx_token_active_true ON token(serial) WHERE active = true;
CREATE INDEX idx_policy_active_true ON policy(name) WHERE active = true;
CREATE INDEX idx_challenge_valid ON challenge(transaction_id) WHERE otp_valid = false;
```

---

## 6. MIGRATIONS

### EF Core Migration với PostgreSQL

#### Cập nhật Infrastructure.csproj
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
```

#### Cập nhật DbContext
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseNpgsql(connectionString, options =>
    {
        options.EnableRetryOnFailure(3);
        options.CommandTimeout(30);
    });
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // PostgreSQL specific: Use snake_case naming
    foreach (var entity in modelBuilder.Model.GetEntityTypes())
    {
        entity.SetTableName(entity.GetTableName()?.ToLower());
        
        foreach (var property in entity.GetProperties())
        {
            property.SetColumnName(property.GetColumnName()?.ToLower());
        }
    }
    
    // PostgreSQL JSONB support for user_data
    modelBuilder.Entity<UserCache>()
        .Property(e => e.UserData)
        .HasColumnType("jsonb");
}
```

#### Connection String
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=privacyidea;Username=privacyidea;Password=your_password;SSL Mode=Prefer"
  }
}
```

#### Chạy Migration
```bash
dotnet ef migrations add InitialPostgreSQL -p src/PrivacyIDEA.Infrastructure -s src/PrivacyIDEA.Api
dotnet ef database update -p src/PrivacyIDEA.Infrastructure -s src/PrivacyIDEA.Api
```

---

## 📝 GHI CHÚ

### PostgreSQL vs SQLite/SQL Server

| Feature | PostgreSQL | Lợi ích |
|---------|------------|---------|
| JSONB | Native support | Lưu user_data hiệu quả |
| Full-text search | Built-in | Tìm kiếm audit logs |
| Partitioning | Native | Scale audit tables |
| Connection pooling | PgBouncer | Production ready |
| Extensions | PostGIS, pg_cron | Mở rộng tương lai |

### Best Practices

1. **Connection Pooling**: Sử dụng PgBouncer trong production
2. **Backup**: `pg_dump` hàng ngày
3. **Monitoring**: pg_stat_statements cho performance
4. **Indexes**: Review với EXPLAIN ANALYZE

---

*Schema được tạo dựa trên PrivacyIDEA Python models*
