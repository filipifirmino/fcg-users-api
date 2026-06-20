using FCG.UsersAPI.Application.DTOs;
using FCG.UsersAPI.Application.Interfaces;
using FCG.UsersAPI.Domain.Common;
using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.Interfaces;
using FCG.UsersAPI.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Authentication;

namespace FCG.UsersAPI.Application.Services
{
    public class AuthService(
    IUserRepository userRepository,
    ILogger<AuthService> logger,
    ITokenService tokenService,
    IConfiguration configuration) : IAuthService
    {
        public async Task<Result<LoginResponseDto>> AuthenticateAsync(LoginRequestDto request)
        {
            try
            {
                logger.LogInformation("Tentativa de autenticação para utilizador: {Email}", request.Email);

                if (!ValidateCredentials(request.Email, request.Password))
                {
                    logger.LogWarning("Credenciais inválidas para utilizador: {Email}", request.Email);
                    return Result<LoginResponseDto>.Failure("Credenciais inválidas.");
                }

                var user = await userRepository.GetUserByEmail(request.Email);
                if (user == null)
                {
                    logger.LogWarning("Utilizador não encontrado: {Email}", request.Email);
                    return Result<LoginResponseDto>.Failure("Credenciais inválidas.");
                }

                var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password.Value, user.Password.Value);

                if (!passwordValid)
                {
                    logger.LogWarning("Senha inválida para usuário: {Email}", request.Email);
                    throw new AuthenticationException("Credenciais inválidas");
                }

                var token = tokenService.GenerateToken(user);
                var expirationMinutes = int.TryParse(configuration["Jwt:expirationMinutes"], out var minutes) ? minutes : 60;

                var response = new LoginResponseDto
                {
                    Token = token,
                    Username = user.Name,
                    ExpiresIn = expirationMinutes * 60,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
                };

                logger.LogInformation("Utilizador autenticado com sucesso: {Email}", request.Email);

                return Result<LoginResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro interno durante autenticação do utilizador: {Email}", request.Email);
                throw new AuthenticationException("Erro interno durante autenticação");
            }
        }

        public bool ValidateCredentials(Email email, Password password)
            => email.IsValid() && password.IsValid();

        public async Task<Result<User>> ValidateTokenAsync(string token)
        {
            try
            {
                var user = await tokenService.ValidateTokenAsync(token);
                if (user == null)
                {
                    logger.LogWarning("Validação de token falhou: Token expirado ou utilizador inexistente.");
                    return Result<User>.Failure("Token inválido");
                }

                return Result<User>.Success(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro durante validação do token");
                throw new AuthenticationException("Token inválido");
            }
        }
    }
}
