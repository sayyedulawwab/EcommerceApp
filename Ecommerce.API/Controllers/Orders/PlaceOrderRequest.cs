namespace Ecommerce.API.Controllers.Orders;

public record PlaceOrderRequest(List<(Guid productId, int quantity)> orderItems);
