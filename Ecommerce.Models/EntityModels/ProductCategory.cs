namespace Ecommerce.Models.EntityModels
{

    public class ProductCategory
    {
        public int ProductCategoryID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public ICollection<Product>? Products { get; set; }


    }
}