using ErrorOr;

namespace Ecommerce.Domain.Common.Errors
{
    public static partial class Errors
    {
        public static class Auth
        {
            public static Error InvalidCredentials => Error.Validation(code: "Auth.InvalidCred", description: "Invalid Credentials.");
        }
    }
}
