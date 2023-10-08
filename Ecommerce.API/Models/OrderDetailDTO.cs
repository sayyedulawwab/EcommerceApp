namespace Ecommerce.API.Models
{
    public class OrderDetailDTO
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
    }
}
