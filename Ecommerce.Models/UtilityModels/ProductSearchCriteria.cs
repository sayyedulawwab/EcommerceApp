using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Models.UtilityModels
{
    public class ProductSearchCriteria
    {
        public ProductSearchCriteria()
        {
            CurrentPage = 1;
            PageSize = 10;
        }
        public string Name { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public int ProductCategoryID { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
