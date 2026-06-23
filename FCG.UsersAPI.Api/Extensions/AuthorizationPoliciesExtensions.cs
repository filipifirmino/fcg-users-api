using FCG.UsersAPI.Api.Authorization;
using FCG.UsersAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace FCG.UsersAPI.Api.Extensions
{
    public static class AuthorizationPoliciesExtensions
    {
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build())
                .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(UserRole.Admin.ToString()))
                .AddPolicy(Policies.UserOrAdmin, policy => policy.RequireRole(UserRole.User.ToString(), UserRole.Admin.ToString()))
                .AddPolicy(Policies.UserOnly, policy => policy.RequireRole(UserRole.User.ToString()));
            return services;
        }
    }
}
