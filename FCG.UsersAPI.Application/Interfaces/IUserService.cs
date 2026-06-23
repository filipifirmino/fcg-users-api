using FCG.UsersAPI.Application.DTOs;
using FCG.UsersAPI.Domain.Entities;

namespace FCG.UsersAPI.Application.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>?> GetAll();
        Task<User?> GetById(Guid id);
        Task<RegisterResultDto?> RegisterAsync(RegisterRequestDto request);
        Task<User?> Update(Guid id, UpdateUserDto request);
    }
}
