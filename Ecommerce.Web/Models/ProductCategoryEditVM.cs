using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Web;

public class ProductCategoryEditVM
{
    public int ProductCategoryID { get; set;}
    [Required]
    public string Name { get; set;}
    public string Code  { get; set;}
}
