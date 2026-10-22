using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users.Events;

namespace Ecommerce.Domain.Users;
public sealed class User : Entity<UserId>
{
    private User(UserId id, FirstName firstName, LastName lastName, Email email, string passwordHash, string passwordSalt, bool isAdmin) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        IsAdmin = isAdmin;
    }

    #pragma warning disable CS8618 // Used by EF Core to materialize the entity
    private User()
    {
    }
    #pragma warning restore CS8618
    public FirstName FirstName { get; private set; }
    public LastName LastName { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string PasswordSalt { get; private set; }
    public bool IsAdmin { get; private set; }

    public static User Create(FirstName firstName, LastName lastName, Email email, string passwordHash, string passwordSalt, bool isAdmin)
    {
        var user = new User(UserId.New(), firstName, lastName, email, passwordHash, passwordSalt, isAdmin);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }
}
