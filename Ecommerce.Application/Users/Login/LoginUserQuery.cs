using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Users.Login;

public record LoginUserQuery(string email, string password) : IQuery<AccessTokenResponse>;