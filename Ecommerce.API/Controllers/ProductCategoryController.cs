using Ecommerce.Models.UtilityModels;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Models.APIModels;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Models.EntityModels;
using AutoMapper;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Ecommerce.API.Controllers
{
    [Route("api/productcategories")]
    [ApiController]
    public class ProductCategoryController : ControllerBase
    {
        private readonly IProductCategoryService _productCategoryService;
        private readonly IMapper _mapper;

        public ProductCategoryController(IProductCategoryService productCategoryService, IMapper mapper)
        {
            _productCategoryService = productCategoryService;
            _mapper = mapper;


        }
        // GET: api/productcategories
        [HttpGet]
        public IActionResult Get([FromQuery] ProductCategorySearchCriteria productCategorySearchCriteria)
        {
            var productCategories = _productCategoryService.Search(productCategorySearchCriteria);

            if (productCategories == null)
            {
                return NotFound();
            }

            ICollection<ProductCategoryViewVM> productCategoryModels = _mapper.Map<ICollection<ProductCategoryViewVM>>(productCategories);

            return Ok(productCategoryModels);

        }

        // GET api/productcategories/5
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var productCategory = _productCategoryService.GetById(id);

            if (productCategory == null)
            {
                return NotFound("Product category not found!");
            }

            var model = _mapper.Map<ProductCategoryViewVM>(productCategory);

         
            return Ok(model);
        }

        // POST api/productcategories
        [HttpPost]
        public IActionResult Post([FromBody] ProductCategoryCreateVM model)
        {
            if (ModelState.IsValid)
            {
                var productCategory = _mapper.Map<ProductCategory>(model);


                bool isSuccess = _productCategoryService.Add(productCategory);

                if (isSuccess)
                {
                    return Ok();
                }

                return BadRequest("Product category could not be saved!");
            }

            return BadRequest(ModelState);
        }

        // PUT api/productcategories/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] ProductCategoryEditVM model)
        {
            if (ModelState.IsValid)
            {
                var productCategory = _productCategoryService.GetById(id);

                if (productCategory == null)
                {
                    return NotFound("Product Category not found to update!");
                }

                _mapper.Map(model, productCategory);

                bool isSuccess = _productCategoryService.Update(productCategory);
                if (isSuccess)
                {
                    return Ok();
                }
            }

            return BadRequest(ModelState);
        }

        // DELETE api/productcategories/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var productCategory = _productCategoryService.GetById((int)id);

                if (productCategory == null)
                {
                    return NotFound("Product Category not found to delete!");
                }

                bool isSuccess = _productCategoryService.Delete(productCategory);

                if (isSuccess)
                {
                    return Ok("Product Category is deleted");
                }
                return BadRequest();

            }
            catch
            {
                return BadRequest();
            }
        }
    }
}
