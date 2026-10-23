using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Users.Register;
internal sealed class RegisterUserCommandHandler(
    IAuthService authService,
    IApplicationDbContext dbContext)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        bool emailExists = await dbContext.Users.AnyAsync(user => user.Email == new Email(request.Email), cancellationToken);

        if (emailExists)
        {
            return Result.Failure<Guid>(UserErrors.AlreadyExists);
        }

        string passwordSalt = authService.GenerateSalt();
        string hashedPassword = authService.HashPassword(request.Password, passwordSalt);

        var user = User.Create(new FirstName(request.FirstName),
                                new LastName(request.LastName),
                                new Email(request.Email), hashedPassword, passwordSalt, false);

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(cancellationToken);

        return user.Id.Value;
    }
}
