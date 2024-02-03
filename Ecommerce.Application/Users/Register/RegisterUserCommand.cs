using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Users.Register;

public record RegisterUserCommand(string firstName, string lastName, string email, string password) : ICommand<Guid>;
