﻿using Ecommerce.Domain.Orders;
using Ecommerce.Domain.ProductCategories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;
internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(orderItem => orderItem.Id);

        builder.Property(orderItem => orderItem.Id)
               .HasConversion(orderItemId => orderItemId.Value, value => new OrderItemId(value));

        builder.Property(orderItem => orderItem.Quantity);

        builder.Property(orderItem => orderItem.CreatedOn);

        builder.Property(orderItem => orderItem.UpdatedOn);

        builder.HasOne<Order>()
            .WithMany(order => order.OrderItems)
            .HasForeignKey(orderItem => orderItem.OrderId);

        builder.HasOne(orderItem => orderItem.Product)
               .WithMany()
               .HasForeignKey(orderItem => orderItem.ProductId);

    }
}
