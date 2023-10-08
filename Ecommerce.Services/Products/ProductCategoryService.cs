using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories;
using Ecommerce.Repositories.Abstractions.Base;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Services.Products
{
    public class ProductCategoryService : BaseService<ProductCategory>, IProductCategoryService
    {
        private readonly IProductCategoryRepository _productCategoryRepository;

        public ProductCategoryService(IProductCategoryRepository repository) : base(repository)
        {
            _productCategoryRepository = repository;
        }

        public override ICollection<ProductCategory> GetAll()
        {
            return _productCategoryRepository.GetAll();
        }

        public ProductCategory GetById(int id)
        {
            return _productCategoryRepository.GetById(id);
        }

        public override bool Add(ProductCategory entity)
        {
            return _productCategoryRepository.Add(entity);
        }

        public override bool Update(ProductCategory entity)
        {
            return _productCategoryRepository.Update(entity);
        }
        public override bool Delete(ProductCategory entity)
        {
            return _productCategoryRepository.Delete(entity);
        }
        public ICollection<ProductCategory> Search(ProductCategorySearchCriteria searchCriteria)
        {
            return _productCategoryRepository.Search(searchCriteria);
        }
       
    }
}
