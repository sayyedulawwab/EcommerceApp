using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Infrastructure.Auth;
public sealed class PasswordHashingOptions
{
    // bcrypt cost: each +1 doubles the work. 10 is the lowest accepted value and 12 is the default.
    [Range(10, 16)]
    public int WorkFactor { get; init; } = 12;
}
