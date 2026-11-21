namespace Ecommerce.Application.Exceptions;

public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException()
    {
    }

    public ConcurrencyException(string message)
        : base(message)
    {
    }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
