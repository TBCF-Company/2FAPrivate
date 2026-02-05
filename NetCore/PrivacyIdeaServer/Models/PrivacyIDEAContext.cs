// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
// Database context for PrivacyIDEA Server
// Converted and extended from Python SQLAlchemy models

using Microsoft.EntityFrameworkCore;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Models
{
    /// <summary>
    /// Database context for PrivacyIDEA Server
    /// Equivalent to Python's db (SQLAlchemy instance)
    /// </summary>
    public class PrivacyIDEAContext : DbContext
    {
        public PrivacyIDEAContext(DbContextOptions<PrivacyIDEAContext> options)
            : base(options)
        {
        }

        // Legacy - kept for backward compatibility
        public DbSet<PrivacyIDEAServerDB> PrivacyIDEAServers { get; set; } = null!;

        // Core configuration and system tables
        public DbSet<Config> Configs { get; set; } = null!;
        public DbSet<NodeName> NodeNames { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<PasswordReset> PasswordResets { get; set; } = null!;

        // User and realm management
        public DbSet<Resolver> Resolvers { get; set; } = null!;
        public DbSet<ResolverConfig> ResolverConfigs { get; set; } = null!;
        public DbSet<Realm> Realms { get; set; } = null!;
        public DbSet<ResolverRealm> ResolverRealms { get; set; } = null!;

        // Token management
        public DbSet<Token> Tokens { get; set; } = null!;
        public DbSet<TokenInfo> TokenInfos { get; set; } = null!;
        public DbSet<TokenOwner> TokenOwners { get; set; } = null!;
        public DbSet<TokenRealm> TokenRealms { get; set; } = null!;
        public DbSet<TokenCredentialIdHash> TokenCredentialIdHashes { get; set; } = null!;

        // Token containers and groups
        public DbSet<TokenContainer> TokenContainers { get; set; } = null!;
        public DbSet<TokenContainerInfo> TokenContainerInfos { get; set; } = null!;
        public DbSet<TokenContainerRealm> TokenContainerRealms { get; set; } = null!;
        public DbSet<TokenContainerOwner> TokenContainerOwners { get; set; } = null!;
        public DbSet<TokenContainerToken> TokenContainerTokens { get; set; } = null!;
        public DbSet<TokenContainerStates> TokenContainerStates { get; set; } = null!;
        public DbSet<TokenContainerTemplate> TokenContainerTemplates { get; set; } = null!;
        public DbSet<Tokengroup> Tokengroups { get; set; } = null!;
        public DbSet<TokenTokengroup> TokenTokengroups { get; set; } = null!;

        // Policy management
        public DbSet<Policy> Policies { get; set; } = null!;
        public DbSet<PolicyCondition> PolicyConditions { get; set; } = null!;
        public DbSet<PolicyDescription> PolicyDescriptions { get; set; } = null!;

        // Machine management
        public DbSet<MachineResolver> MachineResolvers { get; set; } = null!;
        public DbSet<MachineResolverConfig> MachineResolverConfigs { get; set; } = null!;
        public DbSet<MachineToken> MachineTokens { get; set; } = null!;
        public DbSet<MachineTokenOptions> MachineTokenOptions { get; set; } = null!;

        // Challenge-response
        public DbSet<Challenge> Challenges { get; set; } = null!;

        // Server configurations
        public DbSet<PrivacyIDEAServer> PrivacyIDEARemoteServers { get; set; } = null!;
        public DbSet<RADIUSServer> RADIUSServers { get; set; } = null!;
        public DbSet<SMTPServer> SMTPServers { get; set; } = null!;

        // Gateway configurations
        public DbSet<SMSGateway> SMSGateways { get; set; } = null!;
        public DbSet<SMSGatewayOption> SMSGatewayOptions { get; set; } = null!;

        // Event handling
        public DbSet<Database.EventHandler> EventHandlers { get; set; } = null!;
        public DbSet<EventHandlerOption> EventHandlerOptions { get; set; } = null!;
        public DbSet<EventHandlerCondition> EventHandlerConditions { get; set; } = null!;
        public DbSet<EventCounter> EventCounters { get; set; } = null!;

        // Periodic tasks
        public DbSet<PeriodicTask> PeriodicTasks { get; set; } = null!;
        public DbSet<PeriodicTaskOption> PeriodicTaskOptions { get; set; } = null!;
        public DbSet<PeriodicTaskLastRun> PeriodicTaskLastRuns { get; set; } = null!;

        // Audit and cache
        public DbSet<Audit> AuditLogs { get; set; } = null!;
        public DbSet<AuthCache> AuthCaches { get; set; } = null!;
        public DbSet<UserCache> UserCaches { get; set; } = null!;

        // Monitoring and services
        public DbSet<MonitoringStats> MonitoringStats { get; set; } = null!;
        public DbSet<Serviceid> Serviceids { get; set; } = null!;

        // CA Connector
        public DbSet<CAConnector> CAConnectors { get; set; } = null!;
        public DbSet<CAConnectorConfig> CAConnectorConfigs { get; set; } = null!;

        // Custom attributes
        public DbSet<CustomUserAttribute> CustomUserAttributes { get; set; } = null!;

        // Subscription management
        public DbSet<ClientApplication> ClientApplications { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Legacy entity configuration
            modelBuilder.Entity<PrivacyIDEAServerDB>()
                .HasIndex(p => p.Identifier)
                .IsUnique();

            // Configure relationships and constraints
            // Note: Most are handled by attributes, but complex ones can be configured here
            
            // Ensure proper cascade behavior for token-related entities
            modelBuilder.Entity<Token>()
                .HasMany(t => t.InfoList)
                .WithOne(ti => ti.Token)
                .HasForeignKey(ti => ti.TokenId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Token>()
                .HasMany(t => t.Owners)
                .WithOne(to => to.Token)
                .HasForeignKey(to => to.TokenId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Token>()
                .HasMany(t => t.RealmList)
                .WithOne(tr => tr.Token)
                .HasForeignKey(tr => tr.TokenId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure resolver relationships
            modelBuilder.Entity<Resolver>()
                .HasMany(r => r.ConfigList)
                .WithOne(rc => rc.Resolver)
                .HasForeignKey(rc => rc.ResolverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure policy relationships
            modelBuilder.Entity<Policy>()
                .HasMany(p => p.Conditions)
                .WithOne(pc => pc.Policy)
                .HasForeignKey(pc => pc.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Policy>()
                .HasMany(p => p.Descriptions)
                .WithOne(pd => pd.Policy)
                .HasForeignKey(pd => pd.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
