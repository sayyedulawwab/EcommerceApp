using Ecommerce.Application.Abstractions.Clock;

namespace Ecommerce.Infrastructure.Clock;
internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
