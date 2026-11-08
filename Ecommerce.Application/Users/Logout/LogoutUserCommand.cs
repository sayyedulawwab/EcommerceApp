using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Users.Logout;

public record LogoutUserCommand(string RefreshToken) : ICommand;
