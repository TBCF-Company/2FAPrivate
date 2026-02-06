using XmlSigningExample.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "XML Signing API with 2FA", 
        Version = "v1",
        Description = "A centralized XML signing application that uses 2FA authentication. " +
                     "Each XML signing operation requires a 2-character authentication code that must be entered by the user."
    });
});

// Register XML Signing Service
builder.Services.AddSingleton<IXmlSigningService, XmlSigningService>();

// Register background service for session cleanup
builder.Services.AddHostedService<SessionCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
