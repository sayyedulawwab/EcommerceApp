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
               .HasConversion(name => name.Value, value => ProductName.Create(value).Value);

        builder.Property(product => product.Description)
               .HasMaxLength(2000)
               .HasConversion(description => description.Value, value => ProductDescription.Create(value).Value);

        builder.Property(product => product.Quantity);

        // PostgreSQL's xmin system column changes on every update, so two orders that both read the same stock can't both save.
        builder.Property<uint>("Version")
               .HasColumnName("xmin")
               .HasColumnType("xid")
               .IsRowVersion();

        builder.OwnsOne(product => product.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code).Value)
                        .HasDefaultValue(Currency.Bdt);

        });

        builder.Property(product => product.CreatedOnUtc);
        builder.Property(product => product.UpdatedOnUtc);

        builder.HasOne<Category>()
               .WithMany()
               .HasForeignKey(product => product.CategoryId);

    }
}
