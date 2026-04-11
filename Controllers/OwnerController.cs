using AgriskyApi.IRepo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgriskyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OwnerController : ControllerBase
    {
        private readonly IOwnerRepo _repo;

        public OwnerController(IOwnerRepo repo)
        {
            _repo = repo;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var data = await _repo.GetDashboardData();
            return Ok(data);
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _repo.GetOrders();
            return Ok(orders);
        }

        [HttpDelete("messages/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var result = await _repo.DeleteMessage(id);

            if (!result)
                return NotFound();

            return Ok( "Deleted successfully" );
        }

        [HttpGet("proof/{orderId}")]
        public async Task<IActionResult> GetProof(int orderId)
        {
            var path = await _repo.GetProofImage(orderId);

            if (string.IsNullOrEmpty(path))
                return NotFound("No proof found");

            return Ok(new { image = path });
        }
    }
}
