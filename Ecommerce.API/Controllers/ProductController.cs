using AutoMapper;
using Ecommerce.Models.APIModels;
using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Services.Abstractions.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Ecommerce.API.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IMapper _mapper;

        public ProductController(IProductService productService, IMapper mapper)
        {
            _productService = productService;
            _mapper = mapper;

        }
        // GET: api/products
        [HttpGet]
        public IActionResult Get([FromQuery] ProductSearchCriteria productSearchCriteria)
        {
            var products = _productService.Search(productSearchCriteria);

            if (products == null || !products.Any())
            {
                return NotFound("Product not found!");
            }

            ICollection<ProductViewDTO> productModels = _mapper.Map<ICollection<ProductViewDTO>>(products);

            return Ok(productModels);

        }

        // GET api/products/5
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var product = _productService.GetById(id);

            if (product == null)
            {
                return NotFound("Product not found!");
            }

            var model = _mapper.Map<ProductViewDTO>(product);

            return Ok(model);
        }

        // POST api/products
        [Authorize]
        [HttpPost]
        public IActionResult Post([FromBody] ProductCreateDTO model)
        {
            if (ModelState.IsValid)
            {
                var product = _mapper.Map<Product>(model);

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
        [Authorize]
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] ProductEditDTO model)
        {
            if (ModelState.IsValid)
            {
                var product = _productService.GetById(id);

                if (product == null)
                {
                    return NotFound("Product not found to update!");
                }

                _mapper.Map(model, product);

                bool isSuccess = _productService.Update(product);
                if (isSuccess)
                {
                    return Ok("Product is updated!");
                }
            }

            return BadRequest(ModelState);
        }

        // DELETE api/products/5
        [Authorize]
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
