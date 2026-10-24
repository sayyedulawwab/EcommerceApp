using Ecommerce.Domain.Orders;
using FluentValidation;

namespace Ecommerce.Application.Orders.SearchOrders;
public class SearchOrdersQueryValidator : AbstractValidator<SearchOrdersQuery>
{
    public const int MaxPageSize = 100;

    // The only columns an order list can be sorted by.
    public static readonly string[] SortColumns = ["CreatedOnUtc", "TotalPrice"];

    public SearchOrdersQueryValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(q => q.Status)
            .Must(status => Enum.TryParse(status, ignoreCase: true, out OrderStatus parsed) && Enum.IsDefined(parsed))
            .When(q => q.Status is not null)
            .WithMessage("Status must be one of: " + string.Join(", ", Enum.GetNames<OrderStatus>()));
        RuleFor(q => q.ToUtc).GreaterThanOrEqualTo(q => q.FromUtc).When(q => q.FromUtc is not null && q.ToUtc is not null);
        RuleFor(q => q.SortColumn)
            .Must(column => SortColumns.Contains(column, StringComparer.OrdinalIgnoreCase))
            .When(q => q.SortColumn is not null)
            .WithMessage("SortColumn must be one of: " + string.Join(", ", SortColumns));
        RuleFor(q => q.SortOrder)
            .Must(order => order is "asc" or "desc")
            .When(q => q.SortOrder is not null)
            .WithMessage("SortOrder must be asc or desc");
    }
}
