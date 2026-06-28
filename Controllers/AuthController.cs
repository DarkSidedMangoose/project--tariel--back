// AuthController.cs
using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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
            if (string.IsNullOrEmpty(sessionToken))
            {
                return Unauthorized("Session token is missing");
            }

            var userId = await cache.GetStringAsync($"session:{sessionToken}");
            if (userId == null)
            {
                return Unauthorized("Session token not found or expired");
            }

            await cache.RemoveAsync($"session:{sessionToken}");

            // Generate secure session token
            var newToken = GenerateSecureToken();
            var expiration = TimeSpan.FromMinutes(30);

            await cache.SetStringAsync($"session:{newToken}", userId, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });

            HttpContext.Response.Cookies.Append("session-token", newToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.Add(expiration)
            });

            return Ok(new { message = "Session token updated successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest,
                                       [FromServices] LoginAttemptService attemptService)
        {
            if (await attemptService.IsLockedOutAsync(loginRequest.Username))
                return Unauthorized("მომხმარებელი დაბლოკილია 15 წუთით.");

            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.username == loginRequest.Username);

            if (user == null || user.status == "არა აქტიური" || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.passwordHash))
            {
                if(!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.passwordHash)) {
                    attemptService.RegisterFailedAttemptAsync(loginRequest.Username);
                }
                return Unauthorized("შეყვანილი User ან Password არასწორია");
            }

            var code = new Random().Next(100000, 999999).ToString();

            await _cache.SetStringAsync($"mfa:{user.id}", code,
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3) });

            // Send MFA code via Gmail
            await SendMfaCodeAsync(user.email, code);


            // Generate secure session token
            //var uniqueId = GenerateSecureToken();

            //var expiration = TimeSpan.FromMinutes(30);
            //await _cache.SetStringAsync($"session:{uniqueId}", user.id, new DistributedCacheEntryOptions
            //{
            //    AbsoluteExpirationRelativeToNow = expiration
            //});

            //var cookieOptions = new CookieOptions
            //{
            //    HttpOnly = true,
            //    Secure = true,
            //    SameSite = SameSiteMode.Strict,
            //    Expires = DateTime.UtcNow.Add(expiration)
            //};

            //Response.Cookies.Append("session-token", uniqueId, cookieOptions);

            return Ok(new
            {
                message = "MFA Code sent to email",
                userId = user.id,
                userEmail = user.email
            });
        }

        public class MfaRequest
        {
            public string UserId { get; set; }     // მომხმარებლის უნიკალური ID
            public string Code { get; set; }       // MFA კოდი, რომელიც მომხმარებელმა შეიყვანა
        }

        [HttpPost("verify-mfa")]
        public async Task<IActionResult> VerifyMfa([FromBody] MfaRequest request)
        {
            var code = await _cache.GetStringAsync($"mfa:{request.UserId}");
            if (code == null || code != request.Code)
                return Unauthorized("კოდი არასწორია, ან ამოიწურა მისი მოქმედების დრო");

            // Generate secure session token
            var uniqueId = GenerateSecureToken();
            var expiration = TimeSpan.FromMinutes(30);

            await _cache.SetStringAsync($"session:{uniqueId}", request.UserId,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.Add(expiration)
            };

            Response.Cookies.Append("session-token", uniqueId, cookieOptions);

            return Ok(new { message = "Authentication successful" });
        }
        private string GenerateSecureToken(int size = 32)
        {
            // 32 bytes = 256-bit token
            byte[] tokenBytes = new byte[size];
            RandomNumberGenerator.Fill(tokenBytes);
            return Convert.ToBase64String(tokenBytes);
        }

        private async Task SendMfaCodeAsync(string toEmail, string code)
        {
            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("topuria2074@gmail.com", "chsy qeef wwtj dohp"),
                EnableSsl = true
            };

            var mail = new MailMessage("topuria2074@gmail.com", toEmail)
            {
                Subject = "Your MFA Code",
                Body = $"Your verification code is {code}"
            };

            await smtp.SendMailAsync(mail);
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