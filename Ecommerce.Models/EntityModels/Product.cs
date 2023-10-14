namespace Ecommerce.Models.EntityModels 
{ 
    public class Product
    {
        public int ProductID { get; set;}
        public string Name { get; set;}
        public double Price { get; set;}
        public int Quantity { get; set;}
        public ProductCategory? ProductCategory { get; set;}
        public int? ProductCategoryID  { get; set;}
        public string? ImagePath { get; set; }



    }
}