namespace Ecommerce.API.Models
{
    public class OrderEditDTO
    {
        public int OrderID { get; set; }
        public ICollection<OrderDetailDTO> Products { get; set; }
    }
}
