﻿namespace Ecommerce.Domain.Abstractions;
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
