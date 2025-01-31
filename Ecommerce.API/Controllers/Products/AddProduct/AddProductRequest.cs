﻿using System.Text.Json.Serialization;

namespace Ecommerce.API.Controllers.Products.AddProduct;

public record AddProductRequest
{
    [JsonRequired] public string Name { get; init; }
    [JsonRequired] public string Description { get; init; }
    [JsonRequired] public string PriceCurrency { get; init; }
    [JsonRequired] public decimal PriceAmount { get; init; }
    [JsonRequired] public int Quantity { get; init; }
    [JsonRequired] public Guid CategoryId { get; init; }
}
