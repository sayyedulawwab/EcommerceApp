namespace Ecommerce.API.Models;

public class ProductViewDTO
{
    public int ProductID { get; set; }
    public string Name { get; set;}
    public double Price { get; set;}
    public int Quantity { get; set;}
    public int ProductCategoryID { get; set; }
    public string ProductCategoryName { get; set;}
    public string ProductCategoryCode { get; set; }
    public string ImagePath { get; set; }
}
