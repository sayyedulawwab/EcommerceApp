using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Contollers
{
    [Route("[controller]")]
    [Authorize]
    public class ProductsController : ApiController
    {

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = Array.Empty<string>();

            await Task.CompletedTask;

            return Ok(products); 
        }
    }
}
