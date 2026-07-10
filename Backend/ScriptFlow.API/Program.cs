using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ScriptFlow.API.Api.Middleware;
using ScriptFlow.API.Application;
using ScriptFlow.API.Infrastructure;
using ScriptFlow.API.Infrastructure.Auth;
using ScriptFlow.API.Infrastructure.Database;
using Serilog;
using Shared.Infrastructure;
using Shared.Infrastructure.Correlation;
using Shared.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

// Structured logging: every log line carries a "Service" and, once a request starts, a "CorrelationId".
Log.Logger = SerilogExtensions.CreateBaseConfiguration(builder.Configuration, "ScriptFlow.API").CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Application: MediatR commands/queries + FluentValidation + logging pipeline behaviors.
builder.Services.AddApplication();

// Infrastructure: in-memory repositories, JWT issuing, password hashing (this project's own concerns).
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
    });

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Temporary connectivity check: repositories still run in-memory until the stored
// procedures exist, so this is the only thing exercising ISqlConnectionFactory for now.
app.MapGet("/health/db", async (ISqlConnectionFactory factory, CancellationToken cancellationToken) =>
{
    using var connection = await factory.CreateOpenConnectionAsync(cancellationToken);
    var result = await connection.ExecuteScalarAsync<int>("SELECT 1");
    return Results.Ok(new { database = "ScriptFlow_DEV", connected = result == 1 });
});

app.Run();
