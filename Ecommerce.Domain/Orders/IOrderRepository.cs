﻿using Ecommerce.Domain.Products;

namespace Ecommerce.Domain.Orders;
public interface IOrderRepository
{
    Task<IReadOnlyList<Order?>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(Order order);
}
