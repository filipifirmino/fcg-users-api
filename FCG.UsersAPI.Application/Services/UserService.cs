using FCG.UsersAPI.Application.DTOs;
using FCG.UsersAPI.Application.Exceptions;
using FCG.UsersAPI.Application.Interfaces;
using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.Interfaces;
using FCG.UsersAPI.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FCG.UsersAPI.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly ILogger<UserService> _logger;
        public UserService(IUserRepository repository, ILogger<UserService> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        public async Task<IEnumerable<User>?> GetAll() => await _repository.GetAllAsync();

        public async Task<User?> GetById(Guid id) => await _repository.GetByIdAsync(id);

        public async Task<User?> Update(Guid id, UpdateUserDto request)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user is null) return null;

            user.Update(request.Name, request.IsActive);
            await _repository.UpdateAsync(user);
            return user;
        }

        public async Task<RegisterResultDto?> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                if (!request.IsValid())
                    return new RegisterResultDto(false, null, "Invalid request data");

                _logger.LogInformation("Tentativa de registro para usuário: {Email}", request.Email);

                var existingUser = await _repository.GetUserByEmail(request.Email);

                if (existingUser != null)
                {
                    _logger.LogWarning("Usuário já existe: {Email}", request.Email);
                    return new RegisterResultDto(false, null, "Usuário já cadastrado");
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password.Value);
                var passwordHashVo = new Password(hashedPassword);

                var user = new User(
                    request.Name,
                    request.Email,
                    passwordHashVo
                );

                var createdUser = await _repository.AddAsync(user);

                _logger.LogInformation("Usuário criado com sucesso: {Email}", request.Email);

                var response = new RegisterResponseDto
                {
                    Id = createdUser.Id,
                    Name = createdUser.Name,
                    Email = createdUser.Email.Value,
                    CreatedAt = createdUser.CreatedAt
                };

                return new RegisterResultDto(true, response, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante registro do usuário: {Email}", request.Email);
                throw new RegisterException("Erro interno durante o registro do usuário");
            }
        }

        private bool ValidateCredentials(Email email, Password password)
            => email.IsValid() && password.IsValid();
    }
}
