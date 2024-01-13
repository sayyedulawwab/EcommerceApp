using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories;
using Ecommerce.Domain.Products.Events;
using System;

namespace Ecommerce.Domain.Products;

public sealed class Product : Entity
{
    private Product(Guid id, ProductName name, ProductDescription description, Money price, int quantity, Guid productCategoryId, DateTime createdOn) : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        Quantity = quantity;
        ProductCategoryId = productCategoryId;
        CreatedOn = createdOn;
    }

    public Guid ProductCategoryId { get; private set; }
    public ProductName Name { get; private set; }
    public ProductDescription Description { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    public static Product Create(ProductName name, ProductDescription description, Money price, int quantity, Guid productCategoryId, DateTime createdOn)
    {
        var product = new Product(Guid.NewGuid(), name, description, price, quantity, productCategoryId, createdOn);

        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id));

        return product;
    }


    public static Product Update(Product product, ProductName name, ProductDescription description, Money price, int quantity, Guid productCategoryId, DateTime updatedOn)
    {

        product.Name = name;
        product.Description = description;
        product.Price = price;
        product.Quantity = quantity;
        product.ProductCategoryId = productCategoryId;
        product.UpdatedOn = updatedOn;

        return product;
    }

}
