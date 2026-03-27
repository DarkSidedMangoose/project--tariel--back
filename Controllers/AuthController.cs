// AuthController.cs
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration; // For accessing the secret key
        private readonly IDistributedCache _cache;

        public AuthController(IUserRepository userRepository, IConfiguration configuration, IDistributedCache cache)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }
        [HttpPost("eraseSessionToken")]
        public async Task<IActionResult> EraseSessionToken([FromServices] IDistributedCache cache)
        {
            var sessionToken = HttpContext.Request.Cookies["session-token"];
            if(string.IsNullOrEmpty(sessionToken))
            {
                return Unauthorized("session Token is missed from cookies");
            }
            
            var userId = await cache.GetStringAsync($"session:{sessionToken}");
            if(string.IsNullOrEmpty(userId))
            {
                return Unauthorized("sessionToken is missed from redis");
            }

            await cache.RemoveAsync($"session:{sessionToken}");

            
                // Remove cookie by setting it with an expired date
                HttpContext.Response.Cookies.Append("session-token", "", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(-1) // Expired in the past
                });
               
            return Ok(new { message = "session token removed succesfully" });
        } 
        [HttpPost("updateSessionToken")]
        public async Task<IActionResult> UpdateSessionToken([FromServices] IDistributedCache cache) 

        {
            var sessionToken = HttpContext.Request.Cookies["session-token"];
            if(string.IsNullOrEmpty(sessionToken))
            {
                return Unauthorized("Session token Is Missing");
            }
            var userId = await cache.GetStringAsync($"session:{sessionToken}");
            if(userId == null)
            {
                return Unauthorized("Session token not found or expired");
            }
            await cache.RemoveAsync($"session:{sessionToken}");

            var newToken = Guid.NewGuid().ToString();
            var expiration = TimeSpan.FromMinutes(30);
            await cache.SetStringAsync($"session:{newToken}", userId,
                new DistributedCacheEntryOptions
                {

                    AbsoluteExpirationRelativeToNow = expiration
                });

            HttpContext.Response.Cookies.Append("session-token", newToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict, // Adjust as needed
                Expires = DateTime.UtcNow.Add(expiration)
            });

            return Ok(new { message= "session token updated succesfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            
            // Validate the user's credentials
            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.username == loginRequest.Username);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.passwordHash))
            {
                
                return Unauthorized(users);

                
            }
            //new unique session token
            var uniqueId = Guid.NewGuid().ToString();
            
            // Set the session in Redis
            var expiration = TimeSpan.FromMinutes(30);
            if(string.IsNullOrEmpty(uniqueId) || string.IsNullOrEmpty(user.id))
            {
                return NotFound("user id or unique id is lost");
            }
            await _cache.SetStringAsync($"session:{uniqueId}", user.id, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });


            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Use HTTPS in production
                SameSite = SameSiteMode.Strict, // Adjust as needed
                Expires = DateTime.UtcNow.Add(expiration)
            };

            Response.Cookies.Append("session-token", uniqueId, cookieOptions);


            return Ok(new { message = "Authentication succesfull" });

        }
    }
}















//var token = GenerateJwtToken(user);

//var cookieOptions = new CookieOptions
//{
//    HttpOnly = true,
//    Secure = true, // Set to true for HTTPS in production
//    SameSite = SameSiteMode.None, // Required for cross-origin requests
//    Path = "/",
//    Expires = DateTime.UtcNow.AddHours(1),
//};

//Response.Cookies.Append("auth-token", token, cookieOptions);



//return Ok(new { message = "Authentication succesful!" });
//        }

//        private string GenerateJwtToken(Users user)
//{
//    // Add the user's role as a standard "role" claim
//    var claims = new[] {
//                new Claim(JwtRegisteredClaimNames.Sub, user.id),  // User's id
//                new Claim(ClaimTypes.Role, user.role) // Role of the user
//            };

//    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
//    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//    var token = new JwtSecurityToken(
//        issuer: _configuration["JwtSettings:Issuer"],
//        audience: _configuration["JwtSettings:Audience"],
//        claims: claims,
//        expires: DateTime.Now.AddHours(1),
//        signingCredentials: creds);

//    return new JwtSecurityTokenHandler().WriteToken(token);
//}