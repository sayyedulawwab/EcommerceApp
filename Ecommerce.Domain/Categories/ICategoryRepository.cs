namespace Ecommerce.Domain.Categories;
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default);
    Task<List<CategoryId>> GetCategoryGuidsAsync();
    void Add(Category productCategory);
    void Update(Category productCategory);
    void Remove(Category productCategory);
}
