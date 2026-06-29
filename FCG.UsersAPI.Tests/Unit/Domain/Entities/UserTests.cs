using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.Enums;
using FCG.UsersAPI.Domain.ValueObjects;

namespace FCG.UsersAPI.Tests.Unit.Domain.Entities;

public class UserTests
{
    private static Email ValidEmail() => new("test@fcg.com");
    private static Password ValidPassword() => new("Senha@123");

    [Fact]
    public void Constructor_EmptyName_ThrowsException()
    {
        var act = () => new User("", ValidEmail(), ValidPassword());

        act.Should().Throw<Exception>().WithMessage("Nome é obrigatório");
    }

    [Fact]
    public void Constructor_WhitespaceName_ThrowsException()
    {
        var act = () => new User("   ", ValidEmail(), ValidPassword());

        act.Should().Throw<Exception>().WithMessage("Nome é obrigatório");
    }

    [Fact]
    public void Constructor_ValidData_CreatesUser()
    {
        var user = new User("João Silva", ValidEmail(), ValidPassword());

        user.Should().NotBeNull();
        user.Name.Should().Be("João Silva");
        user.Email.Value.Should().Be("test@fcg.com");
    }

    [Fact]
    public void Constructor_NewUser_HasGeneratedId()
    {
        var user = new User("João Silva", ValidEmail(), ValidPassword());

        user.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_NewUser_IsActiveByDefault()
    {
        var user = new User("João Silva", ValidEmail(), ValidPassword());

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NewUser_HasUserRole()
    {
        var user = new User("João Silva", ValidEmail(), ValidPassword());

        user.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public void Constructor_NewUser_HasCreatedAt()
    {
        var before = DateTime.UtcNow;
        var user = new User("João Silva", ValidEmail(), ValidPassword());

        user.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Update_WithNewName_ChangesName()
    {
        var user = new User("Nome Original", ValidEmail(), ValidPassword());

        user.Update("Novo Nome", null);

        user.Name.Should().Be("Novo Nome");
    }

    [Fact]
    public void Update_WithNullName_KeepsCurrentName()
    {
        var user = new User("Nome Original", ValidEmail(), ValidPassword());

        user.Update(null, null);

        user.Name.Should().Be("Nome Original");
    }

    [Fact]
    public void Update_WithEmptyName_KeepsCurrentName()
    {
        var user = new User("Nome Original", ValidEmail(), ValidPassword());

        user.Update("", null);

        user.Name.Should().Be("Nome Original");
    }

    [Fact]
    public void Update_WithIsActiveFalse_DeactivatesUser()
    {
        var user = new User("João Silva", ValidEmail(), ValidPassword());

        user.Update(null, false);

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Update_WithIsActiveTrue_ActivatesUser()
    {
        var user = new User("João Silva", ValidEmail(), ValidPassword());
        user.Update(null, false);

        user.Update(null, true);

        user.IsActive.Should().BeTrue();
    }
}
