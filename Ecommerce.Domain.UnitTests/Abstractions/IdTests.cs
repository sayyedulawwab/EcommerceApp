using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.UnitTests.Abstractions;

// IDs are UUIDv7: the timestamp at the front keeps primary key inserts in order in PostgreSQL.
public class IdTests
{
    [Fact]
    public void New_Should_CreateVersion7Guids()
    {
        Guid[] ids =
        [
            CategoryId.New().Value,
            OrderId.New().Value,
            OrderItemId.New().Value,
            ProductId.New().Value,
            ReviewId.New().Value,
            UserId.New().Value
        ];

        Assert.All(ids, id => Assert.Equal(7, id.Version));
    }
}
