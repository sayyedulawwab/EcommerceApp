using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users.Events;

namespace Ecommerce.Domain.Users;
public sealed class User : Entity<UserId>
{
    private User(UserId id, FirstName firstName, LastName lastName, Email email, string passwordHash, bool isAdmin) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        IsAdmin = isAdmin;
    }

    public FirstName FirstName { get; private set; }
    public LastName LastName { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsAdmin { get; private set; }

    public static User Create(FirstName firstName, LastName lastName, Email email, string passwordHash, bool isAdmin)
    {
        var user = new User(UserId.New(), firstName, lastName, email, passwordHash, isAdmin);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }

    // Used when the stored hash was made with an older work factor and is replaced at login.
    public void UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }
}
