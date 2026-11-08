using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Users.Login;

public record LoginUserCommand(string Email, string Password) : ICommand<AuthTokensResponse>;
