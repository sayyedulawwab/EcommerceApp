using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Users.Login;

public record LoginUserQuery(string Email, string Password) : IQuery<AccessTokenResponse>;
