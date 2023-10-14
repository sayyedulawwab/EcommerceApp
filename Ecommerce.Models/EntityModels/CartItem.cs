namespace Ecommerce.Models.EntityModels
{
    public class CartItem
    {
        public int CartItemID { get; set; }
        public Cart Cart { get; set; }
        public int CartID { get; set; }
        public Product Product { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }

    }
}
