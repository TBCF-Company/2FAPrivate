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
}
