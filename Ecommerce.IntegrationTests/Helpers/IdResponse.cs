namespace Ecommerce.IntegrationTests.Helpers;

// The body the API returns for a create, an edit or a delete: { "id": "..." }.
internal sealed record IdResponse(Guid Id);
