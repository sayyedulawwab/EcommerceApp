using AutoMapper;
using Ecommerce.API.Models;
using Ecommerce.Models.EntityModels;
using Ecommerce.Services.Abstractions.Carts;
using Ecommerce.Services.Abstractions.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {

        private readonly ICartService _cartService;
        private readonly IMapper _mapper;

        public CartController(ICartService cartService, IMapper mapper)
        {
            _cartService = cartService;
            _mapper = mapper;

        }

        [HttpPost("add")]
        public ActionResult AddToCart([FromBody] AddToCartDTO model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = _cartService.GetCartByUserId(userId) ?? _cartService.CreateCartForUser(userId);

            var cartItem = new CartItem
            {
                CartID = cart.CartID,
                ProductID = model.ProductID,
                Quantity = model.Quantity
            };

            if (_cartService.AddCartItem(cartItem))
                return Ok(new { message = "Product added to cart successfully." });
            else
                return BadRequest(new { message = "Error adding product to cart." });
        }

        [HttpGet]
        public ActionResult GetCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = _cartService.GetCartByUserId(userId);
            var cartDTO = _mapper.Map<CartViewDTO>(cart);
            return Ok(cartDTO);
        }

        [HttpPut("edit")]
        public ActionResult EditCartItem([FromBody] CartItemEditDTO model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = _cartService.GetCartByUserId(userId);

            var cartItem = cart?.CartItems?.FirstOrDefault(item => item.ProductID == model.ProductID);
            if (cartItem == null)
                return NotFound(new { message = "Product not found in cart." });

            cartItem.Quantity = model.Quantity;

            if (_cartService.UpdateCartItem(cartItem))
                return Ok(new { message = "Cart item updated successfully." });
            else
                return BadRequest(new { message = "Error updating cart item." });
        }

        [HttpDelete("remove/{productId}")]
        public ActionResult RemoveFromCart(int productId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = _cartService.GetCartByUserId(userId);

            var cartItem = cart?.CartItems?.FirstOrDefault(item => item.ProductID == productId);
            if (cartItem == null)
                return NotFound(new { message = "Product not found in cart." });

            if (_cartService.DeleteCartItem(cartItem))
                return Ok(new { message = "Product removed from cart successfully." });
            else
                return BadRequest(new { message = "Error removing product from cart." });
        }

        
    }
}
