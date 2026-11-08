using FluentValidation;

namespace Ecommerce.Application.Products.SearchProduct;
public class SearchProductQueryValidator : AbstractValidator<SearchProductQuery>
{
    public const int MaxPageSize = 100;

    // The only columns a product list can be sorted by.
    public static readonly string[] SortColumns = ["Name", "PriceAmount", "CreatedOnUtc"];

    public SearchProductQueryValidator()
    {
        // A page or page size below 1 is not an error: the handler falls back to the defaults.
        RuleFor(q => q.PageSize).LessThanOrEqualTo(MaxPageSize);
        RuleFor(q => q.SortColumn)
            .Must(column => SortColumns.Contains(column))
            .When(q => q.SortColumn is not null)
            .WithMessage("SortColumn must be one of: " + string.Join(", ", SortColumns));
    }
}
