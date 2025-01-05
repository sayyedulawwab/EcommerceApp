using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
               .HasConversion(productId => productId.Value, value => new ProductId(value));

        builder.Property(product => product.Name)
               .HasMaxLength(200)
               .HasConversion(name => name.Value, value => new ProductName(value));

        builder.Property(product => product.Description)
               .HasMaxLength(2000)
               .HasConversion(description => description.Value, value => new ProductDescription(value));

        builder.Property(product => product.Quantity);



        builder.OwnsOne(product => product.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code))
                        .HasDefaultValue(Currency.FromCode("BDT"));

        });

        builder.Property(product => product.CreatedOnUtc);
        builder.Property(product => product.UpdatedOnUtc);

        builder.HasOne<Category>()
               .WithMany()
               .HasForeignKey(product => product.CategoryId);

    }
}
