using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging: console + rolling daily file under Logs/, same output shape as the
// other three services (Shared.Infrastructure/Logging/SerilogExtensions.cs). Bootstrapped
// directly here rather than via that shared helper, since this project deliberately has no
// dependency on Shared.Infrastructure (no DB/RabbitMQ concerns of its own to justify pulling
// in Dapper/SqlClient/RabbitMQ.Client).
const string outputTemplate =
    "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {SourceContext}: {Message:lj}{NewLine}{Exception}";
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "PharmacyGateway.mock")
    .WriteTo.Console(outputTemplate: outputTemplate)
    .WriteTo.File(
        path: "Logs/PharmacyGateway.mock-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: outputTemplate)
    .CreateLogger();
builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
