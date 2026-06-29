using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.ValueObjects;
using FCG.UsersAPI.Infra;
using FCG.UsersAPI.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FCG.UsersAPI.Tests.Unit.Infra.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _sut;
    private readonly Faker _faker;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new UserRepository(_context);
        _faker = new Faker("pt_BR");
    }

    public void Dispose() => _context.Dispose();

    private User CreateFakeUser(string? email = null)
        => new(
            _faker.Name.FullName(),
            new Email(email ?? _faker.Internet.Email()),
            new Password("Senha@123"));

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithUsers_ReturnsAllUsers()
    {
        await _sut.AddAsync(CreateFakeUser("user1@test.com"));
        await _sut.AddAsync(CreateFakeUser("user2@test.com"));
        await _sut.AddAsync(CreateFakeUser("user3@test.com"));

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        var user = CreateFakeUser();
        await _sut.AddAsync(user);

        var result = await _sut.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ValidUser_PersistsUser()
    {
        var user = CreateFakeUser();

        var result = await _sut.AddAsync(user);

        result.Should().NotBeNull();
        var persisted = await _context.Users.FindAsync(user.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_ValidUser_ReturnsSameUser()
    {
        var user = CreateFakeUser();

        var result = await _sut.AddAsync(user);

        result!.Id.Should().Be(user.Id);
        result.Name.Should().Be(user.Name);
    }

    [Fact]
    public async Task AddAsync_ValidUser_IsActiveByDefault()
    {
        var result = await _sut.AddAsync(CreateFakeUser());

        result!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_MultipleUsers_IncreasesCount()
    {
        await _sut.AddAsync(CreateFakeUser("a@test.com"));
        await _sut.AddAsync(CreateFakeUser("b@test.com"));

        var all = await _sut.GetAllAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ExistingUser_PersistsNameChange()
    {
        var user = CreateFakeUser();
        await _sut.AddAsync(user);

        user.Update("Nome Atualizado", null);
        await _sut.UpdateAsync(user);

        var updated = await _context.Users.FindAsync(user.Id);
        updated!.Name.Should().Be("Nome Atualizado");
    }

    [Fact]
    public async Task UpdateAsync_ExistingUser_PersistsIsActiveChange()
    {
        var user = CreateFakeUser();
        await _sut.AddAsync(user);

        user.Update(null, false);
        await _sut.UpdateAsync(user);

        var updated = await _context.Users.FindAsync(user.Id);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingUser_RemovesFromDatabase()
    {
        var user = CreateFakeUser();
        await _sut.AddAsync(user);

        await _sut.DeleteAsync(user);

        var deleted = await _context.Users.FindAsync(user.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingUser_DecreasesCount()
    {
        var user1 = CreateFakeUser("x@test.com");
        var user2 = CreateFakeUser("y@test.com");
        await _sut.AddAsync(user1);
        await _sut.AddAsync(user2);

        await _sut.DeleteAsync(user1);

        var all = await _sut.GetAllAsync();
        all.Should().HaveCount(1);
        all.Should().NotContain(u => u.Id == user1.Id);
    }

    [Fact]
    public async Task GetByIdAsync_AfterDelete_ReturnsNull()
    {
        var user = CreateFakeUser();
        await _sut.AddAsync(user);
        await _sut.DeleteAsync(user);

        var result = await _sut.GetByIdAsync(user.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByEmail_NonExistingEmail_ReturnsNull()
    {
        var result = await _sut.GetUserByEmail(new Email("notfound@test.com"));

        result.Should().BeNull();
    }
}
