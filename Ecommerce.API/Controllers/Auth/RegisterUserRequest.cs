namespace Ecommerce.API.Controllers.Auth;

public record RegisterUserRequest(string FirstName, string LastName, string Email, string Password);
