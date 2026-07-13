using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ScriptFlow.API.Tests.Integration;

/// <summary>
/// Boots the real ScriptFlow.API pipeline in-process, pinned to Development so it loads
/// appsettings.Development.json's real connection string and RabbitMq credentials - the
/// same configuration `dotnet run` uses locally. No fakes/mocks are substituted: this
/// talks to the real local SQL Server and RabbitMQ, same as manual smoke testing does.
/// </summary>
public sealed class ScriptFlowApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
