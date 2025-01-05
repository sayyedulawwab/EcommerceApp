namespace Ecommerce.API.Controllers.Orders.PlaceOrder;

public record PlaceOrderProductRequest(Guid ProductId, int Quantity);
public record PlaceOrderRequest(List<PlaceOrderProductRequest> OrderItems);
