using FCG.UsersAPI.Domain.Enums;
using FCG.UsersAPI.Domain.ValueObjects;

namespace FCG.UsersAPI.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Email Email { get; private set; }
        public Password Password { get; private set; }
        public UserRole Role { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private User() { }

        public void Update(string? name, bool? isActive)
        {
            if (!string.IsNullOrWhiteSpace(name)) Name = name;
            if (isActive.HasValue) IsActive = isActive.Value;
        }

        public User(string name, Email email, Password password)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Nome é obrigatório");

            Id = Guid.NewGuid();
            Name = name;
            Email = email;
            Password = password;
            Role = UserRole.User;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
