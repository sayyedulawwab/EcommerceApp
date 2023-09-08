using Dapper;
using Ecommerce.Data;
using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Repositories
{
    public class DapperProductRepository : IProductRepository
    {
        EcommerceDapperDbContext _db;
        public DapperProductRepository(EcommerceDapperDbContext db)
        {
            _db = db;
        }
        public bool Add(Product product)
        {
            using (var connection = _db.GetConnection())
            {
                int rowsAffected = connection.Execute(
                    "INSERT INTO Products (Name, Price, Quantity, ProductCategoryID) VALUES (@Name, @Price, @Quantity, @ProductCategoryID)",
                    product
                );

                return rowsAffected > 0;
            }
        }

        public bool Update(Product product)
        {
            using (var connection = _db.GetConnection())
            {
                int rowsAffected = connection.Execute(
                    "UPDATE Products SET Name = @Name, Price = @Price,  Quantity = @Quantity,  ProductCategoryID = @ProductCategoryID WHERE ProductID = @ProductID",
                    product
                );

                return rowsAffected > 0;
            }
        }

        public bool Delete(Product product)
        {
            using (var connection = _db.GetConnection())
            {
                int rowsAffected = connection.Execute(
                    "DELETE FROM Products WHERE ProductID = @ProductID",
                    product
                );

                return rowsAffected > 0;
            }
        }
        public Product GetById(int id)
        {
            using (var connection = _db.GetConnection())
            {
                return connection.QueryFirstOrDefault<Product>("SELECT * FROM Products WHERE ProductID = @id", new { id });
            }

        }
        public ICollection<Product> GetAll()
        {
            using (var connection = _db.GetConnection())
            {
                return connection.Query<Product>("SELECT * FROM Products").ToList();
            }
        }

        public ICollection<Product> Search(ProductSearchCriteria searchCriteria)
        {
            using (var connection = _db.GetConnection())
            {
                var sql = new StringBuilder(
                    @"SELECT P.*, PC.* 
              FROM Products P 
              LEFT JOIN ProductCategories PC ON PC.ProductCategoryID = P.ProductCategoryID");

                var parameters = new DynamicParameters();
                var conditions = new List<string>();

                if (searchCriteria != null)
                {
                    if (!string.IsNullOrEmpty(searchCriteria.Name))
                    {
                        conditions.Add("LOWER(P.Name) LIKE @Name");
                        parameters.Add("Name", "%" + searchCriteria.Name.ToLower() + "%");
                    }

                    if (searchCriteria.Price > 0)
                    {
                        conditions.Add("P.Price = @Price");
                        parameters.Add("Price", searchCriteria.Price);
                    }

                    if (searchCriteria.ProductCategoryID > 0)
                    {
                        conditions.Add("P.ProductCategoryID = @ProductCategoryID");
                        parameters.Add("ProductCategoryID", searchCriteria.ProductCategoryID);
                    }
                }

                if (conditions.Any())
                {
                    sql.Append(" WHERE " + string.Join(" AND ", conditions));
                }

                sql.Append(" ORDER BY P.ProductID OFFSET @SkipSize ROWS FETCH NEXT @PageSize ROWS ONLY");
                parameters.Add("SkipSize", (searchCriteria.CurrentPage - 1) * searchCriteria.PageSize);
                parameters.Add("PageSize", searchCriteria.PageSize);

                var productList = connection.Query<Product, ProductCategory, Product>(
                    sql.ToString(),
                    (product, productCategory) =>
                    {
                        product.ProductCategory = productCategory;
                        return product;
                    },
                    parameters, splitOn: "ProductCategoryID").ToList();

                return productList;
            }
        }

    }
}
