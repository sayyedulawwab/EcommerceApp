using Ecommerce.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
               .HasConversion(categoryId => categoryId.Value, value => new CategoryId(value));

        builder.Property(category => category.Name)
               .HasMaxLength(200)
               .HasConversion(name => name.Value, value => new CategoryName(value));

        builder.Property(category => category.Code)
               .HasMaxLength(200)
               .HasConversion(code => code.Value, value => new CategoryCode(value));

        builder.Property(category => category.CreatedOnUtc);

        builder.Property(category => category.UpdatedOnUtc);

    }
}
