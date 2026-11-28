using Microsoft.Net.Http.Headers;

namespace Ecommerce.IntegrationTests.Helpers;

// Reads the Set-Cookie headers of a response, the way a browser would see them.
internal static class SetCookies
{
    public static SetCookieHeaderValue? Find(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string>? values))
        {
            return null;
        }

        return SetCookieHeaderValue.ParseList(values.ToList())
            .LastOrDefault(cookie => cookie.Name.Equals(name, StringComparison.Ordinal));
    }

    public static string GetValue(HttpResponseMessage response, string name)
    {
        SetCookieHeaderValue cookie = Find(response, name)
            ?? throw new InvalidOperationException($"The response sets no {name} cookie.");

        return cookie.Value.ToString();
    }
}
