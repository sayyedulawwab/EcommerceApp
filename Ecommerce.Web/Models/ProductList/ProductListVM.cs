using Ecommerce.Models.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ecommerce.Web;

public class ProductListVM
{
    public ProductSearchCriteria ProductSearchCriteria { get; set; }
    public ICollection<ProductListItem> ProductList { get; set; }
    public IEnumerable<SelectListItem> ProductCategoryList { get; set; }

}
