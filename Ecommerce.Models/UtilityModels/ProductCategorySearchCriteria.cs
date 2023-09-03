using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Models.UtilityModels
{
    public class ProductCategorySearchCriteria
    {
        public ProductCategorySearchCriteria()
        {
            CurrentPage = 1;
            PageSize = 10;
        }
        public string? Name { get; set; }
        public string Code { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
