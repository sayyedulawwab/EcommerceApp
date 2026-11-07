namespace Ecommerce.Application.Exceptions;

// A write broke a unique index. Handlers that pre-check for duplicates catch it for the case where
// two requests pass the check at the same moment, and turn it into the same domain error.
public sealed class UniqueConstraintException : Exception
{
    public UniqueConstraintException()
    {
    }

    public UniqueConstraintException(string message)
        : base(message)
    {
    }

    public UniqueConstraintException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string? ConstraintName { get; init; }
}
