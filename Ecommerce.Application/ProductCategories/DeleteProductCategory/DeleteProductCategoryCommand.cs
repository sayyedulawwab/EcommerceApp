﻿using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.ProductCategories.DeleteProductCategory;
public record DeleteProductCategoryCommand(Guid id) : ICommand<Guid>;
