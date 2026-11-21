using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;
internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(orderItem => orderItem.Id);

        builder.Property(orderItem => orderItem.Id)
               .HasConversion(orderItemId => orderItemId.Value, value => new OrderItemId(value));

        builder.Property(orderItem => orderItem.Quantity);

        builder.OwnsOne(orderItem => orderItem.UnitPrice, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code).Value);
        });

        builder.Property(orderItem => orderItem.CreatedOnUtc);

        builder.HasOne<Order>()
            .WithMany(order => order.OrderItems)
            .HasForeignKey(orderItem => orderItem.OrderId);

        // Restrict, not cascade: deleting a product must not delete lines from customers' orders.
        builder.HasOne(orderItem => orderItem.Product)
               .WithMany()
               .HasForeignKey(orderItem => orderItem.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

    }
}
