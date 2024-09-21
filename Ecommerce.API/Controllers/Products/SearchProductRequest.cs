namespace Ecommerce.API.Controllers.Products;

public record SearchProductRequest(Guid? productCategoryId, 
                                    decimal? minPrice, 
                                    decimal? maxPrice, 
                                    string? keyword,
                                    int page,
                                    int pageSize,
                                    string? sortColumn, 
                                    string? sortOrder);
