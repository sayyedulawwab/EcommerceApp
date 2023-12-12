using Ecommerce.Application.Common.Interfaces.Services;

namespace Ecommerce.Infrastructure.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
