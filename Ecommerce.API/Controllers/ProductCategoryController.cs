using Ecommerce.Models.UtilityModels;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Models.APIModels;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Models.EntityModels;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Ecommerce.API.Controllers
{
    [Route("api/productcategories")]
    [ApiController]
    public class ProductCategoryController : ControllerBase
    {
        private readonly IProductCategoryService _productCategoryService;

        public ProductCategoryController(IProductCategoryService productCategoryService)
        {
            _productCategoryService = productCategoryService;
           
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

            return Ok(productCategories);

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

            return Ok(productCategory);
        }

        // POST api/productcategories
        [HttpPost]
        public IActionResult Post([FromBody] ProductCategoryCreateVM model)
        {
            if (ModelState.IsValid)
            {
                var productCategory = new ProductCategory()
                {
                    Name = model.Name,
                    Code = model.Code,

                };
                
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

                productCategory.Name = model.Name;
                productCategory.Code = model.Code;


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
