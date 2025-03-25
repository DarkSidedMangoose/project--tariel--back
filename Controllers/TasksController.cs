using System.Security.Claims;
using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



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
            return await GetFilteredTasksAsync("onGoing");
        }
        [HttpGet("onPending")]
        public async Task<IActionResult> GetOnPending()
        {
            return await GetFilteredTasksAsync("onPending");
        }
        [HttpGet("pendingApproval")]
        public async Task<IActionResult> GetPendingApproval()
        {
            return await GetFilteredTasksAsync("pendingApproval");
        }
        [HttpGet("waitApproval")]
        public async Task<IActionResult> GetWaitApproval()
        {
            return await GetFilteredTasksAsync("waitApproval");
        }

        // get users for the tasks
        [HttpGet("getUsersForTasks")]
        public async Task<IActionResult> GetSpecificUsersForGiveTasks()
        {
            var user = await GetValidUserAsync();
            var users = await _userRepository.GetAllAsync();

            var usersDedicatedForUser = users
        .Where(d => d.level == user.level - 1)
        .Select(d => new {d.fullname, d.diversion, d.imgUrl}).ToList();
            
            return Ok(usersDedicatedForUser);
        }
        //to get users access dedicated for them tasks
        private async Task<IActionResult> GetFilteredTasksAsync(string status)
        {
            var user = await GetValidUserAsync();
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

                return levelData != null && levelData.status == status && levelData.username == username;
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

        public async Task<Users> GetValidUserAsync()
        {
            var UserId = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(UserId))
            {
                return null;
            }
            var user = await _userRepository.GetByIdAsync(UserId);
            return user;
        }
    }
}
