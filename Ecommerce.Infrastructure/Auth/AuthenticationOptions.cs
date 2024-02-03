namespace Ecommerce.Infrastructure.Auth;
public sealed class AuthenticationOptions
{
    public string Audience { get; init; } = string.Empty;

    public string MetadataUrl { get; init; } = string.Empty;

    public bool RequireHttpsMetadata { get; init; }

    public string Issuer { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;
}
