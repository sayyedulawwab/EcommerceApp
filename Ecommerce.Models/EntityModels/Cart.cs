namespace Ecommerce.Models.EntityModels
{
    public class Cart
    {
        public int CartID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; }
        public ICollection<CartItem> CartItems { get; set; }
    }
}
