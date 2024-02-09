namespace Ecommerce.API.Controllers.Orders;

public record PlaceOrderProductRequest(Guid productId, int quantity);
public record PlaceOrderRequest(List<PlaceOrderProductRequest> orderItems);
