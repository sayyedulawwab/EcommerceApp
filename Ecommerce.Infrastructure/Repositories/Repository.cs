using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

internal abstract class Repository<T>
    where T : Entity
{
    protected readonly ApplicationDbContext DbContext;

    protected Repository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<IReadOnlyList<T?>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<T>().ToListAsync(cancellationToken);
    }

    public async Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<T>()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public void Add(T entity)
    {
        DbContext.Add(entity);
    }

    public void Remove(T entity)
    {
        DbContext.Remove(entity);
    }

    public void Update(T entity)
    {
        DbContext.Update(entity);
    }
}
