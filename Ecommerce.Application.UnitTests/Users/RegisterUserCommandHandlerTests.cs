using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Users.Register;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;
using FluentAssertions;
using Moq;

namespace Ecommerce.Application.UnitTests.Users;
public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public RegisterUserCommandHandlerTests()
    {
        _userRepositoryMock = new();
        _unitOfWorkMock = new();
        _authServiceMock = new();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailureResult_WhenEmailIsNotUnique()
    {
        // Arrange
        var existingUser = User.Create(
                new FirstName("Existing"),
                new LastName("User"),
                new Email("test@gmail.com"),
                "existingPasswordHash",
                "existingPasswordSalt",
                isAdmin: false);


        var command = new RegisterUserCommand("Existing", "User", "test@gmail.com", "test12345");

        _userRepositoryMock.Setup(
            x => x.GetByEmail(
                It.IsAny<string>()))
            .ReturnsAsync(existingUser);


        var handler = new RegisterUserCommandHandler(
            _authServiceMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);

        // Act

        Result<Guid> result = await handler.Handle(command, default);

        // Assert

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.AlreadyExists);
    }


    [Fact]
    public async Task Handle_Should_ReturnSuccessResult_WhenEmailIsUnique()
    {
        // Arrange
        var existingUser = User.Create(
                new FirstName("Existing"),
                new LastName("User"),
                new Email("test@gmail.com"),
                "existingPasswordHash",
                "existingPasswordSalt",
                isAdmin: false);


        var command = new RegisterUserCommand("Uniquer", "User", "unique@gmail.com", "testunique12345");

        _userRepositoryMock.Setup(
            x => x.GetByEmail(
                It.IsAny<string>()))
            .ReturnsAsync((User?)null);


        var handler = new RegisterUserCommandHandler(
            _authServiceMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);

        // Act

        Result<Guid> result = await handler.Handle(command, default);

        // Assert

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }


    [Fact]
    public async Task Handle_Should_CallAddOnRepository_WhenEmailIsUnique()
    {
        // Arrange
        var existingUser = User.Create(
                new FirstName("Existing"),
                new LastName("User"),
                new Email("test@gmail.com"),
                "existingPasswordHash",
                "existingPasswordSalt",
                isAdmin: false);


        var command = new RegisterUserCommand("Uniquer", "User", "unique@gmail.com", "testunique12345");

        _userRepositoryMock.Setup(
            x => x.GetByEmail(
                It.IsAny<string>()))
            .ReturnsAsync((User?)null);


        var handler = new RegisterUserCommandHandler(
            _authServiceMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);

        // Act

        Result<Guid> result = await handler.Handle(command, default);

        // Assert

        _userRepositoryMock.Verify(
            x => x.Add(It.Is<User>(u => u.Id.Value == result.Value)),
            Times.Once
            );
    }


    [Fact]
    public async Task Handle_Should_NotCallUnitOfWork_WhenEmailIsNotUnique()
    {
        // Arrange
        var existingUser = User.Create(
                new FirstName("Existing"),
                new LastName("User"),
                new Email("test@gmail.com"),
                "existingPasswordHash",
                "existingPasswordSalt",
                isAdmin: false);


        var command = new RegisterUserCommand("Existing", "User", "test@gmail.com", "test12345");

        _userRepositoryMock.Setup(
            x => x.GetByEmail(
                It.IsAny<string>()))
            .ReturnsAsync(existingUser);


        var handler = new RegisterUserCommandHandler(
            _authServiceMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);

        // Act

        Result<Guid> result = await handler.Handle(command, default);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never
            );

    }
}
