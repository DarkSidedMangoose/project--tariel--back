using System.Security.Claims;
using ASP.MongoDb.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;


namespace ASP.MongoDb.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]

    public class TasksController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private ITasksRepository _tasksRepository;

        public TasksController(ITasksRepository tasksRepository, IUserRepository userRepository)
        {
            _userRepository = userRepository;
            _tasksRepository = tasksRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
           
            var userId = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) {
                return Unauthorized(new { message = "Invalid or missing token" });
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if(user == null)
            {
                return NotFound(new { message = "User Not Found" });
            }

            var userLevel = user.level;


            var tasks = await _tasksRepository.GetAllAsync();

            return Ok(userLevel);
        }
    }
}
