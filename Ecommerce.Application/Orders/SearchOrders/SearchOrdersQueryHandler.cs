using System.Linq.Expressions;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Abstractions.Paging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Orders.SearchOrders;
internal sealed class SearchOrdersQueryHandler(
    IApplicationDbContext dbContext,
    IValidator<SearchOrdersQuery> validator)
    : IQueryHandler<SearchOrdersQuery, PagedList<OrderResponse>>
{
    public async Task<Result<PagedList<OrderResponse>>> Handle(SearchOrdersQuery request, CancellationToken cancellationToken)
    {
        // No decorator validates queries, so the handler runs the validator itself.
        ValidationResult validation = await validator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
        {
            return Result.Failure<PagedList<OrderResponse>>(new ValidationError(validation.Errors.Select(f => Error.BadRequest(f.PropertyName, f.ErrorMessage)).ToArray()));
        }

        // Customers see only their own orders.
        IQueryable<Order> query = dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == new UserId(request.UserId));

        if (request.Status is not null)
        {
            OrderStatus status = Enum.Parse<OrderStatus>(request.Status, ignoreCase: true);
            query = query.Where(order => order.Status == status);
        }

        if (request.FromUtc is not null)
        {
            query = query.Where(order => order.CreatedOnUtc >= request.FromUtc);
        }

        if (request.ToUtc is not null)
        {
            query = query.Where(order => order.CreatedOnUtc <= request.ToUtc);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        // Newest first by default. The ID breaks ties, so a page never repeats or skips an order.
        Expression<Func<Order, object>> sortKey = string.Equals(request.SortColumn, "TotalPrice", StringComparison.OrdinalIgnoreCase)
            ? order => order.TotalPrice.Amount
            : order => order.CreatedOnUtc;

        IOrderedQueryable<Order> sorted = request.SortOrder == "asc"
            ? query.OrderBy(sortKey)
            : query.OrderByDescending(sortKey);

        List<Order> orders = await sorted
            .ThenBy(order => order.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(order => order.OrderItems)
            .ThenInclude(orderItem => orderItem.Product)
            .ToListAsync(cancellationToken);

        IEnumerable<OrderResponse> items = orders.Select(order => new OrderResponse
        {
            Id = order.Id.Value,
            UserId = order.UserId.Value,
            OrderItems = order.OrderItems.Select(oi => new OrderItemResponse
            {
                OrderId = oi.OrderId.Value,
                ProductId = oi.ProductId.Value,
                Name = oi.Product.Name.Value,
                Description = oi.Product.Description.Value,
                PriceAmount = oi.UnitPrice.Amount,
                PriceCurrency = oi.UnitPrice.Currency.Code,
                CategoryId = oi.Product.CategoryId.Value,
                Quantity = oi.Quantity,
                CreatedOnUtc = oi.CreatedOnUtc
            }).ToList(),
            TotalPriceAmount = order.TotalPrice.Amount,
            TotalPriceCurrency = order.TotalPrice.Currency.Code,
            DiscountAmount = order.Discount.Amount,
            Status = order.Status.ToString(),
            CreatedOnUtc = order.CreatedOnUtc,
            CancelledOnUtc = order.CancelledOnUtc
        });

        return PagedList<OrderResponse>.Create(items.ToList(), request.Page, request.PageSize, totalCount);
    }
}
