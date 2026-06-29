using FCG.UsersAPI.Application.DTOs;
using FCG.UsersAPI.Application.Events;
using FCG.UsersAPI.Application.Services;
using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.Interfaces;
using FCG.UsersAPI.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FCG.UsersAPI.Tests.Unit.Application.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly UserService _sut;
    private readonly Faker _faker;

    public UserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _sut = new UserService(_repositoryMock.Object, _loggerMock.Object, _publishEndpointMock.Object);
        _faker = new Faker("pt_BR");
    }

    private static User CreateFakeUser(string name = "João Silva", string email = "joao@fcg.com")
        => new(name, new Email(email), new Password("Senha@123"));

    private static RegisterRequestDto ValidRegisterRequest(string name = "João Silva", string email = "joao@fcg.com")
        => new()
        {
            Name = name,
            Email = new Email(email),
            Password = new Password("Senha@123")
        };

    // ---- RegisterAsync ----

    [Fact]
    public async Task RegisterAsync_InvalidRequest_ReturnsFailureResult()
    {
        var request = new RegisterRequestDto
        {
            Name = "",
            Email = new Email("invalid"),
            Password = new Password("weak")
        };

        var result = await _sut.RegisterAsync(request);

        result!.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid request data");
    }

    [Fact]
    public async Task RegisterAsync_UserAlreadyExists_ReturnsFailureResult()
    {
        var request = ValidRegisterRequest();
        var existingUser = CreateFakeUser();
        _repositoryMock.Setup(r => r.GetUserByEmail(request.Email, default))
            .ReturnsAsync(existingUser);

        var result = await _sut.RegisterAsync(request);

        result!.Success.Should().BeFalse();
        result.Message.Should().Be("Usuário já cadastrado");
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccessResult()
    {
        var request = ValidRegisterRequest();
        _repositoryMock.Setup(r => r.GetUserByEmail(request.Email, default))
            .ReturnsAsync((User?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var result = await _sut.RegisterAsync(request);

        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsCorrectUserData()
    {
        var request = ValidRegisterRequest("Maria Santos", "maria@fcg.com");
        _repositoryMock.Setup(r => r.GetUserByEmail(request.Email, default))
            .ReturnsAsync((User?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var result = await _sut.RegisterAsync(request);

        result!.Data!.Name.Should().Be("Maria Santos");
        result.Data.Email.Should().Be("maria@fcg.com");
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_PublishesUserCreatedEvent()
    {
        var request = ValidRegisterRequest();
        _repositoryMock.Setup(r => r.GetUserByEmail(request.Email, default))
            .ReturnsAsync((User?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        await _sut.RegisterAsync(request);

        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<UserCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_PublishesEventWithCorrectData()
    {
        var request = ValidRegisterRequest("Carlos Souza", "carlos@fcg.com");
        _repositoryMock.Setup(r => r.GetUserByEmail(request.Email, default))
            .ReturnsAsync((User?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        await _sut.RegisterAsync(request);

        _publishEndpointMock.Verify(
            p => p.Publish(
                It.Is<UserCreatedEvent>(e =>
                    e.Name == "Carlos Souza" &&
                    e.Email == "carlos@fcg.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CallsRepositoryAdd()
    {
        var request = ValidRegisterRequest();
        _repositoryMock.Setup(r => r.GetUserByEmail(request.Email, default))
            .ReturnsAsync((User?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        await _sut.RegisterAsync(request);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    // ---- GetAll ----

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsAllUsers()
    {
        var users = new List<User>
        {
            CreateFakeUser("User1", "user1@fcg.com"),
            CreateFakeUser("User2", "user2@fcg.com"),
            CreateFakeUser("User3", "user3@fcg.com")
        };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = await _sut.GetAll();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAll_EmptyRepository_ReturnsEmptyList()
    {
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

        var result = await _sut.GetAll();

        result.Should().BeEmpty();
    }

    // ---- GetById ----

    [Fact]
    public async Task GetById_ExistingId_ReturnsUser()
    {
        var user = CreateFakeUser();
        _repositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.GetById(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var result = await _sut.GetById(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ---- Update ----

    [Fact]
    public async Task Update_UserNotFound_ReturnsNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var result = await _sut.Update(Guid.NewGuid(), new UpdateUserDto { Name = "Novo Nome" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsUpdatedUser()
    {
        var user = CreateFakeUser();
        _repositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await _sut.Update(user.Id, new UpdateUserDto { Name = "Nome Atualizado" });

        result.Should().NotBeNull();
        result!.Name.Should().Be("Nome Atualizado");
    }

    [Fact]
    public async Task Update_ValidRequest_CallsRepositoryUpdate()
    {
        var user = CreateFakeUser();
        _repositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        await _sut.Update(user.Id, new UpdateUserDto { Name = "Nome Atualizado", IsActive = false });

        _repositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Update_WithIsActiveFalse_DeactivatesUser()
    {
        var user = CreateFakeUser();
        _repositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await _sut.Update(user.Id, new UpdateUserDto { IsActive = false });

        result!.IsActive.Should().BeFalse();
    }
}
