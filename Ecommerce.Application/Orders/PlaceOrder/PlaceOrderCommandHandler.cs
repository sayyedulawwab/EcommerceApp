using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;

namespace Ecommerce.Application.Orders.PlaceOrder;
internal sealed class PlaceOrderCommandHandler(
    IProductRepository productRepository, 
    IOrderRepository orderRepository, 
    IUnitOfWork unitOfWork, 
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<PlaceOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var order = Order.Create(new UserId(request.UserId), OrderStatus.Placed, dateTimeProvider.UtcNow);

        foreach (OrderStockItem item in request.OrderStockItems)
        {
            Product? product = await productRepository.GetByIdAsync(new ProductId(item.ProductId), cancellationToken);

            if (product is null)
            {
                return Result.Failure<Guid>(ProductErrors.NotFound);
            }

            order.AddOrderItem(order.Id, product, item.Quantity, dateTimeProvider.UtcNow);
        }

        orderRepository.Add(order);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Id.Value;
    }
}
