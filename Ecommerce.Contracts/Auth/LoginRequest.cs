namespace Ecommerce.Contracts.Auth
{
    public record LoginRequest(
         string Email,
         string Password);
}
