using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FCG.UsersAPI.Api.Extensions
{
    public static class JwtExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options =>
               {
                   options.MapInboundClaims = true;
                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = true,
                       ValidateAudience = true,
                       ValidateLifetime = true,
                       ValidateIssuerSigningKey = true,
                       ValidIssuer = configuration["Jwt:Issuer"],
                       ValidAudience = configuration["Jwt:Audience"],
                       IssuerSigningKey = new SymmetricSecurityKey(
                           Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"])),
                       ClockSkew = TimeSpan.Zero,
                       RoleClaimType = ClaimTypes.Role,
                       NameClaimType = ClaimTypes.NameIdentifier
                   };

                   options.Events = new JwtBearerEvents
                   {
                       // 401 - Token inválido ou ausente
                       OnChallenge = context =>
                       {
                           // Prevenir resposta padrão
                           context.HandleResponse();

                           context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                           context.Response.ContentType = "application/json";

                           var response = new
                           {
                               status = 401,
                               error = "Unauthorized",
                               message = "Invalid or missing authentication token. Please log in.",
                               timestamp = DateTime.UtcNow
                           };

                           return context.Response.WriteAsJsonAsync(response);
                       },

                       // 403 - Token válido mas sem permissão
                       OnForbidden = context =>
                       {
                           context.Response.StatusCode = StatusCodes.Status403Forbidden;
                           context.Response.ContentType = "application/json";

                           var response = new
                           {
                               status = 403,
                               error = "Forbidden",
                               message = "You do not have permission to access this resource.",
                               timestamp = DateTime.UtcNow
                           };

                           return context.Response.WriteAsJsonAsync(response);
                       },
                   };
               });

            services.AddAuthorization();

            return services;
        }
    }

}