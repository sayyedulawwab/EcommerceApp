namespace Ecommerce.Models.APIModels;
public class ProductEditDTO
{
    public int ProductID { get; set; }
    public string Name { get; set;}
    public double Price { get; set;}
    public int Quantity { get; set;}
    public int? ProductCategoryID  { get; set;}
}
