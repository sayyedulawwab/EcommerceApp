using Ecommerce.Domain.ProductCategories;
using Ecommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
               .HasMaxLength(200)
               .HasConversion(name => name.Value, value => new ProductName(value));

        builder.Property(product => product.Description)
               .HasMaxLength(2000)
               .HasConversion(description => description.Value, value => new ProductDescription(value));

        builder.Property(product => product.Quantity);

        builder.OwnsOne(product => product.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code));

        });

        builder.Property(product => product.CreatedOn);
        builder.Property(product => product.UpdatedOn);

        builder.HasOne<ProductCategory>()
               .WithMany()
               .HasForeignKey(product => product.ProductCategoryId);





    }
}
