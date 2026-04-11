using Agrisky.Models;
using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgriskyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepo _repo;

        public ProductController(IProductRepo repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string search, int? categoryId)
        {
            var products = await _repo.GetAllWithFilter(search, categoryId);

            var result = products.Select(p => new ProductDto
            {
                ProductID = p.ProductID,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CategoryName = p.Category.Name,
                Images = p.ProductImages.Select(i => i.ImageURL).ToList()
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
                Images = p.ProductImages.Select(i => i.ImageURL).ToList()
            };

            return Ok(result);
        }

        [HttpPost]
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

            return Ok(new { message = "Created successfully", product.ProductID });
        }

        [HttpPut("{id}")]
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

            return Ok( "Updated successfully" );
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _repo.GetWithImages(id);

            if (product == null)
                return NotFound();

            _repo.Delete(product);
            await _repo.SaveAsync();

            return Ok( "Deleted successfully" );
        }
    }
}
