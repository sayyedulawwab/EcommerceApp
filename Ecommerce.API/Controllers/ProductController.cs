using AutoMapper;
using Ecommerce.API.Models;
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
        public IActionResult Post([FromForm] ProductCreateDTO model)
        {
            if (ModelState.IsValid)
            {
                var virtualFilePath = string.Empty;

                // Save the image and set the path to the Product entity
                if (model.Image != null && model.Image.Length > 0)
                {
                   
                    var fileExtension = Path.GetExtension(model.Image.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", uniqueFileName);
                    virtualFilePath = $"/Uploads/{uniqueFileName}";

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.Image.CopyTo(stream);
                    }
                }

                var product = _mapper.Map<Product>(model);

                product.ImagePath = virtualFilePath;

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
        public IActionResult Put(int id, [FromForm] ProductEditDTO model)
        {
            if (ModelState.IsValid)
            {
                var existingProduct = _productService.GetById(id);

                if (existingProduct == null)
                {
                    return NotFound("Product not found to update!");
                }

                // Backup the existing image path
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), existingProduct.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                var virtualFilePath = string.Empty;

                if (model.Image != null && model.Image.Length > 0)
                {
                    // Save the new image
                    var fileExtension = Path.GetExtension(model.Image.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", uniqueFileName);
                    virtualFilePath = $"/Uploads/{uniqueFileName}";

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.Image.CopyTo(stream);
                    }

                  
                }

                _mapper.Map(model, existingProduct);

                if (!string.IsNullOrEmpty(virtualFilePath))
                {
                    // Remove old image from storage, if it exists
                    if (!string.IsNullOrEmpty(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }

                    // Assign new image path
                    existingProduct.ImagePath = virtualFilePath;
                }

                bool isSuccess = _productService.Update(existingProduct);
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

                // Delete the image file
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), product.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
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
