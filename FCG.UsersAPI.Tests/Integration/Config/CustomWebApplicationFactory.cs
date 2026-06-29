using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.ValueObjects;
using FCG.UsersAPI.Infra;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FCG.UsersAPI.Tests.Integration.Config;

public class CustomWebApplicationFactory : WebApplicationFactory<FCG.UsersAPI.Api.Startup>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    public const string TestJwtSecret = "test-super-secret-key-that-is-at-least-32-chars!!";
    public const string TestJwtIssuer = "FCG.UsersAPI";
    public const string TestJwtAudience = "FCG.Client";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "",
                ["Jwt:SecretKey"] = TestJwtSecret,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:ExpirationMinutes"] = "60",
                ["RabbitMq:Host"] = "localhost",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            var massTransitHostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            (d.ImplementationType?.FullName?.StartsWith("MassTransit") == true ||
                             d.ImplementationFactory?.Method.DeclaringType?.FullName?.StartsWith("MassTransit") == true))
                .ToList();
            foreach (var d in massTransitHostedServices) services.Remove(d);

            var publishDescriptors = services
                .Where(d => d.ServiceType == typeof(IPublishEndpoint))
                .ToList();
            foreach (var d in publishDescriptors) services.Remove(d);

            services.AddSingleton(Mock.Of<IPublishEndpoint>());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    public IServiceScope CreateTestScope() => Services.CreateScope();

    public async Task<User> SeedUserAsync(string name = "Test User", string email = "test@fcg.com", string password = "Senha@123")
    {
        using var scope = CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User(name, new Email(email), new Password(hashedPassword));
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public HttpClient CreateClientWithToken(string role, Guid? userId = null)
    {
        var client = CreateClient();
        var token = GenerateToken(userId ?? Guid.NewGuid(), role);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static string GenerateToken(Guid userId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Email, $"test-{role.ToLower()}@test.com"),
            new Claim(ClaimTypes.Name, $"Test {role}")
        };

        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
