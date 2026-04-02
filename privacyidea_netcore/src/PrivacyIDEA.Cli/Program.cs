using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Core.Services;
using PrivacyIDEA.Infrastructure.Data;

namespace PrivacyIDEA.Cli;

/// <summary>
/// PI-Manage CLI Tool
/// Maps to Python: pi-manage command line tool
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("PrivacyIDEA Management CLI")
        {
            Name = "pi-manage"
        };

        // Create tables command
        var createTablesCommand = new Command("create-tables", "Create database tables");
        var connectionStringOption = new Option<string>("--connection", "Database connection string") { IsRequired = true };
        var providerOption = new Option<string>("--provider", () => "sqlite", "Database provider (sqlite, mysql, postgresql)");
        createTablesCommand.AddOption(connectionStringOption);
        createTablesCommand.AddOption(providerOption);
        createTablesCommand.SetHandler(CreateTablesHandler, connectionStringOption, providerOption);
        rootCommand.AddCommand(createTablesCommand);

        // Create encryption key command
        var createEncKeyCommand = new Command("create-enckey", "Create encryption key file");
        var keyFileOption = new Option<string>("--file", () => "enckey", "Key file path");
        createEncKeyCommand.AddOption(keyFileOption);
        createEncKeyCommand.SetHandler(CreateEncKeyHandler, keyFileOption);
        rootCommand.AddCommand(createEncKeyCommand);

        // Create audit keys command
        var createAuditKeysCommand = new Command("create-audit-keys", "Create audit signing keys");
        var publicKeyOption = new Option<string>("--public", () => "public.pem", "Public key file path");
        var privateKeyOption = new Option<string>("--private", () => "private.pem", "Private key file path");
        createAuditKeysCommand.AddOption(publicKeyOption);
        createAuditKeysCommand.AddOption(privateKeyOption);
        createAuditKeysCommand.SetHandler(CreateAuditKeysHandler, publicKeyOption, privateKeyOption);
        rootCommand.AddCommand(createAuditKeysCommand);

        // Admin commands
        var adminCommand = new Command("admin", "Admin user management");
        
        var adminAddCommand = new Command("add", "Add admin user");
        var adminUsernameOption = new Option<string>("--username", "Admin username") { IsRequired = true };
        var adminEmailOption = new Option<string>("--email", "Admin email");
        var adminPasswordOption = new Option<string>("--password", "Admin password");
        adminAddCommand.AddOption(adminUsernameOption);
        adminAddCommand.AddOption(adminEmailOption);
        adminAddCommand.AddOption(adminPasswordOption);
        adminAddCommand.AddOption(connectionStringOption);
        adminAddCommand.SetHandler(AdminAddHandler, adminUsernameOption, adminEmailOption, adminPasswordOption, connectionStringOption);
        adminCommand.AddCommand(adminAddCommand);

        var adminListCommand = new Command("list", "List admin users");
        adminListCommand.AddOption(connectionStringOption);
        adminListCommand.SetHandler(AdminListHandler, connectionStringOption);
        adminCommand.AddCommand(adminListCommand);

        rootCommand.AddCommand(adminCommand);

        // Realm commands
        var realmCommand = new Command("realm", "Realm management");
        
        var realmListCommand = new Command("list", "List realms");
        realmListCommand.AddOption(connectionStringOption);
        realmListCommand.SetHandler(RealmListHandler, connectionStringOption);
        realmCommand.AddCommand(realmListCommand);

        rootCommand.AddCommand(realmCommand);

        // Token commands
        var tokenCommand = new Command("token", "Token management");
        
        var tokenListCommand = new Command("list", "List tokens");
        var userOption = new Option<string>("--user", "Filter by user");
        var realmOption = new Option<string>("--realm", "Filter by realm");
        tokenListCommand.AddOption(connectionStringOption);
        tokenListCommand.AddOption(userOption);
        tokenListCommand.AddOption(realmOption);
        tokenListCommand.SetHandler(TokenListHandler, connectionStringOption, userOption, realmOption);
        tokenCommand.AddCommand(tokenListCommand);

        var tokenJanitorCommand = new Command("janitor", "Clean up orphaned tokens");
        var dryRunOption = new Option<bool>("--dry-run", () => true, "Show what would be deleted without deleting");
        tokenJanitorCommand.AddOption(connectionStringOption);
        tokenJanitorCommand.AddOption(dryRunOption);
        tokenJanitorCommand.SetHandler(TokenJanitorHandler, connectionStringOption, dryRunOption);
        tokenCommand.AddCommand(tokenJanitorCommand);

        rootCommand.AddCommand(tokenCommand);

        // Policy commands
        var policyCommand = new Command("policy", "Policy management");
        
        var policyListCommand = new Command("list", "List policies");
        var policyActiveOption = new Option<bool?>("--active", "Filter by active status");
        policyListCommand.AddOption(connectionStringOption);
        policyListCommand.AddOption(policyActiveOption);
        policyListCommand.SetHandler(PolicyListHandler, connectionStringOption, policyActiveOption);
        policyCommand.AddCommand(policyListCommand);

        var policyExportCommand = new Command("export", "Export policies to file");
        var policyExportFileOption = new Option<string>("--file", () => "policies.json", "Output file path");
        policyExportCommand.AddOption(connectionStringOption);
        policyExportCommand.AddOption(policyExportFileOption);
        policyExportCommand.SetHandler(PolicyExportHandler, connectionStringOption, policyExportFileOption);
        policyCommand.AddCommand(policyExportCommand);

        var policyImportCommand = new Command("import", "Import policies from file");
        var policyImportFileOption = new Option<string>("--file", "Input file path") { IsRequired = true };
        policyImportCommand.AddOption(connectionStringOption);
        policyImportCommand.AddOption(policyImportFileOption);
        policyImportCommand.SetHandler(PolicyImportHandler, connectionStringOption, policyImportFileOption);
        policyCommand.AddCommand(policyImportCommand);

        rootCommand.AddCommand(policyCommand);

        // Event commands
        var eventCommand = new Command("event", "Event handler management");
        
        var eventListCommand = new Command("list", "List event handlers");
        eventListCommand.AddOption(connectionStringOption);
        eventListCommand.SetHandler(EventListHandler, connectionStringOption);
        eventCommand.AddCommand(eventListCommand);

        var eventEnableCommand = new Command("enable", "Enable event handler");
        var eventNameOption = new Option<string>("--name", "Event handler name") { IsRequired = true };
        eventEnableCommand.AddOption(connectionStringOption);
        eventEnableCommand.AddOption(eventNameOption);
        eventEnableCommand.SetHandler(async (conn, name) => await EventEnableHandler(conn, name), connectionStringOption, eventNameOption);
        eventCommand.AddCommand(eventEnableCommand);

        var eventDisableCommand = new Command("disable", "Disable event handler");
        eventDisableCommand.AddOption(connectionStringOption);
        eventDisableCommand.AddOption(eventNameOption);
        eventDisableCommand.SetHandler(async (conn, name) => await EventDisableHandler(conn, name), connectionStringOption, eventNameOption);
        eventCommand.AddCommand(eventDisableCommand);

        rootCommand.AddCommand(eventCommand);

        // Audit commands
        var auditCommand = new Command("audit", "Audit log management");
        
        var auditSearchCommand = new Command("search", "Search audit log");
        var auditActionOption = new Option<string>("--action", "Filter by action");
        var auditUserOption = new Option<string>("--user", "Filter by user");
        var auditDaysOption = new Option<int>("--days", () => 7, "Days to search back");
        auditSearchCommand.AddOption(connectionStringOption);
        auditSearchCommand.AddOption(auditActionOption);
        auditSearchCommand.AddOption(auditUserOption);
        auditSearchCommand.AddOption(auditDaysOption);
        auditSearchCommand.SetHandler(AuditSearchHandler, connectionStringOption, auditActionOption, auditUserOption, auditDaysOption);
        auditCommand.AddCommand(auditSearchCommand);

        var auditExportCommand = new Command("export", "Export audit log");
        var auditExportFileOption = new Option<string>("--file", () => "audit.csv", "Output file path");
        var auditExportDaysOption = new Option<int>("--days", () => 30, "Days to export");
        auditExportCommand.AddOption(connectionStringOption);
        auditExportCommand.AddOption(auditExportFileOption);
        auditExportCommand.AddOption(auditExportDaysOption);
        auditExportCommand.SetHandler(AuditExportHandler, connectionStringOption, auditExportFileOption, auditExportDaysOption);
        auditCommand.AddCommand(auditExportCommand);

        rootCommand.AddCommand(auditCommand);

        // Server configuration commands
        var configCommand = new Command("config", "Server configuration");
        
        var configGetCommand = new Command("get", "Get configuration value");
        var configKeyOption = new Option<string>("--key", "Configuration key");
        configGetCommand.AddOption(connectionStringOption);
        configGetCommand.AddOption(configKeyOption);
        configGetCommand.SetHandler(ConfigGetHandler, connectionStringOption, configKeyOption);
        configCommand.AddCommand(configGetCommand);

        var configSetCommand = new Command("set", "Set configuration value");
        var configValueOption = new Option<string>("--value", "Configuration value") { IsRequired = true };
        configSetCommand.AddOption(connectionStringOption);
        configSetCommand.AddOption(configKeyOption);
        configSetCommand.AddOption(configValueOption);
        configSetCommand.SetHandler(ConfigSetHandler, connectionStringOption, configKeyOption, configValueOption);
        configCommand.AddCommand(configSetCommand);

        var configListCommand = new Command("list", "List all configuration");
        configListCommand.AddOption(connectionStringOption);
        configListCommand.SetHandler(ConfigListHandler, connectionStringOption);
        configCommand.AddCommand(configListCommand);

        rootCommand.AddCommand(configCommand);

        // Database commands
        var dbCommand = new Command("db", "Database management");
        
        var dbMigrateCommand = new Command("migrate", "Apply database migrations");
        dbMigrateCommand.AddOption(connectionStringOption);
        dbMigrateCommand.AddOption(providerOption);
        dbMigrateCommand.SetHandler(DbMigrateHandler, connectionStringOption, providerOption);
        dbCommand.AddCommand(dbMigrateCommand);

        var dbBackupCommand = new Command("backup", "Backup database");
        var backupFileOption = new Option<string>("--file", () => "backup.sql", "Backup file path");
        dbBackupCommand.AddOption(connectionStringOption);
        dbBackupCommand.AddOption(backupFileOption);
        dbBackupCommand.SetHandler(DbBackupHandler, connectionStringOption, backupFileOption);
        dbCommand.AddCommand(dbBackupCommand);

        var dbStatsCommand = new Command("stats", "Show database statistics");
        dbStatsCommand.AddOption(connectionStringOption);
        dbStatsCommand.SetHandler(DbStatsHandler, connectionStringOption);
        dbCommand.AddCommand(dbStatsCommand);

        rootCommand.AddCommand(dbCommand);

        // Import/Export commands
        var exportCommand = new Command("export-data", "Export all data to JSON");
        var exportFileOption = new Option<string>("--file", () => "export.json", "Output file path");
        exportCommand.AddOption(connectionStringOption);
        exportCommand.AddOption(exportFileOption);
        exportCommand.SetHandler(ExportDataHandler, connectionStringOption, exportFileOption);
        rootCommand.AddCommand(exportCommand);

        var importCommand = new Command("import-data", "Import data from JSON");
        var importFileOption = new Option<string>("--file", "Input file path") { IsRequired = true };
        var importMergeOption = new Option<bool>("--merge", "Merge with existing data (default: replace)");
        importCommand.AddOption(connectionStringOption);
        importCommand.AddOption(importFileOption);
        importCommand.AddOption(importMergeOption);
        importCommand.SetHandler(ImportDataHandler, connectionStringOption, importFileOption, importMergeOption);
        rootCommand.AddCommand(importCommand);

        // Rotate audit command
        var rotateAuditCommand = new Command("rotate-audit", "Rotate audit log");
        var ageOption = new Option<int>("--age", () => 90, "Age in days");
        rotateAuditCommand.AddOption(connectionStringOption);
        rotateAuditCommand.AddOption(ageOption);
        rotateAuditCommand.SetHandler(RotateAuditHandler, connectionStringOption, ageOption);
        rootCommand.AddCommand(rotateAuditCommand);

        // API test command
        var testCommand = new Command("test", "Test configuration");
        testCommand.AddOption(connectionStringOption);
        testCommand.SetHandler(TestHandler, connectionStringOption);
        rootCommand.AddCommand(testCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task CreateTablesHandler(string connectionString, string provider)
    {
        Console.WriteLine($"Creating database tables using {provider}...");
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        services.AddDbContext<PrivacyIdeaDbContext>(options =>
        {
            switch (provider.ToLower())
            {
                case "mysql":
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;
                case "postgresql":
                    options.UseNpgsql(connectionString);
                    break;
                default:
                    options.UseSqlite(connectionString);
                    break;
            }
        });

        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetRequiredService<PrivacyIdeaDbContext>();

        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("Database tables created successfully.");
    }

    static async Task CreateEncKeyHandler(string keyFile)
    {
        Console.WriteLine($"Creating encryption key file: {keyFile}");
        
        var cryptoService = new CryptoService();
        await cryptoService.CreateEncryptionKeyAsync(keyFile);
        
        Console.WriteLine($"Encryption key created: {keyFile}");
        Console.WriteLine("IMPORTANT: Keep this file secure and backed up!");
    }

    static async Task CreateAuditKeysHandler(string publicKeyFile, string privateKeyFile)
    {
        Console.WriteLine("Creating audit signing keys...");
        
        var cryptoService = new CryptoService();
        var (publicKey, privateKey) = cryptoService.GenerateRsaKeyPair(2048);

        await File.WriteAllBytesAsync(publicKeyFile, publicKey);
        await File.WriteAllBytesAsync(privateKeyFile, privateKey);

        Console.WriteLine($"Public key: {publicKeyFile}");
        Console.WriteLine($"Private key: {privateKeyFile}");
        Console.WriteLine("IMPORTANT: Keep the private key secure!");
    }

    static async Task AdminAddHandler(string username, string? email, string? password, string connectionString)
    {
        Console.WriteLine($"Adding admin user: {username}");

        if (string.IsNullOrEmpty(password))
        {
            Console.Write("Enter password: ");
            password = ReadPassword();
            Console.WriteLine();
        }

        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();
        var cryptoService = services.GetRequiredService<ICryptoService>();

        var admin = new Domain.Entities.Admin
        {
            Username = username,
            Email = email ?? $"{username}@localhost",
            Password = cryptoService.HashPassword(password),
            Active = true
        };

        context.Admins.Add(admin);
        await context.SaveChangesAsync();

        Console.WriteLine($"Admin user '{username}' created successfully.");
    }

    static async Task AdminListHandler(string connectionString)
    {
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var admins = await context.Admins.ToListAsync();

        Console.WriteLine($"{"Username",-20} {"Email",-30} {"Active",-10}");
        Console.WriteLine(new string('-', 60));
        
        foreach (var admin in admins)
        {
            Console.WriteLine($"{admin.Username,-20} {admin.Email,-30} {admin.Active,-10}");
        }

        Console.WriteLine($"\nTotal: {admins.Count} admin(s)");
    }

    static async Task RealmListHandler(string connectionString)
    {
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var realms = await context.Realms
            .Include(r => r.ResolverRealms)
            .ToListAsync();

        Console.WriteLine($"{"Name",-20} {"Default",-10} {"Resolvers",-20}");
        Console.WriteLine(new string('-', 50));
        
        foreach (var realm in realms)
        {
            var resolvers = string.Join(", ", realm.ResolverRealms.Select(rr => rr.Resolver?.Name ?? "?"));
            Console.WriteLine($"{realm.Name,-20} {realm.IsDefault,-10} {resolvers,-20}");
        }

        Console.WriteLine($"\nTotal: {realms.Count} realm(s)");
    }

    static async Task TokenListHandler(string connectionString, string? user, string? realm)
    {
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var query = context.Tokens.AsQueryable();

        // Note: Full filtering would need joins with TokenOwner
        
        var tokens = await query.Take(100).ToListAsync();

        Console.WriteLine($"{"Serial",-15} {"Type",-10} {"Active",-8} {"FailCount",-10}");
        Console.WriteLine(new string('-', 50));
        
        foreach (var token in tokens)
        {
            Console.WriteLine($"{token.Serial,-15} {token.TokenType,-10} {token.Active,-8} {token.FailCount,-10}");
        }

        Console.WriteLine($"\nShowing {tokens.Count} token(s)");
    }

    static async Task TokenJanitorHandler(string connectionString, bool dryRun)
    {
        Console.WriteLine($"Running token janitor (dry-run: {dryRun})...");
        
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        // Find orphaned tokens (no owners)
        var orphanedTokens = await context.Tokens
            .Where(t => !t.TokenOwners.Any())
            .ToListAsync();

        Console.WriteLine($"Found {orphanedTokens.Count} orphaned token(s)");

        if (!dryRun && orphanedTokens.Count > 0)
        {
            context.Tokens.RemoveRange(orphanedTokens);
            await context.SaveChangesAsync();
            Console.WriteLine($"Deleted {orphanedTokens.Count} orphaned token(s)");
        }
        else if (orphanedTokens.Count > 0)
        {
            Console.WriteLine("Orphaned tokens:");
            foreach (var token in orphanedTokens.Take(10))
            {
                Console.WriteLine($"  - {token.Serial}");
            }
            if (orphanedTokens.Count > 10)
            {
                Console.WriteLine($"  ... and {orphanedTokens.Count - 10} more");
            }
        }
    }

    static async Task RotateAuditHandler(string connectionString, int age)
    {
        Console.WriteLine($"Rotating audit entries older than {age} days...");
        
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-age);
        var oldEntries = await context.AuditEntries
            .Where(a => a.Date < cutoffDate)
            .ToListAsync();

        Console.WriteLine($"Found {oldEntries.Count} entries to archive/delete");

        if (oldEntries.Count > 0)
        {
            context.AuditEntries.RemoveRange(oldEntries);
            await context.SaveChangesAsync();
            Console.WriteLine($"Deleted {oldEntries.Count} audit entries");
        }
    }

    static async Task TestHandler(string connectionString)
    {
        Console.WriteLine("Testing configuration...");
        
        try
        {
            var services = BuildServices(connectionString);
            var context = services.GetRequiredService<PrivacyIdeaDbContext>();

            // Test database connection
            var canConnect = await context.Database.CanConnectAsync();
            Console.WriteLine($"Database connection: {(canConnect ? "OK" : "FAILED")}");

            // Test crypto service
            var cryptoService = services.GetRequiredService<ICryptoService>();
            var testData = cryptoService.GenerateRandomBytes(16);
            Console.WriteLine($"Crypto service: OK (generated {testData.Length} bytes)");

            // Count entities
            var tokenCount = await context.Tokens.CountAsync();
            var realmCount = await context.Realms.CountAsync();
            var adminCount = await context.Admins.CountAsync();

            Console.WriteLine($"\nStatistics:");
            Console.WriteLine($"  Tokens: {tokenCount}");
            Console.WriteLine($"  Realms: {realmCount}");
            Console.WriteLine($"  Admins: {adminCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex.Message}");
        }
    }

    static IServiceProvider BuildServices(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        services.AddDbContext<PrivacyIdeaDbContext>(options =>
        {
            if (connectionString.Contains("Server=") || connectionString.Contains("Host="))
            {
                // MySQL or PostgreSQL
                if (connectionString.Contains("Host="))
                    options.UseNpgsql(connectionString);
                else
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddSingleton<ICryptoService, CryptoService>();

        return services.BuildServiceProvider();
    }

    static string ReadPassword()
    {
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Length--;
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        }
        return password.ToString();
    }

    // Policy handlers
    static async Task PolicyListHandler(string connectionString, bool? active)
    {
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var query = context.Policies.AsQueryable();
        if (active.HasValue)
            query = query.Where(p => p.Active == active.Value);

        var policies = await query.OrderBy(p => p.Name).ToListAsync();

        Console.WriteLine($"{"Name",-25} {"Scope",-15} {"Priority",-10} {"Active",-8}");
        Console.WriteLine(new string('-', 60));
        
        foreach (var policy in policies)
        {
            Console.WriteLine($"{policy.Name,-25} {policy.Scope,-15} {policy.Priority,-10} {policy.Active,-8}");
        }

        Console.WriteLine($"\nTotal: {policies.Count} policy(ies)");
    }

    static async Task PolicyExportHandler(string connectionString, string outputFile)
    {
        Console.WriteLine($"Exporting policies to {outputFile}...");
        
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var policies = await context.Policies
            .Include(p => p.Conditions)
            .ToListAsync();

        var json = System.Text.Json.JsonSerializer.Serialize(policies, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(outputFile, json);
        Console.WriteLine($"Exported {policies.Count} policy(ies) to {outputFile}");
    }

    static async Task PolicyImportHandler(string connectionString, string inputFile)
    {
        Console.WriteLine($"Importing policies from {inputFile}...");
        
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: File not found: {inputFile}");
            return;
        }

        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var json = await File.ReadAllTextAsync(inputFile);
        var policies = System.Text.Json.JsonSerializer.Deserialize<List<Domain.Entities.Policy>>(json);

        if (policies == null || policies.Count == 0)
        {
            Console.WriteLine("No policies found in file");
            return;
        }

        foreach (var policy in policies)
        {
            var existing = await context.Policies.FirstOrDefaultAsync(p => p.Name == policy.Name);
            if (existing != null)
            {
                Console.WriteLine($"  Updating policy: {policy.Name}");
                context.Entry(existing).CurrentValues.SetValues(policy);
            }
            else
            {
                Console.WriteLine($"  Adding policy: {policy.Name}");
                context.Policies.Add(policy);
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"Imported {policies.Count} policy(ies)");
    }

    // Event handlers
    static async Task EventListHandler(string connectionString)
    {
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var handlers = await context.EventHandlers
            .OrderBy(e => e.Name)
            .ToListAsync();

        Console.WriteLine($"{"Name",-25} {"HandlerModule",-20} {"Position",-10} {"Active",-8}");
        Console.WriteLine(new string('-', 70));
        
        foreach (var handler in handlers)
        {
            Console.WriteLine($"{handler.Name,-25} {handler.HandlerModule,-20} {handler.Position,-10} {handler.Active,-8}");
        }

        Console.WriteLine($"\nTotal: {handlers.Count} event handler(s)");
    }

    static async Task EventEnableHandler(string connectionString, string name)
    {
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var handler = await context.EventHandlers.FirstOrDefaultAsync(e => e.Name == name);
        if (handler == null)
        {
            Console.WriteLine($"Event handler not found: {name}");
            return;
        }

        handler.Active = true;
        await context.SaveChangesAsync();
        Console.WriteLine($"Event handler '{name}' enabled");
    }

    static async Task EventDisableHandler(string connectionString, string name)
    {
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var handler = await context.EventHandlers.FirstOrDefaultAsync(e => e.Name == name);
        if (handler == null)
        {
            Console.WriteLine($"Event handler not found: {name}");
            return;
        }

        handler.Active = false;
        await context.SaveChangesAsync();
        Console.WriteLine($"Event handler '{name}' disabled");
    }

    // Audit handlers
    static async Task AuditSearchHandler(string connectionString, string? action, string? user, int days)
    {
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-days);
        var query = context.AuditEntries
            .Where(a => a.Date >= cutoff);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action.Contains(action));
        if (!string.IsNullOrEmpty(user))
            query = query.Where(a => a.User != null && a.User.Contains(user));

        var entries = await query
            .OrderByDescending(a => a.Date)
            .Take(100)
            .ToListAsync();

        Console.WriteLine($"{"Date",-22} {"Action",-20} {"User",-15} {"Success",-8}");
        Console.WriteLine(new string('-', 70));
        
        foreach (var entry in entries)
        {
            Console.WriteLine($"{entry.Date:yyyy-MM-dd HH:mm:ss}  {entry.Action,-20} {entry.User ?? "-",-15} {entry.Success,-8}");
        }

        Console.WriteLine($"\nShowing {entries.Count} entries (max 100)");
    }

    static async Task AuditExportHandler(string connectionString, string outputFile, int days)
    {
        Console.WriteLine($"Exporting audit log (last {days} days) to {outputFile}...");
        
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-days);
        var entries = await context.AuditEntries
            .Where(a => a.Date >= cutoff)
            .OrderByDescending(a => a.Date)
            .ToListAsync();

        using var writer = new StreamWriter(outputFile);
        await writer.WriteLineAsync("Date,Action,User,Realm,Serial,Success,Info");
        
        foreach (var entry in entries)
        {
            await writer.WriteLineAsync($"\"{entry.Date:yyyy-MM-dd HH:mm:ss}\",\"{entry.Action}\",\"{entry.User}\",\"{entry.Realm}\",\"{entry.Serial}\",{entry.Success},\"{entry.Info?.Replace("\"", "\"\"")}\"");
        }

        Console.WriteLine($"Exported {entries.Count} audit entries to {outputFile}");
    }

    // Config handlers
    static async Task ConfigGetHandler(string connectionString, string? key)
    {
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        if (string.IsNullOrEmpty(key))
        {
            await ConfigListHandler(connectionString);
            return;
        }

        var config = await context.Configs.FirstOrDefaultAsync(c => c.Key == key);
        if (config == null)
        {
            Console.WriteLine($"Configuration key not found: {key}");
            return;
        }

        Console.WriteLine($"{config.Key} = {config.Value}");
    }

    static async Task ConfigSetHandler(string connectionString, string? key, string value)
    {
        if (string.IsNullOrEmpty(key))
        {
            Console.WriteLine("Error: --key is required");
            return;
        }

        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var config = await context.Configs.FirstOrDefaultAsync(c => c.Key == key);
        if (config == null)
        {
            config = new Domain.Entities.Config { Key = key, Value = value };
            context.Configs.Add(config);
        }
        else
        {
            config.Value = value;
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"Set {key} = {value}");
    }

    static async Task ConfigListHandler(string connectionString)
    {
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var configs = await context.Configs.OrderBy(c => c.Key).ToListAsync();

        Console.WriteLine($"{"Key",-40} {"Value",-40}");
        Console.WriteLine(new string('-', 80));
        
        foreach (var config in configs)
        {
            var value = config.Value?.Length > 37 ? config.Value.Substring(0, 37) + "..." : config.Value;
            Console.WriteLine($"{config.Key,-40} {value,-40}");
        }

        Console.WriteLine($"\nTotal: {configs.Count} configuration(s)");
    }

    // Database handlers
    static async Task DbMigrateHandler(string connectionString, string provider)
    {
        Console.WriteLine($"Applying database migrations using {provider}...");
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        services.AddDbContext<PrivacyIdeaDbContext>(options =>
        {
            switch (provider.ToLower())
            {
                case "mysql":
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;
                case "postgresql":
                    options.UseNpgsql(connectionString);
                    break;
                default:
                    options.UseSqlite(connectionString);
                    break;
            }
        });

        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetRequiredService<PrivacyIdeaDbContext>();

        Console.WriteLine("Checking for pending migrations...");
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var pending = pendingMigrations.ToList();

        if (pending.Count == 0)
        {
            Console.WriteLine("Database is up to date.");
            return;
        }

        Console.WriteLine($"Applying {pending.Count} migration(s):");
        foreach (var migration in pending)
        {
            Console.WriteLine($"  - {migration}");
        }

        await context.Database.MigrateAsync();
        Console.WriteLine("Migrations applied successfully.");
    }

    static async Task DbBackupHandler(string connectionString, string backupFile)
    {
        Console.WriteLine($"Creating database backup to {backupFile}...");
        
        // For SQLite, we can copy the file
        // For other databases, this would need provider-specific backup commands
        if (connectionString.StartsWith("Data Source="))
        {
            var dbFile = connectionString.Replace("Data Source=", "").Split(';')[0];
            if (File.Exists(dbFile))
            {
                File.Copy(dbFile, backupFile, true);
                Console.WriteLine($"Database backed up to {backupFile}");
            }
            else
            {
                Console.WriteLine($"Database file not found: {dbFile}");
            }
        }
        else
        {
            Console.WriteLine("Note: Full backup for MySQL/PostgreSQL requires database-specific tools.");
            Console.WriteLine("Use pg_dump for PostgreSQL or mysqldump for MySQL.");
            
            // Export data as JSON as a fallback
            var jsonFile = Path.ChangeExtension(backupFile, ".json");
            await ExportDataHandler(connectionString, jsonFile);
        }

        await Task.CompletedTask;
    }

    static async Task DbStatsHandler(string connectionString)
    {
        Console.WriteLine("Database Statistics");
        Console.WriteLine(new string('=', 40));
        
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        Console.WriteLine($"{"Table",-25} {"Count",-15}");
        Console.WriteLine(new string('-', 40));

        Console.WriteLine($"{"Tokens",-25} {await context.Tokens.CountAsync(),-15}");
        Console.WriteLine($"{"Users (TokenOwners)",-25} {await context.TokenOwners.CountAsync(),-15}");
        Console.WriteLine($"{"Realms",-25} {await context.Realms.CountAsync(),-15}");
        Console.WriteLine($"{"Resolvers",-25} {await context.Resolvers.CountAsync(),-15}");
        Console.WriteLine($"{"Policies",-25} {await context.Policies.CountAsync(),-15}");
        Console.WriteLine($"{"Admins",-25} {await context.Admins.CountAsync(),-15}");
        Console.WriteLine($"{"Audit Entries",-25} {await context.AuditEntries.CountAsync(),-15}");
        Console.WriteLine($"{"Event Handlers",-25} {await context.EventHandlers.CountAsync(),-15}");
        Console.WriteLine($"{"Machine Tokens",-25} {await context.MachineTokens.CountAsync(),-15}");
        Console.WriteLine($"{"Configuration",-25} {await context.Configs.CountAsync(),-15}");
    }

    // Export/Import handlers
    static async Task ExportDataHandler(string connectionString, string outputFile)
    {
        Console.WriteLine($"Exporting data to {outputFile}...");
        
        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var exportData = new Dictionary<string, object>
        {
            { "exportDate", DateTime.UtcNow },
            { "version", "1.0" },
            { "realms", await context.Realms.ToListAsync() },
            { "resolvers", await context.Resolvers.ToListAsync() },
            { "policies", await context.Policies.Include(p => p.Conditions).ToListAsync() },
            { "tokens", await context.Tokens.Include(t => t.TokenOwners).ToListAsync() },
            { "eventHandlers", await context.EventHandlers.Include(e => e.Options).Include(e => e.Conditions).ToListAsync() },
            { "smsGateways", await context.SmsGateways.Include(s => s.Options).ToListAsync() },
            { "radiusServers", await context.RadiusServers.ToListAsync() },
            { "configs", await context.Configs.ToListAsync() }
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        var json = System.Text.Json.JsonSerializer.Serialize(exportData, options);
        await File.WriteAllTextAsync(outputFile, json);

        Console.WriteLine($"Data exported to {outputFile}");
        Console.WriteLine("Note: Admin users and audit logs are NOT exported for security reasons.");
    }

    static async Task ImportDataHandler(string connectionString, string inputFile, bool merge)
    {
        Console.WriteLine($"Importing data from {inputFile}...");
        
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: File not found: {inputFile}");
            return;
        }

        Console.WriteLine($"Mode: {(merge ? "Merge" : "Replace")}");
        Console.WriteLine("WARNING: This will modify your database. Make a backup first!");
        Console.Write("Continue? (yes/no): ");
        var confirm = Console.ReadLine();
        
        if (confirm?.ToLower() != "yes")
        {
            Console.WriteLine("Import cancelled.");
            return;
        }

        var services = BuildServices(connectionString);
        var context = services.GetRequiredService<PrivacyIdeaDbContext>();

        var json = await File.ReadAllTextAsync(inputFile);
        using var doc = System.Text.Json.JsonDocument.Parse(json);

        // Import realms
        if (doc.RootElement.TryGetProperty("realms", out var realmsElement))
        {
            var realms = System.Text.Json.JsonSerializer.Deserialize<List<Domain.Entities.Realm>>(realmsElement.GetRawText());
            Console.WriteLine($"  Importing {realms?.Count ?? 0} realm(s)...");
            // Note: Full implementation would handle merge vs replace logic
        }

        // Import resolvers
        if (doc.RootElement.TryGetProperty("resolvers", out var resolversElement))
        {
            var resolvers = System.Text.Json.JsonSerializer.Deserialize<List<Domain.Entities.Resolver>>(resolversElement.GetRawText());
            Console.WriteLine($"  Importing {resolvers?.Count ?? 0} resolver(s)...");
        }

        // Import policies
        if (doc.RootElement.TryGetProperty("policies", out var policiesElement))
        {
            var policies = System.Text.Json.JsonSerializer.Deserialize<List<Domain.Entities.Policy>>(policiesElement.GetRawText());
            Console.WriteLine($"  Importing {policies?.Count ?? 0} policy(ies)...");
        }

        await context.SaveChangesAsync();
        Console.WriteLine("Import completed. Please review and verify the data.");
    }
}
