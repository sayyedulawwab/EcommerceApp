﻿using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.GetAllOrders;

public sealed class OrderResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public List<OrderItem> OrderItems { get; init; } = new List<OrderItem>();
    public decimal TotalPriceAmount { get; init; }
    public string TotalPriceCurrency { get; init; }
    public string Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ShippedOnUtc { get; init; }
    public DateTime? DeliveredOnUtc { get; init; }
    public DateTime? CancelledOnUtc { get; init; }
   

}