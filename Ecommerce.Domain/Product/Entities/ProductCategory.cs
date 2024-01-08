using Ecommerce.Domain.Common.Models;
using Ecommerce.Domain.Product.ValueObjects;

namespace Ecommerce.Domain.Product.Entities
{
    public sealed class ProductCategory : Entity<ProductCategoryId>
    {
        public string Name { get; }
        public string Code { get; }

        public ProductCategory(ProductCategoryId productCategoryId, string name, string code) : base(productCategoryId)
        {
            Name = name;
            Code = code;
        }
    }
}
