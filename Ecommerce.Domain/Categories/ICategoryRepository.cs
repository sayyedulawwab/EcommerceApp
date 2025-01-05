namespace Ecommerce.Domain.Categories;
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default);
    void Add(Category category);
    void Update(Category category);
    void Remove(Category category);
}
