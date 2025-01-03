using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Users.Register;

public record RegisterUserCommand(string FirstName, string LastName, string Email, string Password) : ICommand<Guid>;
