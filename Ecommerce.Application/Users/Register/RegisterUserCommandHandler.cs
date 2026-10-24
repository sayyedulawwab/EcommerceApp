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
        Result<FirstName> firstName = FirstName.Create(request.FirstName);
        Result<LastName> lastName = LastName.Create(request.LastName);
        Result<Email> email = Email.Create(request.Email);

        if (firstName.IsFailure || lastName.IsFailure || email.IsFailure)
        {
            return Result.Failure<Guid>(ValidationError.FromResults([firstName, lastName, email]));
        }

        bool emailExists = await dbContext.Users.AnyAsync(user => user.Email == email.Value, cancellationToken);

        if (emailExists)
        {
            return Result.Failure<Guid>(UserErrors.AlreadyExists);
        }

        string passwordSalt = authService.GenerateSalt();
        string hashedPassword = authService.HashPassword(request.Password, passwordSalt);

        var user = User.Create(firstName.Value, lastName.Value, email.Value, hashedPassword, passwordSalt, false);

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(cancellationToken);

        return user.Id.Value;
    }
}
