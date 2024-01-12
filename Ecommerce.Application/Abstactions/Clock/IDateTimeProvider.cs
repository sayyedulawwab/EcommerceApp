namespace Ecommerce.Application.Abstactions.Clock;
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
