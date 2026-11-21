using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Products.AddProduct;
public record AddProductCommand(string Name, string Description, string PriceCurrency, decimal PriceAmount, int Quantity, Guid CategoryId) : ICommand<Guid>;
