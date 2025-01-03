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
        var productIds = request.OrderItems.Select(item => item.ProductId).ToList();

        List<Product> products = await _productRepository.GetProductsByIdsAsync(productIds);

        var orderItems = new List<(Product product, int quantity)>();

        foreach (PlaceOrderProductCommand item in request.OrderItems)
        {
            Product? product = products.FirstOrDefault(p => p.Id.Value == item.ProductId);
            if (product == null)
            {
                return Result.Failure<Guid>(Error.NotFound);
            }

            (Product product, int Quantity) orderItem = (product, item.Quantity);
            orderItems.Add(orderItem);
        }


        var order = Order.PlaceOrder(new UserId(request.UserId), orderItems, OrderStatus.Pending, _dateTimeProvider.UtcNow);

        _orderRepository.Add(order);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Id.Value;
    }
}
