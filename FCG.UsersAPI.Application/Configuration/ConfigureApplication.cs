using FCG.UsersAPI.Application.Interfaces;
using FCG.UsersAPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.UsersAPI.Application.Configuration
{
    public static class ApplicationConfigure
    {
        private static void ConfigureDependences(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IAuthService, AuthService>();
            serviceCollection.AddScoped<IUserService, UserService>();
            serviceCollection.AddScoped<ITokenService, TokenService>();
        }
        public static void AddApplicationConfiguration(this IServiceCollection serviceCollection)
        {
            serviceCollection.ConfigureDependences();
        }
    }
}
