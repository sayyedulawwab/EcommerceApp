namespace Ecommerce.Models.APIModels;

public class ProductViewVM
{
    public int ProductID { get; set; }
    public string Name { get; set;}
    public double Price { get; set;}
    public int Quantity { get; set;}
    public string ProductCategoryName { get; set;}
    public string ProductCategoryCode { get; set; }
}
