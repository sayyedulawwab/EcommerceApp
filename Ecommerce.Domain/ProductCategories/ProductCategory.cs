using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories.Events;

namespace Ecommerce.Domain.ProductCategories;

public sealed class ProductCategory : Entity
{
    private ProductCategory(Guid id, CategoryName name, CategoryCode code, DateTime createdOn) : base(id)
    {
        Name = name;
        Code = code;
        CreatedOn = createdOn;
    }

    public CategoryName Name { get; private set; }
    public CategoryCode Code { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }


    public static ProductCategory Create(CategoryName name, CategoryCode code, DateTime createdOn)
    {
        var productCategory = new ProductCategory(Guid.NewGuid(), name, code, createdOn);

        productCategory.RaiseDomainEvent(new ProductCategoryCreatedDomainEvent(productCategory.Id));

        return productCategory;
    }

}
