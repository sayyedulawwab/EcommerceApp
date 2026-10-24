using Ecommerce.Domain.Users;

namespace Ecommerce.IntegrationTests.Helpers;

internal static class UserData
{
    public static User Create(string email = "customer@example.com")
    {
        return User.Create(
            FirstName.Create("Test").Value,
            LastName.Create("Customer").Value,
            Email.Create(email).Value,
            "passwordHash",
            "passwordSalt",
            isAdmin: false);
    }
}
