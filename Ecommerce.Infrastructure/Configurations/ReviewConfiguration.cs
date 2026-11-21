using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");
        builder.HasKey(review => review.Id);

        builder.Property(review => review.Id).HasConversion(id => id.Value, value => new ReviewId(value));

        // A stored value that fails its own Create is corrupt data, so .Value throws on purpose.
        builder.Property(review => review.Rating).HasConversion(rating => rating.Value, value => Rating.Create(value).Value);
        builder.Property(review => review.Comment).HasConversion(comment => comment.Value, value => Comment.Create(value).Value);

        // References to other aggregates by ID only, with no navigation properties.
        builder.HasOne<Product>().WithMany().HasForeignKey(review => review.ProductId);
        builder.HasOne<User>().WithMany().HasForeignKey(review => review.UserId);
    }
}
