using Ecommerce.Domain.Coupons;
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

        // A second cancel that read the same order at the same time fails instead of giving the stock back twice.
        builder.Property<uint>("Version")
               .HasColumnName("xmin")
               .HasColumnType("xid")
               .IsRowVersion();

        builder.OwnsOne(order => order.Discount, discountBuilder =>
        {
            discountBuilder.Property(money => money.Amount)
                           .HasColumnType("decimal(18, 2)");

            discountBuilder.Property(money => money.Currency)
                           .HasConversion(currency => currency.Code, code => Currency.FromCode(code).Value);
        });

        builder.Property(order => order.CouponId)
               .HasConversion(couponId => couponId!.Value, value => new CouponId(value));

        builder.HasOne<Coupon>()
               .WithMany()
               .HasForeignKey(order => order.CouponId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(order => order.TotalPrice, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code).Value)
                        .HasDefaultValue(Currency.Bdt);

        });

        builder.Property(order => order.CreatedOnUtc);
        builder.Property(order => order.CancelledOnUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(order => order.UserId);

    }
}
