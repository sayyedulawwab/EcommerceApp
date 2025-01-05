using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories.Events;

namespace Ecommerce.Domain.Categories;

public sealed class Category : Entity<CategoryId>
{
    private Category(CategoryId id, CategoryName name, CategoryCode code, DateTime createdOnUtc) : base(id)
    {
        Name = name;
        Code = code;
        CreatedOnUtc = createdOnUtc;
    }
    private Category()
    {
    }

    public CategoryName Name { get; private set; }
    public CategoryCode Code { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }


    public static Category Create(CategoryName name, CategoryCode code, DateTime createdOnUtc)
    {
        var category = new Category(CategoryId.New(), name, code, createdOnUtc);

        category.RaiseDomainEvent(new CategoryCreatedDomainEvent(category.Id));

        return category;
    }

    public static Category Update(Category category, CategoryName name, CategoryCode code, DateTime updatedOnUtc)
    {
        category.Name = name;
        category.Code = code;
        category.UpdatedOnUtc = updatedOnUtc;

        return category;
    }
}
