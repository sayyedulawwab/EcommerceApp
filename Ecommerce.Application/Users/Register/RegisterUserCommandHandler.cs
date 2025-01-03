using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;

namespace Ecommerce.Application.Users.Register;
internal sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(IAuthService authService, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _authService = authService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        User? existingUserByEmail = await _userRepository.GetByEmail(request.Email);

        if (existingUserByEmail is not null)
        {
            return Result.Failure<Guid>(UserErrors.AlreadyExists);
        }

        string passwordSalt = _authService.GenerateSalt();
        string hashedPassword = _authService.HashPassword(request.Password, passwordSalt);

        var user = User.Create(new FirstName(request.FirstName),
                                new LastName(request.LastName),
                                new Email(request.Email), hashedPassword, passwordSalt, false);

        _userRepository.Add(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id.Value;
    }
}
