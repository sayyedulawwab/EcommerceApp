namespace Ecommerce.Infrastructure.Auth;

// The API is built for a browser frontend, so both tokens travel only in HttpOnly cookies: page scripts,
// and so an XSS payload, can't read them. The prefixes make the browser enforce the settings:
// __Host- requires Secure, Path=/ and no Domain; __Secure- requires Secure.
public static class AuthCookieNames
{
    public const string AccessToken = "__Host-access_token";

    // Not __Host-: that prefix requires Path=/, and this cookie is sent only to the auth endpoints.
    public const string RefreshToken = "__Secure-refresh_token";

    public const string RefreshTokenPath = "/api/v1/auth";
}
