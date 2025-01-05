namespace Ecommerce.API.Controllers.Users.Register;

public record RegisterUserRequest(string FirstName, string LastName, string Email, string Password);
