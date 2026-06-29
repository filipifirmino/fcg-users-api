using FCG.UsersAPI.Tests.Integration.Config;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FCG.UsersAPI.Tests.Integration.Auth;

[Trait("Category", "Integration")]
public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private const string BaseRoute = "/api/v1/auth";

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    private static object LoginBody(string email, string password) =>
        new { Email = new { Value = email }, Password = new { Value = password } };

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        await _factory.SeedUserAsync("Login Test", "login@fcg.com", "Senha@123");
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/login", Json(LoginBody("login@fcg.com", "Senha@123")));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenInBody()
    {
        await _factory.SeedUserAsync("Token Test", "tokentest@fcg.com", "Senha@123");
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/login", Json(LoginBody("tokentest@fcg.com", "Senha@123")));
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Token");
    }

    [Fact]
    public async Task Login_UserNotFound_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/login", Json(LoginBody("notregistered@fcg.com", "Senha@123")));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/login", Json(LoginBody("not-an-email", "Senha@123")));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_InvalidPasswordFormat_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/login", Json(LoginBody("user@fcg.com", "weak")));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsServerError()
    {
        await _factory.SeedUserAsync("Wrong Pass", "wrongpass@fcg.com", "Senha@123");
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/login", Json(LoginBody("wrongpass@fcg.com", "OutraSenha@9")));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
