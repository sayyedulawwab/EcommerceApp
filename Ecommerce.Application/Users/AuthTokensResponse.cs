namespace Ecommerce.Application.Users;

// Returned by login and refresh. The refresh token appears here once; only its hash is stored.
public sealed record AuthTokensResponse(string AccessToken, string RefreshToken);
