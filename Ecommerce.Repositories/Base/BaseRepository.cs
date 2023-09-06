using Ecommerce.Repositories.Abstractions.Base;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Repositories.Base
{
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        DbContext _db;
        public BaseRepository(DbContext db)
        {
            _db = db;
        }

        private DbSet<T> Table
        {
            get
            {
                return _db.Set<T>();
            }
        }
        public bool Add(T entity)
        {
            Table.Add(entity);
            return _db.SaveChanges() > 0;
        }
        public bool Update(T entity)
        {
            Table.Update(entity);
            return _db.SaveChanges() > 0;
        }


        public bool Delete(T entity)
        {
            Table.Remove(entity);
            return _db.SaveChanges() > 0;
        }

        public ICollection<T> GetAll()
        {
            return Table.ToList();
        }

        public ICollection<T> Search(T entity)
        {
            return Table.ToList();
        }

    }
}
