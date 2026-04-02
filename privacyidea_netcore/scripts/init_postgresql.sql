-- =============================================
-- PrivacyIDEA PostgreSQL Database Initialization
-- Version: 4.0.0
-- Database: PostgreSQL 15+
-- =============================================

-- Run as postgres superuser:
-- CREATE DATABASE privacyidea WITH ENCODING 'UTF8' LC_COLLATE 'en_US.UTF-8' LC_CTYPE 'en_US.UTF-8';
-- CREATE USER privacyidea WITH PASSWORD 'your_secure_password';
-- GRANT ALL PRIVILEGES ON DATABASE privacyidea TO privacyidea;

-- Connect to privacyidea database before running this script
-- \c privacyidea

-- =============================================
-- EXTENSIONS
-- =============================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

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
-- INDEXES
-- =============================================

CREATE INDEX IF NOT EXISTS idx_token_serial ON token(serial);
CREATE INDEX IF NOT EXISTS idx_token_tokentype ON token(tokentype);
CREATE INDEX IF NOT EXISTS idx_token_active ON token(active);
CREATE INDEX IF NOT EXISTS idx_token_created ON token(created_at);
CREATE INDEX IF NOT EXISTS idx_tokeninfo_token_key ON tokeninfo(token_id, key);
CREATE INDEX IF NOT EXISTS idx_tokenowner_user ON tokenowner(user_id);
CREATE INDEX IF NOT EXISTS idx_tokenowner_token ON tokenowner(token_id);
CREATE INDEX IF NOT EXISTS idx_policy_scope ON policy(scope);
CREATE INDEX IF NOT EXISTS idx_policy_active ON policy(active);
CREATE INDEX IF NOT EXISTS idx_audit_date ON audit(date);
CREATE INDEX IF NOT EXISTS idx_audit_user ON audit("user");
CREATE INDEX IF NOT EXISTS idx_audit_serial ON audit(serial);
CREATE INDEX IF NOT EXISTS idx_audit_action ON audit(action);
CREATE INDEX IF NOT EXISTS idx_challenge_serial ON challenge(serial);
CREATE INDEX IF NOT EXISTS idx_challenge_expiration ON challenge(expiration);
CREATE INDEX IF NOT EXISTS idx_authcache_username ON authcache(username, realm);
CREATE INDEX IF NOT EXISTS idx_usercache_username ON usercache(username, resolver);
CREATE INDEX IF NOT EXISTS idx_monitoringstats_key ON monitoringstats(stats_key);

-- =============================================
-- DEFAULT DATA
-- =============================================

INSERT INTO realm (name, default_realm) 
VALUES ('default', true)
ON CONFLICT (name) DO NOTHING;

-- Default admin (password: admin) - CHANGE IN PRODUCTION!
INSERT INTO admin (username, email, password, active)
VALUES ('admin', 'admin@localhost', '$pbkdf2-sha256$29000$admin$hash', true)
ON CONFLICT (username) DO NOTHING;

INSERT INTO config (key, value, type, description) VALUES
('PrependPin', 'True', 'bool', 'Prepend PIN to OTP'),
('FailCounterIncOnFalsePin', 'True', 'bool', 'Increase fail counter on wrong PIN'),
('AutoResync', 'False', 'bool', 'Auto resync tokens'),
('DefaultResetFailCount', 'True', 'bool', 'Reset fail counter on successful auth')
ON CONFLICT (key) DO NOTHING;

-- =============================================
-- VERIFY
-- =============================================
-- SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name;
