using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Users.Refresh;

public record RefreshAccessTokenCommand(string RefreshToken) : ICommand<AuthTokensResponse>;
