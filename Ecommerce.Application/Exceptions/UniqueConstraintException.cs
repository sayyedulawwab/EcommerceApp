using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Exceptions;

// A write broke a unique index. It is a DbUpdateException, so the handlers that already catch that for
// their duplicate checks keep working. The register handler catches this narrower type, so only a real
// duplicate email becomes UserErrors.AlreadyExists, and any other failed write stays a 500.
public sealed class UniqueConstraintException : DbUpdateException
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

    public UniqueConstraintException(string message, Exception innerException, string? constraintName)
        : base(message, innerException)
    {
        ConstraintName = constraintName;
    }

    public string? ConstraintName { get; }
}
