using Ecommerce.Domain.Products;

namespace Ecommerce.API.Controllers.Orders;

public record PlaceOrderRequest(List<(Product product, int quantity)> orderItems);
