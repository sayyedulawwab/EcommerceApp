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

        builder.Property(review => review.Id)
               .HasConversion(reviewId => reviewId.Value, value => new ReviewId(value));

        builder.Property(review => review.Rating)
               .HasConversion(rating => rating.Value, value =>  Rating.Create(value).Value);

        builder.Property(review => review.Comment)
               .HasConversion(comment => comment.Value, value => new Comment(value));

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(review => review.UserId);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(review => review.ProductId);
    }
}
