using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id)
               .HasConversion(orderId => orderId.Value, value => new OrderId(value));

        builder.Property(order => order.Status);

        builder.OwnsOne(order => order.TotalPrice, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code))
                        .HasDefaultValue(Currency.FromCode("BDT"));

        });

        builder.Property(order => order.CreatedOnUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(order => order.UserId);

    }
}
