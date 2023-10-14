namespace Ecommerce.API.Models
{
    public class OrderEditDTO
    {
        public ICollection<OrderDetailDTO> Products { get; set; }
        public string Status { get; set; } = "Pending";

    }
}
