﻿using Ecommerce.Models;
using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Services.Abstractions.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Services.Abstractions.Products
{
    public interface IProductService : IService<Product>
    {
        bool Add(Product product);
        bool Update(Product product);
        bool Delete(Product product);
        ICollection<Product> GetAll();
        ICollection<Product> Search(ProductSearchCriteria searchCriteria);
        Product GetById(int id);
    }
}
