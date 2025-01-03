namespace Ecommerce.API.Controllers.Users;

public record RegisterUserRequest(string FirstName, string LastName, string Email, string Password);
