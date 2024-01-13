using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Products.EditProduct;
public record EditProductCommand(Guid id, string name, string description, string priceCurrency, decimal priceAmount, int quantity, Guid productCategoryId) : ICommand<Guid>;
