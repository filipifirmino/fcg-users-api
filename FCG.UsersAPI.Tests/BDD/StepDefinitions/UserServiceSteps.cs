using FCG.UsersAPI.Application.DTOs;
using FCG.UsersAPI.Application.Events;
using FCG.UsersAPI.Application.Services;
using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.Interfaces;
using FCG.UsersAPI.Domain.ValueObjects;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Reqnroll;

namespace FCG.UsersAPI.Tests.BDD.StepDefinitions;

[Binding]
public class UserServiceSteps
{
    private readonly Mock<IUserRepository> _repositoryMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private UserService _sut = null!;

    private Guid _userId;
    private User? _currentUser;
    private User? _resultUser;
    private RegisterResultDto? _registerResult;
    private IEnumerable<User>? _resultUsers;
    private List<User> _usersInRepo = new();

    public UserServiceSteps()
    {
        _sut = new UserService(_repositoryMock.Object, _loggerMock.Object, _publishEndpointMock.Object);
    }

    // ---- Given ----

    [Given(@"já existe um usuário cadastrado com o e-mail ""(.*)""")]
    public void DadoJaExisteUmUsuarioCadastradoComOEmail(string email)
    {
        var existingUser = new User("Existing User", new Email(email), new Password("Senha@123HashedXYZ"));
        _repositoryMock.Setup(r => r.GetUserByEmail(It.Is<Email>(e => e.Value == email), default))
            .ReturnsAsync(existingUser);
    }

    [Given(@"(\d+) usuários cadastrados no sistema")]
    public void DadoNUsuariosCadastradosNoSistema(int quantidade)
    {
        _usersInRepo = Enumerable.Range(1, quantidade)
            .Select(i => new User($"User {i}", new Email($"user{i}@fcg.com"), new Password("Senha@123")))
            .ToList();
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(_usersInRepo);
    }

    [Given(@"nenhum usuário cadastrado no sistema")]
    public void DadoNenhumUsuarioCadastradoNoSistema()
    {
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
    }

    [Given(@"um usuário cadastrado no sistema")]
    public void DadoUmUsuarioCadastradoNoSistema()
    {
        _currentUser = new User("Usuário Teste", new Email("teste@fcg.com"), new Password("Senha@123"));
        _userId = _currentUser.Id;

        _repositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(_currentUser);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
    }

    // ---- When ----

    [When(@"o sistema tenta registrar um usuário com dados inválidos")]
    public async Task QuandoOSistemaTentaRegistrarUmUsuarioComDadosInvalidos()
    {
        var request = new RegisterRequestDto
        {
            Name = "",
            Email = new Email("invalid"),
            Password = new Password("weak")
        };
        _registerResult = await _sut.RegisterAsync(request);
    }

    [When(@"o sistema tenta registrar um usuário com o mesmo e-mail ""(.*)""")]
    public async Task QuandoOSistemaTentaRegistrarUmUsuarioComOMesmoEmail(string email)
    {
        var request = new RegisterRequestDto
        {
            Name = "Duplicado",
            Email = new Email(email),
            Password = new Password("Senha@123")
        };
        _registerResult = await _sut.RegisterAsync(request);
    }

    [When(@"o sistema registra um usuário com nome ""(.*)"" e-mail ""(.*)"" e senha ""(.*)""")]
    public async Task QuandoOSistemaRegistraUmUsuario(string nome, string email, string senha)
    {
        var request = new RegisterRequestDto
        {
            Name = nome,
            Email = new Email(email),
            Password = new Password(senha)
        };

        _repositoryMock.Setup(r => r.GetUserByEmail(It.Is<Email>(e => e.Value == email), default))
            .ReturnsAsync((User?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        _registerResult = await _sut.RegisterAsync(request);
    }

    [When(@"todos os usuários são listados")]
    public async Task QuandoTodosOsUsuariosSaoListados()
    {
        _resultUsers = await _sut.GetAll();
    }

    [When(@"o sistema busca o usuário pelo id cadastrado")]
    public async Task QuandoOSistemaBuscaOUsuarioPeloIdCadastrado()
    {
        _resultUser = await _sut.GetById(_userId);
    }

    [When(@"o sistema busca um usuário por id inexistente")]
    public async Task QuandoOSistemaBuscaUmUsuarioPorIdInexistente()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        _resultUser = await _sut.GetById(Guid.NewGuid());
    }

    [When(@"o sistema tenta atualizar um usuário com id inexistente")]
    public async Task QuandoOSistemaTentaAtualizarUmUsuarioComIdInexistente()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        _resultUser = await _sut.Update(Guid.NewGuid(), new UpdateUserDto { Name = "Qualquer" });
    }

