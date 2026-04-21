using AgriskyApi.IRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgriskyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]   // ← Entire controller requires Admin role
    public class OwnerController : ControllerBase
    {
        private readonly IOwnerRepo _repo;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<OwnerController> _logger;

        public OwnerController(
            IOwnerRepo repo,
            IWebHostEnvironment env,
            ILogger<OwnerController> logger)
        {
            _repo = repo;
            _env = env;
            _logger = logger;
        }

        // ── GET /api/owner/dashboard ──────────────────────────────────────────
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var data = await _repo.GetDashboardData();
            return Ok(data);
        }

        // ── GET /api/owner/orders ─────────────────────────────────────────────
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _repo.GetOrders();
            return Ok(orders);
        }

        // ── DELETE /api/owner/messages/{id} ──────────────────────────────────
        [HttpDelete("messages/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            var result = await _repo.DeleteMessage(id);
            if (!result)
                return NotFound(new { message = "Message not found." });

            _logger.LogInformation(
                "Admin {Admin} deleted contact message {MessageId}", adminEmail, id);

            return Ok(new { message = "Deleted successfully." });
        }

        // ── GET /api/owner/proof/{orderId} ────────────────────────────────────
        /// <summary>
        /// Serves proof images through an authenticated endpoint.
        /// Files are stored OUTSIDE wwwroot and are NOT accessible via static files.
        /// </summary>
        [HttpGet("proof/{orderId}")]
        public async Task<IActionResult> GetProof(int orderId)
        {
            var relativePath = await _repo.GetProofImage(orderId);

            if (string.IsNullOrEmpty(relativePath))
                return NotFound(new { message = "No proof found for this order." });

            // Proof files are stored relative to ContentRootPath, NOT WebRootPath
            var fullPath = Path.Combine(_env.ContentRootPath, relativePath);
            fullPath = Path.GetFullPath(fullPath);

            // ── Prevent path traversal ────────────────────────────────────────
            var proofsRoot = Path.GetFullPath(
                Path.Combine(_env.ContentRootPath, "private", "proofs"));

            if (!fullPath.StartsWith(proofsRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Path traversal attempt detected for order {OrderId} by admin {Admin}",
                    orderId, User.FindFirst(ClaimTypes.Email)?.Value);
                return BadRequest(new { message = "Invalid file path." });
            }

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { message = "Proof file not found on disk." });

            var extension = Path.GetExtension(fullPath).ToLowerInvariant();
            var mimeType = extension switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            // PhysicalFile streams the file without loading it all into memory
            return PhysicalFile(fullPath, mimeType, enableRangeProcessing: true);
        }
    }
}