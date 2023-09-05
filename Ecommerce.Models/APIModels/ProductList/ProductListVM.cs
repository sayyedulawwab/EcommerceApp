using Ecommerce.Models.UtilityModels;

namespace Ecommerce.Models.APIModels;

public class ProductListVM
{
    public ProductSearchCriteria ProductSearchCriteria { get; set; }
    public ICollection<ProductListItem> ProductList { get; set; }

}
