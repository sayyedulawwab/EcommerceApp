namespace Ecommerce.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal UnitPrice { get; set; }
        public int UnitsInStock {  get; set; }        
        public Guid? ProductCategoryId { get; set;}
        public ProductCategory? Category { get; set; }
    }
}
