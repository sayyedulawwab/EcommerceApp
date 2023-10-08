﻿namespace Ecommerce.API.Models
{
    public class OrderViewDTO
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public ICollection<OrderDetailDTO> Products { get; set; }
    }
}
