using System.Net.Http.Json;
using Ecommerce.API.Controllers.Orders;
using Ecommerce.Domain.Products;

namespace Ecommerce.IntegrationTests.Helpers;

internal static class OrderData
{
    // Places an order through the API, so the order goes through the same rules as in production.
    public static async Task<Guid> PlaceAsync(HttpClient client, Product product, int quantity, string? couponCode = null)
    {
        var request = new PlaceOrderRequest([new PlaceOrderProductRequest(product.Id.Value, quantity)], couponCode);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}

// The JSON shape of PagedList<T>, which can't be deserialized directly.
internal sealed record PagedResponse<T>(List<T> Items, int Page, int PageSize, long TotalCount, bool HasPreviousPage, bool HasNextPage);
