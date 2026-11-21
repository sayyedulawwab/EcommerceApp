using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.UnitTests.Users;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static RefreshToken CreateToken()
    {
        return RefreshToken.Create(UserId.New(), "hash", Now, Now.AddDays(7));
    }

    [Fact]
    public void Create_Should_StartActive_WhenCreated()
    {
        RefreshToken token = CreateToken();

        Assert.False(token.IsRevoked);
        Assert.Null(token.RevokedOnUtc);
        Assert.False(token.IsExpired(Now));
        Assert.Equal("hash", token.TokenHash);
    }

    [Fact]
    public void IsExpired_Should_BeTrue_WhenTheExpiryTimeHasPassed()
    {
        RefreshToken token = CreateToken();

        Assert.False(token.IsExpired(Now.AddDays(7).AddTicks(-1)));
        Assert.True(token.IsExpired(Now.AddDays(7)));
    }

    [Fact]
    public void Revoke_Should_KeepTheFirstRevocationTime_WhenCalledTwice()
    {
        RefreshToken token = CreateToken();

        token.Revoke(Now.AddHours(1));
        token.Revoke(Now.AddHours(2));

        Assert.True(token.IsRevoked);
        Assert.Equal(Now.AddHours(1), token.RevokedOnUtc);
    }
}
