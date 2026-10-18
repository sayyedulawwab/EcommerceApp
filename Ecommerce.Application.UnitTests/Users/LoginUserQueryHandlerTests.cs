using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Users.Login;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;
using Moq;

namespace Ecommerce.Application.UnitTests.Users;
public class LoginUserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;

    public LoginUserQueryHandlerTests()
    {
        _userRepositoryMock = new();
        _authServiceMock = new();
        _jwtServiceMock = new();
    }

    [Fact]
    public async Task Handle_Should_ReturnAccessToken_WhenCredentialsAreValid()
    {
        // Arrange
        User user = MakeUser();

        _userRepositoryMock.Setup(x => x.GetByEmail(It.IsAny<string>())).ReturnsAsync(user);
        _authServiceMock.Setup(x => x.VerifyPassword("test12345", user.PasswordSalt, user.PasswordHash)).Returns(true);
        _jwtServiceMock.Setup(x => x.GetAccessToken(It.IsAny<string>(), user.Id.Value, It.IsAny<CancellationToken>()))
            .Returns(Result.Success("token"));

        LoginUserQueryHandler handler = CreateHandler();

        // Act
        Result<AccessTokenResponse> result = await handler.Handle(new LoginUserQuery("test@gmail.com", "test12345"), default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("token", result.Value.AccessToken);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidCredentials_WhenUserDoesNotExist()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByEmail(It.IsAny<string>())).ReturnsAsync((User?)null);

        LoginUserQueryHandler handler = CreateHandler();

        // Act
        Result<AccessTokenResponse> result = await handler.Handle(new LoginUserQuery("missing@gmail.com", "test12345"), default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidCredentials, result.Error);
        _jwtServiceMock.Verify(
            x => x.GetAccessToken(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidCredentials_WhenPasswordIsWrong()
    {
        // Arrange
        User user = MakeUser();

        _userRepositoryMock.Setup(x => x.GetByEmail(It.IsAny<string>())).ReturnsAsync(user);
        _authServiceMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        LoginUserQueryHandler handler = CreateHandler();

        // Act
        Result<AccessTokenResponse> result = await handler.Handle(new LoginUserQuery("test@gmail.com", "wrong-password"), default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidCredentials, result.Error);
        _jwtServiceMock.Verify(
            x => x.GetAccessToken(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private LoginUserQueryHandler CreateHandler()
    {
        return new LoginUserQueryHandler(
            _userRepositoryMock.Object,
            _authServiceMock.Object,
            _jwtServiceMock.Object);
    }

    private static User MakeUser()
    {
        return User.Create(
            new FirstName("Existing"),
            new LastName("User"),
            new Email("test@gmail.com"),
            "passwordHash",
            "passwordSalt",
            isAdmin: false);
    }
}
