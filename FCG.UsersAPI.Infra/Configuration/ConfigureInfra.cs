using FCG.UsersAPI.Domain.Interfaces;
using FCG.UsersAPI.Infra.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.UsersAPI.Infra.Configuration;

public static class ConfigureInfra
{
    private static void AddRepository(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
    }

    private static void AddRabbitMq(this IServiceCollection services)
    {
        //Adicione aqui toda configuração de DI referente a mensageria
    }

    public static void AddConfigureInfra(this IServiceCollection services)
    {
        services.AddRepository();
        services.AddRabbitMq();
    }
}
