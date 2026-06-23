using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.UsersAPI.Domain.Interfaces
{
    public interface IUserRepository : IRepositoryBase<User>
    {
        Task<User?> GetUserByEmail(Email email, CancellationToken cancellationToken = default);
    }
}
