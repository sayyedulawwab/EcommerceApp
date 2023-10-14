using AutoMapper;
using Ecommerce.API.Models;
using Ecommerce.Models.EntityModels;
using Ecommerce.Services.Abstractions.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {

        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;

        public OrderController(IOrderService orderService, IMapper mapper)
        {
            _orderService = orderService;
            _mapper = mapper;

        }
        // GET: api/orders
        [HttpGet]
        public IActionResult Get()
        {
            var orders = _orderService.GetAll();

            if (orders == null || !orders.Any())
            {
                return NotFound("order not found!");
            }

            ICollection<OrderViewDTO> orderModels = _mapper.Map<ICollection<OrderViewDTO>>(orders);

            return Ok(orderModels);

        }

        // GET api/orders/5
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var order = _orderService.GetById(id);

            if (order == null)
            {
                return NotFound("Order not found!");
            }

            var model = _mapper.Map<OrderViewDTO>(order);

            return Ok(model);
        }

        // POST api/orders/checkout
        [Authorize]
        [HttpPost("checkout")]
        public IActionResult Checkout()
        {
            if (ModelState.IsValid)
            {

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var order = _orderService.PlaceOrderFromCart(userId);

                if (order != null)
                {
                    return Ok("Order successfully created!");
                }
                return BadRequest("Error during the checkout process.");

            }

            return BadRequest(ModelState);
        }

        // PUT api/orders/5
        [Authorize]
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] OrderEditDTO model)
        {
            if (ModelState.IsValid)
            {
                var order = _orderService.GetById(id);

                if (order == null)
                {
                    return NotFound("Order not found to update!");
                }

                _mapper.Map(model, order);

                bool isSuccess = _orderService.Update(order);
                if (isSuccess)
                {
                    return Ok("Order is updated!");
                }
            }

            return BadRequest(ModelState);
        }

        // DELETE api/orders/5
        [Authorize]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var product = _orderService.GetById((int)id);

                if (product == null)
                {
                    return NotFound("Order not found to delete!");
                }

                bool isSuccess = _orderService.Delete(product);

                if (isSuccess)
                {
                    return Ok("Order is deleted");
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
