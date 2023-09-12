using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories.Abstractions.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Repositories.Abstractions
{
    public interface IUserRepository : IRepository<User>
    {
        bool Add(User user);
        bool Update(User user);
        bool Delete(User user);
        User GetByUsername(string username);
        User GetByEmail(string email);
        User GetByUsernameOrEmail(string usernameOrEmail);
    }
}
