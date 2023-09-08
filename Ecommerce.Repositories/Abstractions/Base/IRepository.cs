using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Repositories.Abstractions.Base
{
    public interface IRepository<T> where T : class
    {
        bool Add(T entity);
        bool Update(T entity);
        bool Delete(T entity);
        ICollection<T> GetAll();

    }
}
