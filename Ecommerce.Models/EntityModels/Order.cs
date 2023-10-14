namespace Ecommerce.Models.EntityModels
{
    public class Order
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public User Customer { get; set; }
        public int UserID { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Shipped, Delivered, Cancelled
    }
}
