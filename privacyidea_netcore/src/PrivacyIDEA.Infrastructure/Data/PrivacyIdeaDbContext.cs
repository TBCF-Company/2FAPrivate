using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Infrastructure.Data;

/// <summary>
/// Main database context for PrivacyIDEA
/// Supports PostgreSQL, MySQL, SQLite
/// </summary>
public class PrivacyIdeaDbContext : DbContext
{
    public PrivacyIdeaDbContext(DbContextOptions<PrivacyIdeaDbContext> options)
        : base(options)
    {
    }

    // Token related
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<TokenInfo> TokenInfos => Set<TokenInfo>();
    public DbSet<TokenOwner> TokenOwners => Set<TokenOwner>();
    public DbSet<TokenRealm> TokenRealms => Set<TokenRealm>();

    // Token Groups
    public DbSet<TokenGroup> TokenGroups => Set<TokenGroup>();
    public DbSet<TokenTokenGroup> TokenTokenGroups => Set<TokenTokenGroup>();

    // Token Container
    public DbSet<TokenContainer> TokenContainers => Set<TokenContainer>();
    public DbSet<TokenContainerOwner> TokenContainerOwners => Set<TokenContainerOwner>();
    public DbSet<TokenContainerInfo> TokenContainerInfos => Set<TokenContainerInfo>();
    public DbSet<TokenContainerRealm> TokenContainerRealms => Set<TokenContainerRealm>();
    public DbSet<TokenContainerState> TokenContainerStates => Set<TokenContainerState>();
    public DbSet<TokenContainerToken> TokenContainerTokens => Set<TokenContainerToken>();

    // Realm and Resolver
    public DbSet<Realm> Realms => Set<Realm>();
    public DbSet<Resolver> Resolvers => Set<Resolver>();
    public DbSet<ResolverConfig> ResolverConfigs => Set<ResolverConfig>();
    public DbSet<ResolverRealm> ResolverRealms => Set<ResolverRealm>();

    // Policy
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<PolicyCondition> PolicyConditions => Set<PolicyCondition>();

    // Configuration
    public DbSet<Config> Configs => Set<Config>();
    public DbSet<Admin> Admins => Set<Admin>();

    // Audit
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    // Challenge
    public DbSet<Challenge> Challenges => Set<Challenge>();

    // Event Handler
    public DbSet<Domain.Entities.EventHandler> EventHandlers => Set<Domain.Entities.EventHandler>();
    public DbSet<EventHandlerOption> EventHandlerOptions => Set<EventHandlerOption>();
    public DbSet<EventHandlerCondition> EventHandlerConditions => Set<EventHandlerCondition>();
    public DbSet<EventCounter> EventCounters => Set<EventCounter>();

    // Server configurations
    public DbSet<SmsGateway> SmsGateways => Set<SmsGateway>();
    public DbSet<SmsGatewayOption> SmsGatewayOptions => Set<SmsGatewayOption>();
    public DbSet<SmtpServer> SmtpServers => Set<SmtpServer>();
    public DbSet<RadiusServer> RadiusServers => Set<RadiusServer>();

    // Machine
    public DbSet<MachineResolver> MachineResolvers => Set<MachineResolver>();
    public DbSet<MachineResolverConfig> MachineResolverConfigs => Set<MachineResolverConfig>();
    public DbSet<MachineToken> MachineTokens => Set<MachineToken>();
    public DbSet<MachineTokenOption> MachineTokenOptions => Set<MachineTokenOption>();

    // CA Connector
    public DbSet<CAConnector> CAConnectors => Set<CAConnector>();
    public DbSet<CAConnectorConfig> CAConnectorConfigs => Set<CAConnectorConfig>();

    // Periodic Tasks
    public DbSet<PeriodicTask> PeriodicTasks => Set<PeriodicTask>();
    public DbSet<PeriodicTaskOption> PeriodicTaskOptions => Set<PeriodicTaskOption>();
    public DbSet<PeriodicTaskLastRun> PeriodicTaskLastRuns => Set<PeriodicTaskLastRun>();

