using DotNet9.Application.Users.Abstractions;
using DotNet9.Infrastructure.Users;
using Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserReadService, UserReadService>();

        return services;
    }
}
