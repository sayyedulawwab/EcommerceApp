using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ecommerce.Repositories.Abstractions.Base;
using Ecommerce.Services.Abstractions.Base;

namespace Ecommerce.Services.Base
{
    public class BaseService<T> : IService<T> where T : class
    {
        IRepository<T> _repostiory;

        public BaseService(IRepository<T> repository)
        {
            _repostiory = repository;
        }
        public virtual bool Add(T entity)
        {
            return _repostiory.Add(entity);
        }

        public virtual bool Delete(T entity)
        {
            return _repostiory.Delete(entity);
        }

        public virtual ICollection<T> GetAll()
        {
            return _repostiory.GetAll();
        }

        public virtual bool Update(T entity)
        {
            return _repostiory.Update(entity);
        }

       
    }
}
