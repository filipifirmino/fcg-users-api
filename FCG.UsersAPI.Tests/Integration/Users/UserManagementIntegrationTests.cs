using FCG.UsersAPI.Tests.Integration.Config;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FCG.UsersAPI.Tests.Integration.Users;

[Trait("Category", "Integration")]
public class UserManagementIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private const string BaseRoute = "/api/v1/user";

    public UserManagementIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    private static object RegisterBody(string name, string email, string password) =>
        new { Name = name, Email = new { Value = email }, Password = new { Value = password } };

    [Fact]
    public async Task Register_ValidData_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/register",
            Json(RegisterBody("Novo Usuário", "novousuario@fcg.com", "Senha@123")));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_ValidData_ReturnsUserEmail()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/register",
            Json(RegisterBody("Retorno User", "retorno@fcg.com", "Senha@123")));
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("retorno@fcg.com");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        await _factory.SeedUserAsync("Existing", "duplicate@fcg.com");
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/register",
            Json(RegisterBody("Duplicado", "duplicate@fcg.com", "Senha@123")));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsErrorMessage()
    {
        await _factory.SeedUserAsync("Dup User", "dup2@fcg.com");
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/register",
            Json(RegisterBody("Dup 2", "dup2@fcg.com", "Senha@123")));
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Usuário já cadastrado");
    }

    [Fact]
    public async Task Register_EmptyName_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/register",
            Json(RegisterBody("", "empty-name@fcg.com", "Senha@123")));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/register",
            Json(RegisterBody("Test User", "not-an-email", "Senha@123")));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/register",
            Json(RegisterBody("Test User", "weakpass@fcg.com", "weak")));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_AsAdmin_Returns200()
    {
        var client = _factory.CreateClientWithToken("Admin");

        var response = await client.GetAsync($"{BaseRoute}/get-all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_AsUser_Returns403()
    {
        var client = _factory.CreateClientWithToken("User");

        var response = await client.GetAsync($"{BaseRoute}/get-all");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BaseRoute}/get-all");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_AsAdmin_Returns200()
    {
        var user = await _factory.SeedUserAsync("Get By Id", "getbyid@fcg.com");
        var client = _factory.CreateClientWithToken("Admin");
        client.DefaultRequestHeaders.Add("id", user.Id.ToString());

        var response = await client.GetAsync($"{BaseRoute}/get-by-id");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_AsUser_Returns403()
    {
        var user = await _factory.SeedUserAsync("Forbidden User", "forbiddenuser@fcg.com");
        var client = _factory.CreateClientWithToken("User");
        client.DefaultRequestHeaders.Add("id", user.Id.ToString());

        var response = await client.GetAsync($"{BaseRoute}/get-by-id");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("id", Guid.NewGuid().ToString());

        var response = await client.GetAsync($"{BaseRoute}/get-by-id");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUser_AsAdmin_Returns200()
    {
        var user = await _factory.SeedUserAsync("Update Admin", "updateadmin@fcg.com");
        var client = _factory.CreateClientWithToken("Admin");

        var response = await client.PatchAsync(
            $"{BaseRoute}/update-by-id?id={user.Id}",
            Json(new { Name = "Nome Atualizado" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUser_AsUser_Returns200()
    {
        var user = await _factory.SeedUserAsync("Update Self", "updateself@fcg.com");
        var client = _factory.CreateClientWithToken("User", user.Id);

        var response = await client.PatchAsync(
            $"{BaseRoute}/update-by-id?id={user.Id}",
            Json(new { Name = "Self Updated" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUser_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsync(
            $"{BaseRoute}/update-by-id?id={Guid.NewGuid()}",
            Json(new { Name = "Hack" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUser_AsAdmin_ReturnsUpdatedName()
    {
        var user = await _factory.SeedUserAsync("Original Name", $"original{Guid.NewGuid():N}@fcg.com");
        var client = _factory.CreateClientWithToken("Admin");

        var response = await client.PatchAsync(
            $"{BaseRoute}/update-by-id?id={user.Id}",
            Json(new { Name = "Updated Name Admin" }));
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Updated Name Admin");
    }
}
