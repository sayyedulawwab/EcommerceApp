using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class UserRepository : Repository<User, UserId>, IUserRepository
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<User?> GetByEmail(string email)
    {
        var user = await DbContext.Set<User>().FirstOrDefaultAsync(u => u.Email.Value.ToLower().Equals(email.ToLower()));

        return user;
        
    }
   
}
