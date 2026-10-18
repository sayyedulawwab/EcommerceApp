using Ecommerce.Domain.Users;

namespace Ecommerce.IntegrationTests.Helpers;

internal static class UserData
{
    public static User Create(string email = "customer@example.com")
    {
        return User.Create(
            new FirstName("Test"),
            new LastName("Customer"),
            new Email(email),
            "passwordHash",
            "passwordSalt",
            isAdmin: false);
    }
}
