using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;

namespace Ecommerce.Application.Users.Register;
internal sealed class RegisterUserCommandHandler(
    IAuthService authService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        User? existingUserByEmail = await userRepository.GetByEmail(request.Email);

        if (existingUserByEmail is not null)
        {
            return Result.Failure<Guid>(UserErrors.AlreadyExists);
        }

        string passwordSalt = authService.GenerateSalt();
        string hashedPassword = authService.HashPassword(request.Password, passwordSalt);

        var user = User.Create(new FirstName(request.FirstName),
                                new LastName(request.LastName),
                                new Email(request.Email), hashedPassword, passwordSalt, false);

        userRepository.Add(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id.Value;
    }
}
