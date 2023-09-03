namespace Ecommerce.Services.Abstractions.Base
{
    public interface IService<T> where T : class
    {
        bool Add(T entity);
        bool Update(T entity);
        bool Delete(T entity);
        ICollection<T> GetAll();
    }
}