namespace Ecommerce.API.Models
{
    public class CartViewDTO
    {
        public int CartID { get; set; }
        public ICollection<CartItemDTO> CartItems { get; set; }
        public double TotalPrice { get; set; }
    }
}
