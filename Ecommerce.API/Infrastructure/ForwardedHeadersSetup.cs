using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Ecommerce.API.Infrastructure;

// The API sits behind nginx, which sets X-Forwarded-For to the address it saw. Those headers are only
// believed when the request comes from a proxy listed in ForwardedHeaders:KnownProxies or
// ForwardedHeaders:KnownNetworks (CIDR, e.g. 172.16.0.0/12). With nothing listed, nobody is trusted, so a client
// can't pick its own IP (and its own rate-limit bucket) by sending the header itself.
internal static class ForwardedHeadersSetup
{
    public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Use only the entry the trusted proxy added, never the ones a client may have put in front of it.
            options.ForwardLimit = 1;

            // The defaults trust loopback. Trust exactly what the configuration lists.
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (string proxy in (configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? []).Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (string network in (configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? []).Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }

            // Empty lists would make the middleware trust every sender, so with nothing configured it is turned off.
            if (options.KnownProxies.Count == 0 && options.KnownIPNetworks.Count == 0)
            {
                options.ForwardedHeaders = ForwardedHeaders.None;
            }
        });

        return services;
    }
}
