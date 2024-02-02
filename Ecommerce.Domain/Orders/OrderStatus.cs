namespace Ecommerce.Domain.Orders;

public enum OrderStatus
{ 
    Pending = 0,
    Shipped = 1,
    Delivered = 2,
    Cancelled = 3,
}