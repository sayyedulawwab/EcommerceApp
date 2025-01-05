using Ecommerce.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

internal abstract class Repository<TEntity, TEntityId>(ApplicationDbContext dbContext)
    where TEntity : Entity<TEntityId>
    where TEntityId : class
{
    protected readonly ApplicationDbContext DbContext = dbContext;

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<TEntity>().ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(
        TEntityId id,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<TEntity>()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public void Add(TEntity entity)
    {
        DbContext.Add(entity);
    }

    public void Remove(TEntity entity)
    {
        DbContext.Remove(entity);
    }

    public void Update(TEntity entity)
    {
        DbContext.Update(entity);
    }
}
