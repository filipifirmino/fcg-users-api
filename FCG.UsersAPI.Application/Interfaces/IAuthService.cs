using FCG.UsersAPI.Application.DTOs;
using FCG.UsersAPI.Domain.Common;
using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.ValueObjects;

namespace FCG.UsersAPI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<LoginResponseDto>> AuthenticateAsync(LoginRequestDto request);
        Task<Result<User>> ValidateTokenAsync(string token);
        bool ValidateCredentials(Email email, Password password);
    }

}
