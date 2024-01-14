using Ecommerce.Domain.ProductCategories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;
internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
               .HasMaxLength(200)
               .HasConversion(name => name.Value, value => new CategoryName(value));

        builder.Property(category => category.Code)
               .HasMaxLength(200)
               .HasConversion(code => code.Value, value => new CategoryCode(value));

        builder.Property(category => category.CreatedOn);

        builder.Property(category => category.UpdatedOn);

    }
}
