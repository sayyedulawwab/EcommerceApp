using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products.Events;
using Ecommerce.Domain.Shared;

namespace Ecommerce.Domain.Products;

public sealed class Product : Entity<ProductId>
{
    private Product(ProductId id, ProductName name, ProductDescription description, Money price, int quantity, CategoryId categoryId, DateTime createdOnUtc) : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        Quantity = quantity;
        CategoryId = categoryId;
        CreatedOnUtc = createdOnUtc;
    }

    #pragma warning disable CS8618 // Used by EF Core to materialize the entity
    private Product()
    {
    }
    #pragma warning restore CS8618

    public CategoryId CategoryId { get; private set; }
    public ProductName Name { get; private set; }
    public ProductDescription Description { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static Product Create(ProductName name, ProductDescription description, Money price, int quantity, CategoryId categoryId, DateTime createdOnUtc)
    {
        var product = new Product(ProductId.New(), name, description, price, quantity, categoryId, createdOnUtc);

        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id));

        return product;
    }

    public static Product Update(Product product, ProductName name, ProductDescription description, Money price, int quantity, CategoryId categoryId, DateTime updatedOnUtc)
    {
        product.Name = name;
        product.Description = description;
        product.Price = price;
        product.Quantity = quantity;
        product.CategoryId = categoryId;
        product.UpdatedOnUtc = updatedOnUtc;

        return product;
    }

    public Result RemoveStock(int quantity, DateTime updatedOnUtc)
    {
        // A negative quantity would add stock instead of removing it.
        if (quantity <= 0)
        {
            return Result.Failure(ProductErrors.InvalidStockQuantity);
        }

        if (quantity > Quantity)
        {
            return Result.Failure(ProductErrors.InsufficientStock);
        }

        Quantity -= quantity;
        UpdatedOnUtc = updatedOnUtc;

        return Result.Success();
    }
}
