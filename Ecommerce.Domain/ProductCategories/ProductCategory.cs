using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products.Events;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.ProductCategories.Events;

namespace Ecommerce.Domain.ProductCategories;

public sealed class ProductCategory : Entity
{
    private ProductCategory(Guid id, string name, string code) : base(id)
    {
        Name = name;
        Code = code;

    }

    public string Name { get; private set; }
    public string Code { get; private set; }


    public static ProductCategory Create(string name, string code)
    {
        var productCategory = new ProductCategory(Guid.NewGuid(), name, code);

        productCategory.RaiseDomainEvent(new ProductCategoryCreatedDomainEvent(productCategory.Id));

        return productCategory;
    }

}
