namespace Ecommerce.API.Models;
public class ProductEditDTO
{
    public string Name { get; set;}
    public double Price { get; set;}
    public int Quantity { get; set;}
    public int? ProductCategoryID  { get; set;}
    public IFormFile Image { get; set; }
}
