namespace Ecommerce.Application.Categories;
public sealed class CategoryResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
}
