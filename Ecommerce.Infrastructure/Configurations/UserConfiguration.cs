using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.FirstName)
               .HasMaxLength(200)
               .HasConversion(name => name.Value, value => new FirstName(value));

        builder.Property(user => user.LastName)
               .HasMaxLength(200)
               .HasConversion(name => name.Value, value => new LastName(value));


        builder.Property(user => user.Email)
               .HasMaxLength(200)
               .HasConversion(email => email.Value, value => new Domain.Users.Email(value));

        builder.Property(user => user.PasswordHash);
        builder.Property(user => user.PasswordSalt);
        builder.Property(user => user.IsAdmin);
    }
}
