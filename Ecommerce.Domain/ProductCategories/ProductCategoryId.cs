namespace Ecommerce.Domain.ProductCategories;

public record ProductCategoryId(Guid Value)
{
    public static ProductCategoryId New() => new(Guid.NewGuid());
}
