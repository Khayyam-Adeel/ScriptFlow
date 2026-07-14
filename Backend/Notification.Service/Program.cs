using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Notification.Service.Application;
using Notification.Service.Hubs;
using Serilog;
using Shared.Events;
using Shared.Infrastructure;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Structured logging: same bootstrap ScriptFlow.API/Dispatch.Worker use.
Log.Logger = SerilogExtensions.CreateBaseConfiguration(builder.Configuration, "Notification.Service").CreateLogger();
builder.Host.UseSerilog();

// Shared.Infrastructure: correlation ID accessor + RabbitMQ event publisher/consumer plumbing.
builder.Services.AddSharedInfrastructure(builder.Configuration);

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// JWT validation must match ScriptFlow.API's Issuer/Audience/SigningKey exactly (see this
// project's appsettings.json "Jwt" section) so a token minted by ScriptFlow.API authenticates
// the SignalR connection here too.
var jwtSection = builder.Configuration.GetSection("Jwt");
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
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"] ?? string.Empty))
        };

        // Browsers can't set an Authorization header on the WebSocket handshake, so SignalR
        // clients send the token as a query string param instead; this pulls it back out for
        // JWT validation, only for requests actually hitting a hub endpoint.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },

            // Same revocation check as ScriptFlow.API, backed by the same store type - kept in
            // sync across processes via TokenRevokedEvent (see TokenRevokedEventHandler below).
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

const string AngularClientCorsPolicy = "AngularClient";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    // SignalR's WebSocket transport requires an explicit origin list (not AllowAnyOrigin) plus
    // AllowCredentials, since the connection carries the bearer token.
    options.AddPolicy(AngularClientCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddSingleton<PrescriptionStatusChangedEventHandler>();
builder.Services.AddRabbitMqConsumer<PrescriptionStatusChangedEvent>(new RabbitMqConsumerSettings
{
    QueueName = "notification.prescription-status-changed",
    RoutingKey = nameof(PrescriptionStatusChangedEvent),
    DeadLetterQueueName = "notification.prescription-status-changed.dlq"
});
builder.Services.AddHostedService(provider => new EventConsumerBackgroundService<PrescriptionStatusChangedEvent>(
    provider.GetRequiredService<IEventConsumer<PrescriptionStatusChangedEvent>>(),
    provider.GetRequiredService<PrescriptionStatusChangedEventHandler>().HandleAsync));

builder.Services.AddSingleton<MessageDeadLetteredEventHandler>();
builder.Services.AddRabbitMqConsumer<MessageDeadLetteredEvent>(new RabbitMqConsumerSettings
{
    QueueName = "notification.message-dead-lettered",
    RoutingKey = nameof(MessageDeadLetteredEvent),
    DeadLetterQueueName = "notification.message-dead-lettered.dlq"
});
builder.Services.AddHostedService(provider => new EventConsumerBackgroundService<MessageDeadLetteredEvent>(
    provider.GetRequiredService<IEventConsumer<MessageDeadLetteredEvent>>(),
    provider.GetRequiredService<MessageDeadLetteredEventHandler>().HandleAsync));

builder.Services.AddSingleton<TokenRevokedEventHandler>();
builder.Services.AddRabbitMqConsumer<TokenRevokedEvent>(new RabbitMqConsumerSettings
{
    QueueName = "notification.token-revoked",
    RoutingKey = nameof(TokenRevokedEvent),
    DeadLetterQueueName = "notification.token-revoked.dlq"
});
builder.Services.AddHostedService(provider => new EventConsumerBackgroundService<TokenRevokedEvent>(
    provider.GetRequiredService<IEventConsumer<TokenRevokedEvent>>(),
    provider.GetRequiredService<TokenRevokedEventHandler>().HandleAsync));

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseCors(AngularClientCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<PrescriptionHub>("/hubs/prescriptions");

app.Run();
