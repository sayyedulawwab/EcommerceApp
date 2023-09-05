
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Models.APIModels;

public class ProductCategoryCreateVM
{
    [Required(ErrorMessage = "Please provide a name")]
    public string Name { get; set;}
    public string Code  { get; set;}
    

}
