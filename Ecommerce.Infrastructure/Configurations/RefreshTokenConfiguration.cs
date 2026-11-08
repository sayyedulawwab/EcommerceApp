using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    // A SHA-256 hash written as hex.
    private const int TokenHashLength = 64;

    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
               .HasConversion(tokenId => tokenId.Value, value => new RefreshTokenId(value));

        builder.Property(token => token.TokenHash)
               .HasMaxLength(TokenHashLength);

        builder.HasIndex(token => token.TokenHash)
               .IsUnique();

        builder.HasIndex(token => token.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId);
    }
}
