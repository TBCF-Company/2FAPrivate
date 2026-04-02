using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Infrastructure.Data;

/// <summary>
/// Main database context for PrivacyIDEA
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

    // Server configurations
    public DbSet<SmsGateway> SmsGateways => Set<SmsGateway>();
    public DbSet<SmsGatewayOption> SmsGatewayOptions => Set<SmsGatewayOption>();
    public DbSet<SmtpServer> SmtpServers => Set<SmtpServer>();
    public DbSet<RadiusServer> RadiusServers => Set<RadiusServer>();

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
    }
}
