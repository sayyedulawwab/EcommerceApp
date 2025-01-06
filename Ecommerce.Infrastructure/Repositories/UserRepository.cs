using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class UserRepository(ApplicationDbContext dbContext)
    : Repository<User, UserId>(dbContext), IUserRepository
{
    public async Task<User?> GetByEmail(string email)
    {

        User? user = await DbContext.Set<User>()
                        .FromSqlInterpolated($"SELECT * FROM Users WHERE Email = {email}")
                        .FirstOrDefaultAsync();

        return user;

    }

}
