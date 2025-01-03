using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Users;
public static class UserErrors
{
    public static readonly Error AlreadyExists = new(
        "User.AlreadyExists",
        "User with provided email already exists");

    public static readonly Error NotFound = new(
        "User.Found",
        "The user with the specified identifier was not found");

    public static readonly Error InvalidCredentials = new(
        "User.InvalidCredentials",
        "The provided credentials were invalid");

}
