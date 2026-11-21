namespace Ecommerce.Application.Abstractions.Auth;

// Role names carried in the access token and checked by [Authorize(Roles = ...)].
public static class Roles
{
    public const string Admin = "Admin";
}
