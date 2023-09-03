using Ecommerce.Models;
using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Services.Products
{
    public class ProductService : BaseService<Product>, IProductService
    {
        IProductRepository _productRepository;

        public ProductService(IProductRepository repository) : base(repository)
        {
            _productRepository = repository;
        }
        public override ICollection<Product> GetAll()
        {
            return _productRepository.GetAll();
        }
        public Product GetById(int id)
        {
            return _productRepository.GetById(id);
        }

        public override bool Add(Product entity)
        {
            return _productRepository.Add(entity);
        }
        public override bool Update(Product entity)
        {
            return _productRepository.Update(entity);
        }
        public override bool Delete(Product entity)
        {
            return _productRepository.Delete(entity);
        }
        public ICollection<Product> Search(ProductSearchCriteria searchCriteria)
        {
            return _productRepository.Search(searchCriteria);
        }
    }
}
