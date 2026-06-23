using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.Interfaces;
using FCG.UsersAPI.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.UsersAPI.Infra.Repositories
{
    public class UserRepository(AppDbContext context) : RepositoryBase<User>(context), IUserRepository
    {
        public Task<User?> GetUserByEmail(Email email, CancellationToken cancellationToken = default)
            => context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }
}