    // Cache
    public DbSet<AuthCache> AuthCaches => Set<AuthCache>();
    public DbSet<UserCache> UserCaches => Set<UserCache>();

    // Monitoring
    public DbSet<MonitoringStats> MonitoringStats => Set<MonitoringStats>();

    // Other
    public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();
    public DbSet<CustomUserAttribute> CustomUserAttributes => Set<CustomUserAttribute>();
    public DbSet<ServiceId> ServiceIds => Set<ServiceId>();
    public DbSet<ClientApplication> ClientApplications => Set<ClientApplication>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PrivacyIDEAServer> PrivacyIDEAServers => Set<PrivacyIDEAServer>();
    public DbSet<NodeName> NodeNames => Set<NodeName>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Token
        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasIndex(e => e.Serial).IsUnique();
            entity.HasIndex(e => e.TokenType);
        });

        // TokenInfo
        modelBuilder.Entity<TokenInfo>(entity =>
        {
            entity.HasIndex(e => new { e.TokenId, e.Key }).IsUnique();
        });

        // Realm
        modelBuilder.Entity<Realm>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Resolver
        modelBuilder.Entity<Resolver>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Policy
        modelBuilder.Entity<Policy>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Config
        modelBuilder.Entity<Config>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // Admin
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // Challenge
        modelBuilder.Entity<Challenge>(entity =>
        {
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.Serial);
        });

        // AuditEntry
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Serial);
            entity.HasIndex(e => e.User);
        });

        // SmsGateway
        modelBuilder.Entity<SmsGateway>(entity =>
        {
            entity.HasIndex(e => e.Identifier).IsUnique();
        });

        // SmtpServer
        modelBuilder.Entity<SmtpServer>(entity =>
        {
            entity.HasIndex(e => e.Identifier).IsUnique();
        });

        // RadiusServer
        modelBuilder.Entity<RadiusServer>(entity =>
        {
            entity.HasIndex(e => e.Identifier).IsUnique();
        });

        // EventHandler
        modelBuilder.Entity<Domain.Entities.EventHandler>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // EventCounter
        modelBuilder.Entity<EventCounter>(entity =>
        {
            entity.HasIndex(e => e.CounterName).IsUnique();
        });

        // Token Group
        modelBuilder.Entity<TokenGroup>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<TokenTokenGroup>(entity =>
        {
            entity.HasIndex(e => new { e.TokenId, e.TokenGroupId }).IsUnique();
        });

        // Token Container
        modelBuilder.Entity<TokenContainer>(entity =>
        {
            entity.HasIndex(e => e.Serial).IsUnique();
        });

        // Machine Resolver
        modelBuilder.Entity<MachineResolver>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // CA Connector
        modelBuilder.Entity<CAConnector>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Periodic Task
        modelBuilder.Entity<PeriodicTask>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Auth Cache
        modelBuilder.Entity<AuthCache>(entity =>
        {
            entity.HasIndex(e => new { e.Username, e.Realm });
        });

        // User Cache
        modelBuilder.Entity<UserCache>(entity =>
        {
            entity.HasIndex(e => new { e.Username, e.Resolver });
        });

        // Monitoring Stats
        modelBuilder.Entity<MonitoringStats>(entity =>
        {
            entity.HasIndex(e => e.StatsKey);
            entity.HasIndex(e => e.Timestamp);
        });

        // Password Reset
        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasIndex(e => e.RecoveryCode);
        });

        // Custom User Attribute
        modelBuilder.Entity<CustomUserAttribute>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Resolver, e.Key });
        });

        // Service ID
        modelBuilder.Entity<ServiceId>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Client Application
        modelBuilder.Entity<ClientApplication>(entity =>
        {
            entity.HasIndex(e => e.IP);
        });

        // PrivacyIDEA Server
        modelBuilder.Entity<PrivacyIDEAServer>(entity =>
        {
            entity.HasIndex(e => e.Identifier).IsUnique();
        });

        // Node Name
        modelBuilder.Entity<NodeName>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}
