﻿using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Products;
public static class ProductErrors
{
    public static readonly Error NotFound = new(
       "Property.Found",
       "The property with the specified identifier was not found",
       HttpResponseStatusCodes.NotFound);
}
