using Dispatch.Worker;
using Dispatch.Worker.Infrastructure;
using Serilog;
using Shared.Infrastructure;
using Shared.Infrastructure.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Structured logging: every log line carries a "Service" property, and - once a message
// starts being processed - a "CorrelationId" that traces back to the HTTP request in
// ScriptFlow.API that originally signed the prescription. Same bootstrap ScriptFlow.API uses.
Log.Logger = SerilogExtensions.CreateBaseConfiguration(builder.Configuration, "Dispatch.Worker").CreateLogger();
builder.Services.AddSerilog();

// Shared.Infrastructure: correlation ID accessor + RabbitMQ event publisher (for
// publishing PrescriptionAcknowledgedEvent/PrescriptionRejectedEvent), same as ScriptFlow.API.
builder.Services.AddSharedInfrastructure(builder.Configuration);

// This project's own concerns: the pharmacy HTTP client (with Polly retry), the in-memory
// idempotency store, the dispatch handler, and the RabbitMQ consumer for PrescriptionSignedEvent.
builder.Services.AddDispatchWorker(builder.Configuration);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
