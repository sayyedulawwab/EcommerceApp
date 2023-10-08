namespace Ecommerce.Models.EntityModels
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public bool IsAdmin { get; set; }
        public ICollection<Order>? Orders { get; set; }


    }
}
