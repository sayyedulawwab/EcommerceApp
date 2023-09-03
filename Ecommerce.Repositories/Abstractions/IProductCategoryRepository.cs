using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories.Abstractions.Base;

namespace Ecommerce.Repositories;

public interface IProductCategoryRepository : IRepository<ProductCategory>
{
    bool Add(ProductCategory productCategory);
    bool Update(ProductCategory productCategory);
    bool Delete(ProductCategory productCategory);
    ProductCategory GetById(int id);
    ICollection<ProductCategory> GetAll();
    ICollection<ProductCategory> Search(ProductCategorySearchCriteria searchCriteria);



}
