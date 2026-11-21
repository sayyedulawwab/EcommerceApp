using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Reviews;
public record Comment
{
    public static readonly Error Invalid = new("Comment.Invalid", "The comment is required", HttpStatusCode.BadRequest);

    private Comment(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Comment> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Comment>(Invalid);
        }

        return new Comment(value);
    }
}
