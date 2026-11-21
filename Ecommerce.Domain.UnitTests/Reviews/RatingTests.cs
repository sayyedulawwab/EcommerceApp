using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Reviews;

namespace Ecommerce.Domain.UnitTests.Reviews;

public class RatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Create_Should_ReturnRating_WhenValueIsInRange(int value)
    {
        Result<Rating> result = Rating.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_Should_ReturnInvalid_WhenValueIsOutOfRange(int value)
    {
        Result<Rating> result = Rating.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(Rating.Invalid, result.Error);
    }
}
