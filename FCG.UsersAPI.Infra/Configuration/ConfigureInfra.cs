using Amazon.SQS;
using FCG.Events;
using FCG.UsersAPI.Application.Interfaces;
using FCG.UsersAPI.Domain.Interfaces;
using FCG.UsersAPI.Infra.Messaging;
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

    private static void AddAwsMessaging(this IServiceCollection services)
    {
        // Credenciais/região via variáveis de ambiente padrão da AWS (fcg-shared-secret).
        services.AddAWSService<IAmazonSQS>();
        services.AddScoped<INotificationQueuePublisher, SqsNotificationQueuePublisher>();
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

                // O nome do exchange precisa ser igual em todos os serviços que publicam/consomem
                // este evento. Sem isso, o MassTransit usa o namespace .NET completo do tipo como
                // nome do exchange, e cada serviço tem sua própria cópia do contrato em um namespace
                // diferente — o que faz publisher e consumer conversarem com exchanges diferentes.
                cfg.Message<UserCreatedEvent>(x => x.SetEntityName("UserCreated"));

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
        services.AddAwsMessaging();
    }
}