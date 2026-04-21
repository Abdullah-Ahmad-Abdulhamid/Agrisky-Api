using Agrisky.Models;
using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using AgriskyApi.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AgriskyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepo _repo;
        private readonly IHubContext<AppHub> _hubContext;

        public ProductController(IProductRepo repo, IHubContext<AppHub> hubContext)
        {
            _repo = repo;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? search, int? categoryId)
        {
            var products = await _repo.GetAllWithFilter(search, categoryId);

            var result = products.Select(p => new ProductDto
            {
                ProductID = p.ProductID,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CategoryName = p.Category?.Name,
                Images = p.ProductImages?.Select(i => i.ImageURL).ToList() ?? new List<string>()
            });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _repo.GetWithImages(id);

            if (p == null)
                return NotFound();

            var result = new ProductDto
            {
                ProductID = p.ProductID,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CategoryName = p.Category?.Name,
                Images = p.ProductImages?.Select(i => i.ImageURL).ToList() ?? new List<string>()
            };

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryID = dto.CategoryID
            };

            await _repo.Add(product);
            await _repo.SaveAsync();

            // Fetch the product with Category and Images to send a complete object to clients
            var fullProduct = await _repo.GetWithImages(product.ProductID);

            var productData = new ProductDto
            {
                ProductID = fullProduct.ProductID,
                Name = fullProduct.Name,
                Description = fullProduct.Description,
                Price = fullProduct.Price,
                StockQuantity = fullProduct.StockQuantity,
                CategoryName = fullProduct.Category?.Name,
                Images = fullProduct.ProductImages?.Select(i => i.ImageURL).ToList() ?? new List<string>()
            };

            // Real-time: Notify all users that a new product exists
            await _hubContext.Clients.All.SendAsync("ProductCreated", productData);

            return Ok(new { message = "Created successfully", product.ProductID });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, UpdateProductDto dto)
        {
            var product = await _repo.GetById(id);

            if (product == null)
                return NotFound();

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryID = dto.CategoryID;

            _repo.Update(product);
            await _repo.SaveAsync();

            // Fetch complete data for broadcast
            var updatedProduct = await _repo.GetWithImages(id);
            var productData = new ProductDto
            {
                ProductID = updatedProduct.ProductID,
                Name = updatedProduct.Name,
                Description = updatedProduct.Description,
                Price = updatedProduct.Price,
                StockQuantity = updatedProduct.StockQuantity,
                CategoryName = updatedProduct.Category?.Name,
                Images = updatedProduct.ProductImages?.Select(i => i.ImageURL).ToList() ?? new List<string>()
            };

            // Real-time: Notify all users to update this product in their UI
            await _hubContext.Clients.All.SendAsync("ProductUpdated", productData);

            return Ok("Updated successfully");
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _repo.GetWithImages(id);

            if (product == null)
                return NotFound();

            _repo.Delete(product);
            await _repo.SaveAsync();

            // Real-time: Notify all users to remove this product ID from their UI
            await _hubContext.Clients.All.SendAsync("ProductDeleted", id);

            return Ok("Deleted successfully");
        }
    }
}