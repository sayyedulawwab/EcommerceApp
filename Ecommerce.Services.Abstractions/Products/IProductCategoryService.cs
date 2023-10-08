using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Services.Abstractions.Base;


namespace Ecommerce.Services.Abstractions.Products
{
    public interface IProductCategoryService : IService<ProductCategory>
    {
        bool Add(ProductCategory productCategory);
        bool Update(ProductCategory productCategory);
        bool Delete(ProductCategory productCategory);
        ICollection<ProductCategory> GetAll();
        ProductCategory GetById(int id);
        ICollection<ProductCategory> Search(ProductCategorySearchCriteria searchCriteria);

    }
}
