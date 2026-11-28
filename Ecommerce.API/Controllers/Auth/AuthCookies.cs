using Ecommerce.Application.Users;
using Ecommerce.Infrastructure.Auth;

namespace Ecommerce.API.Controllers.Auth;

internal static class AuthCookies
{
    public static void Append(HttpResponse response, AuthTokensResponse tokens)
    {
        // No expiry: a session cookie. The JWT inside expires after 15 minutes anyway, and then the
        // frontend calls refresh, which sends the longer-lived refresh cookie.
        response.Cookies.Append(AuthCookieNames.AccessToken, tokens.AccessToken, CreateOptions("/"));

        CookieOptions refreshOptions = CreateOptions(AuthCookieNames.RefreshTokenPath);
        refreshOptions.Expires = tokens.RefreshTokenExpiresOnUtc;

        response.Cookies.Append(AuthCookieNames.RefreshToken, tokens.RefreshToken, refreshOptions);
    }

    public static void Delete(HttpResponse response)
    {
        // A cookie is only removed when the name, path and prefix rules match the ones it was set with.
        response.Cookies.Delete(AuthCookieNames.AccessToken, CreateOptions("/"));
        response.Cookies.Delete(AuthCookieNames.RefreshToken, CreateOptions(AuthCookieNames.RefreshTokenPath));
    }

    private static CookieOptions CreateOptions(string path)
    {
        return new CookieOptions
        {
            // Page scripts can't read the cookie, so an XSS payload can't steal the token.
            HttpOnly = true,
            // HTTPS only. Browsers treat http://localhost as secure, so local development still works.
            Secure = true,
            // Never sent on a request that starts on another site: the first defense against CSRF.
            SameSite = SameSiteMode.Strict,
            Path = path,
            IsEssential = true
        };
    }
}
