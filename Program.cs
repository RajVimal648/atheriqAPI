using atheriqAPI.Services.Email;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Load secrets from appsettings.Local.json
builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: false
);

// Controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "atheriqAPI",
        Version = "v1",
        Description = "atheriq API"
    });
});

// CORS
var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Website", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .WithMethods("POST")
            .WithHeaders("Content-Type");
    });
});

// Email configuration
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);

// Email service (SMTP via MailKit)
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("contact", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }
        )
    );
});

var app = builder.Build();

// Swagger
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "atheriqAPI v1"
    );

    options.RoutePrefix = "swagger";
});

// CORS
app.UseCors("Website");

// Rate limiting
app.UseRateLimiter();

// Authorization
app.UseAuthorization();

// Controllers
app.MapControllers();

// Health/root endpoint
app.MapGet("/", () => Results.Ok(new
{
    status = "running",
    application = "atheriqAPI",
    framework = ".NET 8"
}));

app.Run();