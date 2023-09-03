using Ecommerce.Data;
using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories.Base;

namespace Ecommerce.Repositories;

public class ProductCategoryRepository : BaseRepository<ProductCategory>, IProductCategoryRepository
{
    EcommerceDbContext _db;
    public ProductCategoryRepository(EcommerceDbContext db) : base(db)
    {
        _db = db;
    }
    public bool Add(ProductCategory productCategory){
        _db.ProductCategories.Add(productCategory);
        return _db.SaveChanges() > 0;
    }
 
    public bool Update(ProductCategory productCategory){
        _db.ProductCategories.Update(productCategory);
        return _db.SaveChanges() > 0;
    }

    public bool Delete(ProductCategory productCategory){
        _db.ProductCategories.Remove(productCategory);
        return _db.SaveChanges() > 0;
    }
    public ProductCategory GetById(int id){
        return _db.ProductCategories.FirstOrDefault(productCategory => productCategory.ProductCategoryID == id);
        
    }
    public ICollection<ProductCategory> GetAll()
    {
        return _db.ProductCategories.ToList();
    }

    public ICollection<ProductCategory> Search(ProductCategorySearchCriteria searchCriteria)
    {
        var productCategories = _db.ProductCategories.AsQueryable();

        if (searchCriteria != null && !string.IsNullOrEmpty(searchCriteria.Name))
        {
            productCategories = productCategories.Where(p => p.Name.ToLower().Contains(searchCriteria.Name.ToLower()));
        }

        if (searchCriteria != null && !string.IsNullOrEmpty(searchCriteria.Code))
        {
            productCategories = productCategories.Where(p => p.Code.ToLower().Contains(searchCriteria.Code.ToLower()));
        }


        int skipSize = (searchCriteria.CurrentPage - 1) * searchCriteria.PageSize;

        return productCategories.Skip(skipSize).Take(searchCriteria.PageSize).ToList();



    }
}
