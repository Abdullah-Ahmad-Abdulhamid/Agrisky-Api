using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriskyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserRepo _repo;

        public UserController(IUserRepo repo)
        {
            _repo = repo;
        }

        [AllowAnonymous]
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(int? categoryId, string? search)
        {
            var data = await _repo.GetProducts(categoryId, search);
            return Ok(data);
        }

        [HttpPost("add-to-cart/{userId}")]
        public async Task<IActionResult> AddToCart(int userId, AddToCartDto dto)
        {
            var result = await _repo.AddToCart(userId, dto);
            if (!result) return BadRequest(new { message = "Error adding to cart (Check stock or product ID)" });

            return Ok(new { message = "Item added to cart successfully" });
        }

        [HttpGet("cart/{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var data = await _repo.GetCart(userId);
            return Ok(data);
        }

        [AllowAnonymous]
        [HttpPost("contact")]
        public async Task<IActionResult> Contact(ContactDto dto)
        {
            await _repo.SendMessage(dto);
            return Ok(new { message = "Message sent successfully" });
        }

        // --- عمليات الطلبات (تحتاج تسجيل دخول) ---
        [HttpGet("orders/{userId}")]
        public async Task<IActionResult> Orders(int userId)
        {
            var data = await _repo.GetUserOrders(userId);
            return Ok(data);
        }
    }
}