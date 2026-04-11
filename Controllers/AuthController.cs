using Agrisky.Models;
using Agrisky.Models.Agrisky.Models;
using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using AgriskyApi.Services; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriskyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepo _repo;
        private readonly IJwtServices _jwtService;

        public AuthController(IUserRepo repo, IJwtServices jwtService)
        {
            _repo = repo;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = model.Email.Trim().ToLower();


            var user = new User
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = email,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                Role = "User", 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            var result = await _repo.RegisterUserAsync(user);

            if (!result)
                return Conflict(new { message = "Email is already registered." });

            return Ok(new { message = "Registration successful." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _repo.GetUserByEmailAsync(model.Email.Trim().ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Role
                }
            });
        }
    }
}