using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests;

internal static class ValidationAssert
{
    // Each test changes one field of a valid command, so exactly one error is expected, on that field.
    public static void HasSingleError(ValidationResult result, string propertyName)
    {
        ValidationFailure error = Assert.Single(result.Errors);
        Assert.Equal(propertyName, error.PropertyName);
    }
}
