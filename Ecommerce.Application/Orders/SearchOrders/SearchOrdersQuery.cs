using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Abstractions.Paging;

namespace Ecommerce.Application.Orders.SearchOrders;

// Searches the signed-in user's own orders. Status, FromUtc and ToUtc are optional filters.
public record SearchOrdersQuery(
    Guid UserId,
    string? Status,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize,
    string? SortColumn,
    string? SortOrder) : IQuery<PagedList<OrderResponse>>;
