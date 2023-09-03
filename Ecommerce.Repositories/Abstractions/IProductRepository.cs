
using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories.Abstractions.Base;

namespace Ecommerce.Repositories;

public interface IProductRepository : IRepository<Product> 
{
    bool Add(Product product);
    bool Update(Product product);
    bool Delete(Product product);
    Product GetById(int id);
    ICollection<Product> GetAll();
    ICollection<Product> Search(ProductSearchCriteria searchCriteria);
}
