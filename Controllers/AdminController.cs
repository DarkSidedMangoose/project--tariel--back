using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        [Authorize(Policy = "AdminPolicy")] // Only users with "admin" role can access
        [HttpGet("admin-data")]
        public IActionResult AdminData()
        {
            // If unauthorized, return the token and its role
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new
                {
                    message = "Unauthorized",
                    role = User.FindFirst("role")?.Value // Get the user's role from the JWT token
                });
            }

            return Ok("This data is for admins only");
        }
    }
}