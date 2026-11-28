namespace Ecommerce.Application.Users;

// Returned by login and refresh. The refresh token appears here once; only its hash is stored.
// The API puts both tokens in HttpOnly cookies; RefreshTokenExpiresOnUtc becomes the refresh cookie's expiry.
public sealed record AuthTokensResponse(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresOnUtc);
