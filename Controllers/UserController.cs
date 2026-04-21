using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgriskyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserRepo _repo;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserRepo repo, ILogger<UserController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // ── GET /api/user/products — public ───────────────────────────────────
        [AllowAnonymous]
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(int? categoryId, string? search)
        {
            var data = await _repo.GetProducts(categoryId, search);
            return Ok(data);
        }

        // ── GET /api/user/cart — JWT-based ────────────────────────────────────
        [HttpGet("cart")]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var user = await _repo.GetUserByIdAsync(userId.Value);
            if (user == null || !user.IsActive)
                return Unauthorized(new { message = "User not found or inactive." });

            var data = await _repo.GetCart(userId.Value);

            _logger.LogInformation("User {UserId} accessed cart", userId.Value);
            return Ok(data);
        }

        // ── POST /api/user/add-to-cart — JWT-based ───────────────────────────
        // NOTE: The old [HttpPost("add-to-cart/{userId}")] route is DELETED.
        //       UserId is ALWAYS derived from the JWT — never from the URL.
        [HttpPost("add-to-cart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var result = await _repo.AddToCart(userId.Value, dto);
            if (!result)
                return BadRequest(new
                {
                    message = "Failed to add item. Check product ID or available stock."
                });

            return Ok(new { message = "Item added to cart." });
        }

        // ── GET /api/user/orders — JWT-based ─────────────────────────────────
        // NOTE: The old [HttpGet("orders/{userId}")] route is DELETED.
        //       It was a critical IDOR vulnerability — any user could read any
        //       other user's orders by changing the ID in the URL.
        [HttpGet("orders")]
        public async Task<IActionResult> Orders()
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var user = await _repo.GetUserByIdAsync(userId.Value);
            if (user == null || !user.IsActive)
                return Unauthorized(new { message = "User not found or inactive." });

            var data = await _repo.GetUserOrders(userId.Value);

            _logger.LogInformation("User {UserId} accessed orders", userId.Value);
            return Ok(data);
        }

        // ── POST /api/user/contact — public ──────────────────────────────────
        [AllowAnonymous]
        [HttpPost("contact")]
        public async Task<IActionResult> Contact([FromBody] ContactDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _repo.SendMessage(dto);
            if (!success)
                return BadRequest(new { message = "Failed to send message." });

            return Ok(new { message = "Message sent successfully." });
        }

        // ── Helper: safely parse authenticated user ID from JWT ───────────────
        private int? GetAuthenticatedUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}