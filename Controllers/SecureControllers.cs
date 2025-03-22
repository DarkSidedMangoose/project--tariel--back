using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecureController : ControllerBase
    {
        [Authorize(Policy = "AdminPolicy")]
        [HttpGet("secure-data")]
        public IActionResult SecureData()
        {
            return Ok("This is protected data.");
        }
    }
}
