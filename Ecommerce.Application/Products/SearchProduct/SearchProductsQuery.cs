﻿using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Products.SearchProduct;
public record SearchProductsQuery(Guid? ProductCategoryId,
                                    decimal? MinPrice,
                                    decimal? MaxPrice,
                                    string? Keyword,
                                    int Page,
                                    int PageSize,
                                    string? SortColumn,
                                    string? SortOrder) : IQuery<PagedList<ProductResponse>>;
