using Ecommerce.Models.UtilityModels;

namespace Ecommerce.Web;

public class ProductCategoryListVM
{
    public ProductCategorySearchCriteria ProductCategorySearchCriteria { get; set; }
    public ICollection<ProductCategoryListItem> ProductCategoryList { get; set; }

}
