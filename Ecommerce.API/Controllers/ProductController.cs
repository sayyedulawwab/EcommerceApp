using Ecommerce.Models.APIModels;
using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Services.Products;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Ecommerce.API.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;

        }
        // GET: api/products
        [HttpGet]
        public IActionResult Get([FromQuery] ProductSearchCriteria productSearchCriteria)
        {
            var products = _productService.Search(productSearchCriteria);

            if (products == null)
            {
                return NotFound();
            }

            return Ok(products);

        }

        // GET api/products/5
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var product = _productService.GetById(id);

            if (product == null)
            {
                return NotFound("Product category not found!");
            }

            return Ok(product);
        }

        // POST api/products
        [HttpPost]
        public IActionResult Post([FromBody] ProductCreateVM model)
        {
            if (ModelState.IsValid)
            {
                var product = new Product()
                {
                    Name = model.Name,
                    Price = model.Price,
                    Quantity = model.Quantity,
                    ProductCategoryID = model.ProductCategoryID
                };

                bool isSuccess = _productService.Add(product);

                if (isSuccess)
                {
                    return Ok("Product is created!");
                }

                return BadRequest("Product could not be saved!");
            }

            return BadRequest(ModelState);
        }

        // PUT api/products/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] ProductEditVM model)
        {
            if (ModelState.IsValid)
            {
                var product = _productService.GetById(id);

                if (product == null)
                {
                    return NotFound("Product not found to update!");
                }

                product.Name = model.Name;
                product.Price = model.Price;
                product.Quantity = model.Quantity;
                product.ProductCategoryID = model.ProductCategoryID;

                bool isSuccess = _productService.Update(product);
                if (isSuccess)
                {
                    return Ok("Product is upudated!");
                }
            }

            return BadRequest(ModelState);
        }

        // DELETE api/products/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var product = _productService.GetById((int)id);

                if (product == null)
                {
                    return NotFound("Product not found to delete!");
                }

                bool isSuccess = _productService.Delete(product);

                if (isSuccess)
                {
                    return Ok("Product is deleted");
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
