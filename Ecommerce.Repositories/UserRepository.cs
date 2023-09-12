using Ecommerce.Data;
using Ecommerce.Models.EntityModels;
using Ecommerce.Repositories.Abstractions;

namespace Ecommerce.Repositories
{
    public class UserRepository : IUserRepository
    {
        EcommerceEFDbContext _db;
        public UserRepository(EcommerceEFDbContext db) 
        {
            _db = db;
        }

        public ICollection<User> GetAll()
        {
            return _db.Users.ToList();
        }

        public User GetByUsername(string username)
        {
            if (username != null && !string.IsNullOrEmpty(username))
            {
                var user = _db.Users.FirstOrDefault(u => u.Username.ToLower().Equals(username.ToLower()));
                
                return user;

            }

            return null;
        }

        public User GetByEmail(string email)
        {
            if (email != null && !string.IsNullOrEmpty(email))
            {
                var user = _db.Users.FirstOrDefault(u => u.Email.ToLower().Equals(email.ToLower()));

                return user;

            }

            return null;
        }

        public User GetByUsernameOrEmail(string usernameOrEmail)
        {
            if (usernameOrEmail != null && !string.IsNullOrEmpty(usernameOrEmail))
            {
                var user = _db.Users.FirstOrDefault(u => u.Username.ToLower().Equals(usernameOrEmail.ToLower()) || u.Email.ToLower().Equals(usernameOrEmail.ToLower()));

                return user;

            }

            return null;
        }


        public bool Add(User user)
        {
            _db.Users.Add(user);
            return _db.SaveChanges() > 0;
        }

        public bool Update(User user)
        {
            _db.Users.Update(user);
            return _db.SaveChanges() > 0;
        }
        public bool Delete(User user)
        {
            _db.Users.Remove(user);
            return _db.SaveChanges() > 0;
        }

        

        
    }
}
