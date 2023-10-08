using Dapper;
using Ecommerce.Data;
using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories.Abstractions.Base;
using Ecommerce.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Ecommerce.Repositories;

public class DapperProductCategoryRepository : IProductCategoryRepository
{
    private readonly EcommerceDapperDbContext _db;
    public DapperProductCategoryRepository(EcommerceDapperDbContext db)
    {
        _db = db;
    }
    public bool Add(ProductCategory productCategory)
    {
        using (var connection = _db.GetConnection())
        {
            int rowsAffected = connection.Execute(
                "INSERT INTO ProductCategories (Name, Code) VALUES (@Name, @Code)",
                productCategory
            );

            return rowsAffected > 0;
        }
    }
 
    public bool Update(ProductCategory productCategory)
    {
        using (var connection = _db.GetConnection())
        {
            int rowsAffected = connection.Execute(
                "UPDATE ProductCategories SET Name = @Name, Code = @Code WHERE ProductCategoryID = @ProductCategoryID",
                productCategory
            );
            
            return rowsAffected > 0;
        }
    }

    public bool Delete(ProductCategory productCategory)
    {
        using (var connection = _db.GetConnection())
        {
            int rowsAffected = connection.Execute(
                "DELETE FROM ProductCategories WHERE ProductCategoryID = @ProductCategoryID",
                productCategory
            );

            return rowsAffected > 0;
        }
    }
    public ProductCategory GetById(int id)
    {
        using (var connection = _db.GetConnection())
        {
            return connection.QueryFirstOrDefault<ProductCategory>("SELECT * FROM ProductCategories WHERE ProductCategoryID = @id", new { id });
        }

    }
    public ICollection<ProductCategory> GetAll()
    {
        using (var connection = _db.GetConnection())
        {
            return connection.Query<ProductCategory>("SELECT * FROM ProductCategories").ToList();
        }
    }

    
    public ICollection<ProductCategory> Search(ProductCategorySearchCriteria searchCriteria)
    {
  
        using (var connection = _db.GetConnection())
        {
            var sql = new StringBuilder("SELECT * FROM ProductCategories");

            var parameters = new DynamicParameters();

            var conditions = new List<string>();

            if (searchCriteria != null)
            {
                if (!string.IsNullOrEmpty(searchCriteria.Name))
                {
                    conditions.Add("LOWER(Name) LIKE @Name");
                    parameters.Add("Name", "%" + searchCriteria.Name.ToLower() + "%");
                }

                if (!string.IsNullOrEmpty(searchCriteria.Code))
                {
                    conditions.Add("LOWER(Code) LIKE @Code");
                    parameters.Add("Code", "%" + searchCriteria.Code.ToLower() + "%");
                }
            }

            if (conditions.Any())
            {
                sql.Append(" WHERE " + string.Join(" AND ", conditions));
            }

            sql.Append(" ORDER BY ProductCategoryID OFFSET @SkipSize ROWS FETCH NEXT @PageSize ROWS ONLY");
            parameters.Add("SkipSize", (searchCriteria.CurrentPage - 1) * searchCriteria.PageSize);
            parameters.Add("PageSize", searchCriteria.PageSize);

            return connection.Query<ProductCategory>(sql.ToString(), parameters).ToList();
        }
    }

    
}

