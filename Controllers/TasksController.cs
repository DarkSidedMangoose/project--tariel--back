using System.Security.Claims;
using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.OpenApi.Any;


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

        [HttpGet("onGoing")]
        public async Task<IActionResult> GetOnGoing()
        {
            var userId = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            // Check for a valid user ID
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid or missing token" });
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User Not Found" });
            }

            var userLevel = user.level;
            var username = user.username;

            var tasks = await _tasksRepository.GetAllAsync();
            var tasksDataflowSpecificUser = $"level{userLevel}";

            // Filter tasks based on the specified level and "status == onGoing"
            var filteredTasks = tasks.Where(t =>
            {
                // Access the `dataFlow` property for the task
                var dataFlow = t.dataFlow;
                var propertyInfo = dataFlow.GetType().GetProperty(tasksDataflowSpecificUser);

                // Dynamically get the value of the property
                var levelData = propertyInfo?.GetValue(dataFlow) as Tasks.Level;

                // Check if the "status" field is "onGoing"
                return levelData != null && levelData.status == "onGoing" && levelData.username == username;
            }).ToList();


            var result = filteredTasks.Select(t => new
            {
                t.id,
                t.identifyCode,
                t.wholeName,
                t.region,
                t.fizAddress,
                t.turnover,
                t.jobType,
                t.riskLevel
            }).ToList();

            // Return the filtered tasks
            return Ok(result);
        }
        [HttpGet("onPending")]
        public async Task<IActionResult> GetOnPending()
        {
            var userId = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            // Check for a valid user ID
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid or missing token" });
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User Not Found" });
            }

            var userLevel = user.level;
            var username = user.username;

            Console.WriteLine(username);
            var tasks = await _tasksRepository.GetAllAsync();
            var tasksDataflowSpecificUser = $"level{userLevel}";

            // Filter tasks based on the specified level and "status == onGoing"
            var filteredTasks = tasks.Where(t =>
            {
                // Access the `dataFlow` property for the task
                var dataFlow = t.dataFlow;
                
                var propertyInfo = dataFlow.GetType().GetProperty(tasksDataflowSpecificUser);
                

                // Dynamically get the value of the property
                var levelData = propertyInfo?.GetValue(dataFlow) as Tasks.Level;

                // Check if the "status" field is "onGoing"
                
                return levelData != null && levelData.status == "onPending" && levelData.username == username;
            }).ToList();


            var result = filteredTasks.Select(t => new
            {
                t.id,
                t.identifyCode,
                t.wholeName,
                t.region,
                t.fizAddress,
                t.turnover,
                t.jobType,
                t.riskLevel,
                
            }).ToList();

            // Return the filtered tasks
            return Ok(result);
        }

    }
}
