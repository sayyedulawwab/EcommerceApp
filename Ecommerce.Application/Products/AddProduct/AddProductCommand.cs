using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Products.AddProduct;
public record AddProductCommand(string name, string description, string priceCurrency, decimal priceAmount, int quantity, Guid productCategoryId, DateTime createdOn) : ICommand<Guid>;
