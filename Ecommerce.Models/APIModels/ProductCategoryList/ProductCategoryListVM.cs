using Ecommerce.Models.UtilityModels;


namespace Ecommerce.Models.APIModels;

public class ProductCategoryListVM
{
    public ProductCategorySearchCriteria ProductCategorySearchCriteria { get; set; }
    public ICollection<ProductCategoryListItem> ProductCategoryList { get; set; }

}
