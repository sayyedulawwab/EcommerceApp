namespace Ecommerce.Application.Reviews;
public sealed class ReviewResponse
{
    public Guid ReviewId { get; init; }
    public int Rating { get; init; }
    public string Comment { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
