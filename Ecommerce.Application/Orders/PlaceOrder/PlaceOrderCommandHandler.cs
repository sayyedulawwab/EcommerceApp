using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;

namespace Ecommerce.Application.Orders.PlaceOrder;
internal sealed class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlaceOrderCommandHandler(IProductRepository productRepository, IOrderRepository orderRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {

        // Fetch products from the repository based on productIds
        var productIds = request.orderItems.Select(item => item.productId).ToList();

        var products = await _productRepository.GetProductsByIdsAsync(productIds);

        // Create order items using the fetched products and quantities
        var orderItems = new List<(Product product, int quantity)>(); // Assuming there's an OrderItem class to hold order details

        foreach (var item in request.orderItems)
        {
            var product = products.FirstOrDefault(p => p.Id.Value == item.productId);
            if (product == null)
            {
                // Return failure if any product is not found
                return Result.Failure<Guid>(Error.NotFound);
            }

            // Assuming OrderItem has a constructor or factory method that takes product and quantity
            var orderItem = (product, item.quantity);
            orderItems.Add(orderItem);
        }


        var order = Order.PlaceOrder(new UserId(request.userId), orderItems, OrderStatus.Pending, _dateTimeProvider.UtcNow);

        _orderRepository.Add(order);

        await _unitOfWork.SaveChangesAsync();

        return order.Id.Value;
    }
}
