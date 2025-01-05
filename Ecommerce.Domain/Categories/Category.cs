using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories.Events;

namespace Ecommerce.Domain.Categories;

public sealed class Category : Entity<CategoryId>
{
    private Category(CategoryId id, CategoryName name, CategoryCode code, DateTime createdOn) : base(id)
    {
        Name = name;
        Code = code;
        CreatedOn = createdOn;
    }
    private Category()
    {
    }

    public CategoryName Name { get; private set; }
    public CategoryCode Code { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }


    public static Category Create(CategoryName name, CategoryCode code, DateTime createdOn)
    {
        var productCategory = new Category(CategoryId.New(), name, code, createdOn);

        productCategory.RaiseDomainEvent(new CategoryCreatedDomainEvent(productCategory.Id));

        return productCategory;
    }

    public static Category Update(Category productCategory, CategoryName name, CategoryCode code, DateTime updatedOn)
    {
        productCategory.Name = name;
        productCategory.Code = code;
        productCategory.UpdatedOn = updatedOn;

        return productCategory;
    }
}
