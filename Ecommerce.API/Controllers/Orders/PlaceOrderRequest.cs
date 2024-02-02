using Ecommerce.Domain.Products;

namespace Ecommerce.API.Controllers.Orders;

public record PlaceOrderRequest(Guid userId, List<(Product product, int quantity)> orderItems);
