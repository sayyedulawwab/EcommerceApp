using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Models.APIModels;

public class ProductCategoryEditVM
{
    [Required]
    public string Name { get; set;}
    public string Code  { get; set;}
}
