// SPDX-License-Identifier: AGPL-3.0-or-later
// PrivacyIDEA Server - .NET Core 8 Port
// Port of Python Flask app.py to ASP.NET Core

using Microsoft.EntityFrameworkCore;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Lib;
using PrivacyIdeaServer.Services;
using TwoFactorAuth.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure database context (using PostgreSQL)
builder.Services.AddDbContext<PrivacyIDEAContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' not found. Please configure in appsettings.json or environment variables.");
    options.UseNpgsql(connectionString);
});

// Add HTTP client for making requests to remote privacyIDEA servers
builder.Services.AddHttpClient();

// Add named HttpClient for servers that don't validate TLS
builder.Services.AddHttpClient("NoTlsValidation")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

// Register services with Dependency Injection
builder.Services.AddScoped<IPrivacyIDEAServerService, PrivacyIDEAServerService>();
builder.Services.AddScoped<IOtpTokenService, OtpTokenService>();
builder.Services.AddSingleton<IDeviceManagementService, DeviceManagementService>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PrivacyIDEA Server API",
        Version = "v1",
        Description = "REST API for managing remote privacyIDEA server configurations. " +
                      "Port of Python privacyIDEA server to .NET Core 8.",
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "AGPL-3.0-or-later",
            Url = new Uri("https://www.gnu.org/licenses/agpl-3.0.html")
        }
    });
});

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PrivacyIDEAContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PrivacyIDEA Server API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Add a health check endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

// Add an info endpoint
app.MapGet("/info", () => Results.Ok(new
{
    application = "PrivacyIDEA Server",
    version = "1.0.0",
    framework = ".NET Core 8",
    description = "Port of Python privacyIDEA server to .NET Core 8"
}))
.WithName("Info")
.WithOpenApi();

app.Run();
