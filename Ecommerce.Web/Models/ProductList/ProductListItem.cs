using Ecommerce.Models;

namespace Ecommerce.Web;

public class ProductListItem
{
    public int ProductID { get; set;}
    public string Name { get; set;}
    public double Price { get; set;}
    public int Quantity { get; set;}
    public string? ProductCategoryName { get; set;}


}
