using FCG.UsersAPI.Application.DTOs;
using FCG.UsersAPI.Application.Interfaces;
using FCG.UsersAPI.Application.Services;
using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.Interfaces;
using FCG.UsersAPI.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Authentication;

namespace FCG.UsersAPI.Tests.Unit.Application.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _tokenServiceMock = new Mock<ITokenService>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(c => c["Jwt:expirationMinutes"]).Returns("60");

        _sut = new AuthService(
            _userRepositoryMock.Object,
            _loggerMock.Object,
            _tokenServiceMock.Object,
            _configurationMock.Object);
    }

    private static User CreateUser(string email = "user@fcg.com")
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Senha@123");
        return new User("Test User", new Email(email), new Password(hashedPassword));
    }

    // ---- ValidateCredentials ----

    [Fact]
    public void ValidateCredentials_ValidEmailAndPassword_ReturnsTrue()
    {
        var result = _sut.ValidateCredentials(new Email("user@fcg.com"), new Password("Senha@123"));

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCredentials_InvalidEmail_ReturnsFalse()
    {
        var result = _sut.ValidateCredentials(new Email("invalid-email"), new Password("Senha@123"));

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCredentials_InvalidPassword_ReturnsFalse()
    {
        var result = _sut.ValidateCredentials(new Email("user@fcg.com"), new Password("weak"));

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCredentials_EmptyEmail_ReturnsFalse()
    {
        var result = _sut.ValidateCredentials(new Email(""), new Password("Senha@123"));

        result.Should().BeFalse();
    }

    // ---- AuthenticateAsync ----

    [Fact]
    public async Task AuthenticateAsync_InvalidEmail_ReturnsFailure()
    {
        var request = new LoginRequestDto
        {
            Email = new Email("not-an-email"),
            Password = new Password("Senha@123")
        };

        var result = await _sut.AuthenticateAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Credenciais inválidas.");
    }

    [Fact]
    public async Task AuthenticateAsync_UserNotFound_ReturnsFailure()
    {
        var request = new LoginRequestDto
        {
            Email = new Email("notfound@fcg.com"),
            Password = new Password("Senha@123")
        };
        _userRepositoryMock.Setup(r => r.GetUserByEmail(request.Email, default))
            .ReturnsAsync((User?)null);

        var result = await _sut.AuthenticateAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Credenciais inválidas.");
    }

    [Fact]
    public async Task AuthenticateAsync_WrongPassword_ThrowsAuthenticationException()
    {
        var user = CreateUser("user@fcg.com");
        var request = new LoginRequestDto
        {
            Email = new Email("user@fcg.com"),
            Password = new Password("WrongPass@1")
        };
        _userRepositoryMock.Setup(r => r.GetUserByEmail(request.Email, default))
            .ReturnsAsync(user);

        var act = async () => await _sut.AuthenticateAsync(request);

        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsSuccess()
    {
        var user = CreateUser("user@fcg.com");
        var request = new LoginRequestDto
        {
            Email = new Email("user@fcg.com"),
            Password = new Password("Senha@123")
        };
        _userRepositoryMock.Setup(r => r.GetUserByEmail(request.Email, default))
            .ReturnsAsync(user);
        _tokenServiceMock.Setup(t => t.GenerateToken(user)).Returns("jwt.token.here");

        var result = await _sut.AuthenticateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("jwt.token.here");
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsCorrectUsername()
    {
        var user = CreateUser("user@fcg.com");
        var request = new LoginRequestDto
        {
            Email = new Email("user@fcg.com"),
            Password = new Password("Senha@123")
        };
        _userRepositoryMock.Setup(r => r.GetUserByEmail(request.Email, default))
            .ReturnsAsync(user);
        _tokenServiceMock.Setup(t => t.GenerateToken(user)).Returns("jwt.token.here");

        var result = await _sut.AuthenticateAsync(request);

        result.Value!.Username.Should().Be(user.Name);
    }

    // ---- ValidateTokenAsync ----

    [Fact]
    public async Task ValidateTokenAsync_NullUser_ReturnsFailure()
    {
        _tokenServiceMock.Setup(t => t.ValidateTokenAsync("some.token"))
            .ReturnsAsync((User?)null);

        var result = await _sut.ValidateTokenAsync("some.token");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Token inválido");
    }

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsSuccess()
    {
        var user = CreateUser();
        _tokenServiceMock.Setup(t => t.ValidateTokenAsync("valid.token"))
            .ReturnsAsync((User?)user);

        var result = await _sut.ValidateTokenAsync("valid.token");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(user);
    }

    [Fact]
    public async Task ValidateTokenAsync_ServiceThrows_ThrowsAuthenticationException()
    {
        _tokenServiceMock.Setup(t => t.ValidateTokenAsync("bad.token"))
            .ThrowsAsync(new Exception("Token parse error"));

        var act = async () => await _sut.ValidateTokenAsync("bad.token");

        await act.Should().ThrowAsync<AuthenticationException>();
    }
}
