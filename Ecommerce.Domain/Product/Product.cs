using Ecommerce.Domain.Common.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Product.ValueObjects;

namespace Ecommerce.Domain.Product
{
    public sealed class Product : AggregateRoot<ProductId>
    {
        public string Name { get; }
        public string Description { get; }
        public decimal UnitPrice { get; }
        public int UnitsInStock { get; }
        public ProductCategoryId ProductCategoryId { get; }

        public DateTime CreatedDateTime { get; }
        public DateTime UpdatedDateTime { get; }

        private Product(ProductId productId, string name, string description, decimal unitPrice, int unitsInStock, ProductCategoryId productCategoryId, DateTime createdDateTime, DateTime updatedDateTime) : base(productId) 
        {
            Name = name;
            Description = description;
            UnitPrice = unitPrice;
            UnitsInStock = unitsInStock;
            ProductCategoryId = productCategoryId;
            CreatedDateTime = createdDateTime;
            UpdatedDateTime = updatedDateTime;
        }

        public static Product Create(string name, string description, decimal unitPrice, int unitsInStock, ProductCategoryId productCategoryId, DateTime createdDateTime, DateTime updatedDateTime)
        {
            return new(ProductId.CreateUnique(), name, description, unitPrice, unitsInStock, productCategoryId, createdDateTime, updatedDateTime);
        }
    }
}
