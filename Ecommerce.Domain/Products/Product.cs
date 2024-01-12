using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products.Events;

namespace Ecommerce.Domain.Products;

public sealed class Product : Entity
{
    private Product(Guid id, ProductName name, ProductDescription description, Money price, int quantity, Guid productCategoryId) : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        Quantity = quantity;
        ProductCategoryId = productCategoryId;
    }

    public Guid ProductCategoryId { get; private set; }
    public ProductName Name { get; private set; }
    public ProductDescription Description { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }

    public static Product Create(ProductName name, ProductDescription description, Money price, int quantity, Guid productCategoryId)
    {
        var product = new Product(Guid.NewGuid(), name, description, price, quantity, productCategoryId);

        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id));

        return product;
    }

}
