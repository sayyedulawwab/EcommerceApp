using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Models.APIModels;

public class ProductCreateVM
{
    [Required(ErrorMessage = "Please provide a name")]
    public string Name { get; set;}
    [Required(ErrorMessage = "Please provide a price")]
    public double Price { get; set;}
    [Required(ErrorMessage = "Please provide quantity")]
    public int Quantity { get; set;}
    public int? ProductCategoryID  { get; set;}

}
