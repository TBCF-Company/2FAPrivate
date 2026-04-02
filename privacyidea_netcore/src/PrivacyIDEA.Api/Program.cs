using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Core.Services;
using PrivacyIDEA.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PrivacyIDEA API",
        Version = "v1",
        Description = "Multi-Factor Authentication Management System API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "PrivacyIDEA",
            Url = new Uri("https://privacyidea.org")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "AGPL-3.0",
            Url = new Uri("https://www.gnu.org/licenses/agpl-3.0.html")
        }
    });
});

// Configure Database (using SQLite for development, can be changed)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=privacyidea.db";
builder.Services.AddDbContext<PrivacyIdeaDbContext>(options =>
    options.UseSqlite(connectionString));

// Register Infrastructure (Repository pattern)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Core Services
builder.Services.AddSingleton<ICryptoService, CryptoService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Add HttpClient for external services
builder.Services.AddHttpClient();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PrivacyIDEA API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

// Version endpoint
app.MapGet("/version", () => Results.Ok(new { 
    version = "1.0.0", 
    product = "PrivacyIDEA.NET",
    framework = ".NET 8"
}))
    .WithName("Version")
    .WithTags("Info");

app.Run();
