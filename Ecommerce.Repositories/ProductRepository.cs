using Ecommerce.Data;
using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories.Base;

namespace Ecommerce.Repositories;

public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    EcommerceDbContext _db;
    public ProductRepository(EcommerceDbContext db) : base(db)
    {
        _db = db;
    }
    public bool Add(Product product){
        _db.Products.Add(product);
        return _db.SaveChanges() > 0;
    }
  
    public bool Update(Product product){
        _db.Products.Update(product);
        return _db.SaveChanges() > 0;
    }
   
    public bool Delete(Product product){
        _db.Products.Remove(product);
        return _db.SaveChanges() > 0;
    }
    public Product GetById(int id){
        return _db.Products.FirstOrDefault(product => product.ProductID == id);
        
    }
    public ICollection<Product> GetAll()
    {
        return _db.Products.ToList();
    }

    public ICollection<Product> Search(ProductSearchCriteria searchCriteria)
    {
        var products = _db.Products.AsQueryable();

        if (searchCriteria != null && !string.IsNullOrEmpty(searchCriteria.Name))
        {
            products = products.Where(p => p.Name.ToLower().Contains(searchCriteria.Name.ToLower()));
        }

        if (searchCriteria != null && searchCriteria.Price > 0)
        {
            products = products.Where(p => p.Price == searchCriteria.Price);
        }

        if (searchCriteria != null && searchCriteria.ProductCategoryID > 0)
        {
            products = products.Where(p => p.ProductCategoryID == searchCriteria.ProductCategoryID);
        }


        int skipSize = (searchCriteria.CurrentPage - 1) * searchCriteria.PageSize;

        return products.Skip(skipSize).Take(searchCriteria.PageSize).ToList();



    }

}
