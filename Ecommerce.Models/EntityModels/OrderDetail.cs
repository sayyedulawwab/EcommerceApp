namespace Ecommerce.Models.EntityModels
{
    public class OrderDetail
    {
        public int OrderDetailID { get; set; }
        public Order Order { get; set; }
        public int OrderID { get; set; }
        public Product Product { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
    }
}
