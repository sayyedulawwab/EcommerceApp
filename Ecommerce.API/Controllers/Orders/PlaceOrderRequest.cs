namespace Ecommerce.API.Controllers.Orders;

public record PlaceOrderProductRequest(Guid ProductId, int Quantity);
public record PlaceOrderRequest(List<PlaceOrderProductRequest> OrderItems);
