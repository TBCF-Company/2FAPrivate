// SPDX-License-Identifier: AGPL-3.0-or-later
// Database context for PrivacyIDEA Server

using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models
{
    /// <summary>
    /// Database context for PrivacyIDEA Server
    /// </summary>
    public class PrivacyIDEAContext : DbContext
    {
        public PrivacyIDEAContext(DbContextOptions<PrivacyIDEAContext> options)
            : base(options)
        {
        }

        public DbSet<PrivacyIDEAServerDB> PrivacyIDEAServers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Create unique index on Identifier
            modelBuilder.Entity<PrivacyIDEAServerDB>()
                .HasIndex(p => p.Identifier)
                .IsUnique();
        }
    }
}
