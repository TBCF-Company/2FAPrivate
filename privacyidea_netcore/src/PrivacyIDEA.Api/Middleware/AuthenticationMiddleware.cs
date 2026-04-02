using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace PrivacyIDEA.Api.Middleware;

/// <summary>
/// JWT Authentication configuration extension
/// </summary>
public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"] ?? GenerateDefaultSecret();
        var key = Encoding.UTF8.GetBytes(jwtSecret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Set to true in production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "PrivacyIDEA",
                ValidateAudience = true,
                ValidAudience = "PrivacyIDEA",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    // Allow token to be passed in query string for WebSocket connections
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    private static string GenerateDefaultSecret()
    {
        // Generate a random secret if not configured
        // WARNING: This should be set in configuration in production
        var random = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(random);
        return Convert.ToBase64String(random);
    }
}

/// <summary>
/// Request logging middleware
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString("N")[..8];

        // Log request
        _logger.LogInformation(
            "[{RequestId}] {Method} {Path} started",
            requestId,
            context.Request.Method,
            context.Request.Path);

        try
        {
            await _next(context);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "[{RequestId}] {Method} {Path} completed with {StatusCode} in {Duration:F2}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                duration);
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogError(
                ex,
                "[{RequestId}] {Method} {Path} failed after {Duration:F2}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                duration);

            throw;
        }
    }
}

/// <summary>
/// API Key authentication handler (alternative to JWT)
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    private const string ApiKeyHeaderName = "PI-Authorization";

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next, 
        IConfiguration configuration,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if already authenticated via JWT
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Check for API key
        if (context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader))
        {
            var apiKey = apiKeyHeader.ToString();
            
            // Validate API key (this would typically check against database)
            var validApiKeys = _configuration.GetSection("ApiKeys").Get<string[]>() ?? Array.Empty<string>();
            
            if (validApiKeys.Contains(apiKey))
            {
                // Create claims for API key authentication
                var claims = new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "api-user"),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "api")
                };

                var identity = new System.Security.Claims.ClaimsIdentity(claims, "ApiKey");
                context.User = new System.Security.Claims.ClaimsPrincipal(identity);
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                result = new { status = false },
                detail = new { message = "Unauthorized" }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                result = new { status = false },
                detail = new { message = ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                result = new { status = false },
                detail = new { message = "Internal server error" }
            });
        }
    }
}
