// AuthController.cs
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Entities;

namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration; // For accessing the secret key

        public AuthController(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            // Validate the user's credentials
            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.username == loginRequest.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.passwordHash))
            {
                return Unauthorized("Invalid username or password.");
            }

            // Generate a JWT token for the authenticated user
            var token = GenerateJwtToken(user);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Set to true for HTTPS in production
                SameSite = SameSiteMode.None, // Required for cross-origin requests
                Path = "/",
                Expires = DateTime.UtcNow.AddHours(1),
            };

            Response.Cookies.Append("auth-token", token, cookieOptions);



            return Ok(new { message = "Authentication succesful!" });
        }

        private string GenerateJwtToken(Users user)
        {
            // Add the user's role as a standard "role" claim
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.id),  // User's id
                new Claim(ClaimTypes.Role, user.role) // Role of the user
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}