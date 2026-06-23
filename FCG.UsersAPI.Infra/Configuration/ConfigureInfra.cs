using FCG.UsersAPI.Domain.Interfaces;
using FCG.UsersAPI.Infra.Repositories;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.UsersAPI.Infra.Configuration;

public static class ConfigureInfra
{
    private static void AddRepository(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
    }

    private static void AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"] ?? "localhost", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "guest");
                    h.Password(configuration["RabbitMq:Password"] ?? "guest");
                });

                cfg.UseMessageRetry(r =>
                    r.Interval(3, TimeSpan.FromSeconds(5)));
            });
        });
    }

    public static void AddConfigureInfra(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRepository();
        services.AddRabbitMq(configuration);
    }
}