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
        var existingUserByEmail = await _userRepository.GetByEmail(request.email);

        if (existingUserByEmail is not null)
        {
            return Result.Failure<Guid>(UserErrors.AlreadyExists);
        }

        var passwordSalt = _authService.GenerateSalt();
        var hashedPassword = _authService.HashPassword(request.password, passwordSalt);

        var user = User.Create(new FirstName(request.firstName), 
                                new LastName(request.lastName), 
                                new Email(request.email), hashedPassword, passwordSalt, false);

        _userRepository.Add(user);

        await _unitOfWork.SaveChangesAsync();

        return user.Id.Value;
    }
}
