using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Infrastructure.Auth;
using ScriptFlow.API.Infrastructure.Persistence;

namespace ScriptFlow.API.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // In-memory repositories, one instance for the lifetime of the app (no real DB yet).
        services.AddSingleton<IPrescriptionRepository, InMemoryPrescriptionRepository>();
        services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
        services.AddSingleton<IProviderRepository, InMemoryProviderRepository>();
        services.AddSingleton<IMedicineRepository, InMemoryMedicineRepository>();
        services.AddSingleton<IPracticeRepository, InMemoryPracticeRepository>();
        services.AddSingleton<IPracticeLocationRepository, InMemoryPracticeLocationRepository>();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