    [When(@"o sistema atualiza o usuário com o nome ""(.*)""")]
    public async Task QuandoOSistemaAtualizaOUsuarioComONome(string nome)
    {
        _resultUser = await _sut.Update(_userId, new UpdateUserDto { Name = nome });
    }

    [When(@"o sistema desativa o usuário")]
    public async Task QuandoOSistemaDesativaOUsuario()
    {
        _resultUser = await _sut.Update(_userId, new UpdateUserDto { IsActive = false });
    }

    // ---- Then ----

    [Then(@"o resultado de registro deve ser falha com mensagem ""(.*)""")]
    public void EntaoOResultadoDeRegistroDeveSerFalhaComMensagem(string mensagem)
    {
        _registerResult.Should().NotBeNull();
        _registerResult!.Success.Should().BeFalse();
        _registerResult.Message.Should().Be(mensagem);
    }

    [Then(@"o resultado de registro deve ser sucesso")]
    public void EntaoOResultadoDeRegistroDeveSerSucesso()
    {
        _registerResult.Should().NotBeNull();
        _registerResult!.Success.Should().BeTrue();
    }

    [Then(@"o usuário retornado deve ter nome ""(.*)""")]
    public void EntaoOUsuarioRetornadoDeveTerNome(string nome)
    {
        _registerResult!.Data.Should().NotBeNull();
        _registerResult.Data!.Name.Should().Be(nome);
    }

    [Then(@"o usuário retornado deve ter e-mail ""(.*)""")]
    public void EntaoOUsuarioRetornadoDeveTerEmail(string email)
    {
        _registerResult!.Data!.Email.Should().Be(email);
    }

    [Then(@"o evento de criação de usuário deve ter sido publicado")]
    public void EntaoOEventoDeCriacaoDeUsuarioDeveTerSidoPublicado()
    {
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<UserCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Then(@"o repositório deve ter sido chamado para adicionar o usuário")]
    public void EntaoORepositorioDeveTerSidoChamadoParaAdicionarOUsuario()
    {
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Then(@"devem ser retornados (\d+) usuários")]
    public void EntaoDevemSerRetornadosNUsuarios(int quantidade)
    {
        _resultUsers.Should().HaveCount(quantidade);
    }

    [Then(@"o usuário deve ser retornado com sucesso")]
    public void EntaoOUsuarioDeveSerRetornadoComSucesso()
    {
        _resultUser.Should().NotBeNull();
        _resultUser!.Id.Should().Be(_userId);
    }

    [Then(@"o resultado deve ser nulo")]
    public void EntaoOResultadoDeveSerNulo()
    {
        _resultUser.Should().BeNull();
    }

    [Then(@"o usuário deve ter nome ""(.*)""")]
    public void EntaoOUsuarioDeveTerNome(string nome)
    {
        _resultUser.Should().NotBeNull();
        _resultUser!.Name.Should().Be(nome);
    }

    [Then(@"o repositório deve ter sido chamado para atualizar o usuário")]
    public void EntaoORepositorioDeveTerSidoChamadoParaAtualizarOUsuario()
    {
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Then(@"o usuário deve estar inativo")]
    public void EntaoOUsuarioDeveEstarInativo()
    {
        _resultUser.Should().NotBeNull();
        _resultUser!.IsActive.Should().BeFalse();
    }
}
