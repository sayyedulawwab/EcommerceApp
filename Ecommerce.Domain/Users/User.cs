using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users.Events;

namespace Ecommerce.Domain.Users;
public sealed class User : Entity
{
    private User(Guid id, FirstName firstName, LastName lastName, Email email, string passwordHash, string passwordSalt, bool isAdmin) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        IsAdmin = isAdmin;
    }

    private User()
    {
    }
    public FirstName FirstName { get; private set; }
    public LastName LastName { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string PasswordSalt { get; private set; }
    public bool IsAdmin { get; private set; }

    public static User Create(FirstName firstName, LastName lastName, Email email, string passwordHash, string passwordSalt, bool isAdmin)
    {
        var user = new User(Guid.NewGuid(), firstName, lastName, email, passwordHash, passwordSalt, isAdmin);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }
}
