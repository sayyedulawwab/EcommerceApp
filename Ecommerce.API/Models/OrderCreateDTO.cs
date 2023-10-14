namespace Ecommerce.API.Models
{
    public class OrderCreateDTO
    {
        public ICollection<OrderDetailDTO> Products { get; set; }
        public double TotalPrice { get; set; }
    }
}
