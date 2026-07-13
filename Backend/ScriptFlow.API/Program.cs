using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ScriptFlow.API.Api.Middleware;
using ScriptFlow.API.Application;
using ScriptFlow.API.Infrastructure;
using ScriptFlow.API.Infrastructure.Auth;
using Serilog;
using Shared.Infrastructure;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Correlation;
using Shared.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

// Structured logging: every log line carries a "Service" and, once a request starts, a "CorrelationId".
Log.Logger = SerilogExtensions.CreateBaseConfiguration(builder.Configuration, "ScriptFlow.API").CreateLogger();
builder.Host.UseSerilog();

// Enums serialize as their name (e.g. "Signed") rather than a raw integer, so API responses
// and Swagger docs are self-explanatory to any client, including the Angular frontend.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Application: MediatR commands/queries + FluentValidation + logging pipeline behaviors.
builder.Services.AddApplication();

// Infrastructure: SQL Server-backed repositories, JWT issuing, password hashing (this project's own concerns).
builder.Services.AddInfrastructure(builder.Configuration);

// Shared.Infrastructure: correlation ID + RabbitMQ event publisher (reused by other services later).
builder.Services.AddSharedInfrastructure(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };

        // Closes the "stolen token stays valid until it expires" gap: logout (AuthController)
        // records the token's jti in IRevokedTokenStore, and every subsequent request - here and
        // in Notification.Service, which shares the same store type via a RabbitMQ-carried
        // TokenRevokedEvent - rejects it even though it's still within its natural expiry.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                var revokedTokens = context.HttpContext.RequestServices.GetRequiredService<IRevokedTokenStore>();
                if (!string.IsNullOrEmpty(jti) && await revokedTokens.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// Login/register throttling: 5 requests/minute per client IP, rejected immediately (no queueing -
// queuing a login flood just delays the same attacker instead of stopping them). Partitioned by
// remote IP so one client's attempts can't exhaust a budget shared by every other caller.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Lets the Angular dev server (a different origin) call this API.
const string AngularClientCorsPolicy = "AngularClient";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularClientCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ScriptFlow Prescription API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {your JWT}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCorrelationId();
app.UseExceptionHandling();

app.UseHttpsRedirection();

app.UseCors(AngularClientCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

// Exposes the otherwise-internal top-level-statement Program class to
// ScriptFlow.API.Tests, so WebApplicationFactory<Program> can boot this app in-process
// for integration tests - the standard ASP.NET Core pattern for this.
public partial class Program { }
