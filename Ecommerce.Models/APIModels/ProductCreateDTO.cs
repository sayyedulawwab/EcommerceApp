namespace Ecommerce.Models.APIModels;

public class ProductCreateDTO
{
    public string Name { get; set;}
    public double Price { get; set;}
    public int Quantity { get; set;}
    public int? ProductCategoryID  { get; set;}

}
