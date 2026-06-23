using FCG.UsersAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.UsersAPI.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
        Task<User> ValidateTokenAsync(string token);
    }
}
