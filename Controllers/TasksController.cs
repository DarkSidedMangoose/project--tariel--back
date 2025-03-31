using System.Reflection;
using System.Security.Claims;
using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Models;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.SignalIR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;



namespace ASP.MongoDb.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]

    public class TasksController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IUserRepository _userRepository;
        private ITasksRepository _tasksRepository;

        public TasksController(ITasksRepository tasksRepository, IUserRepository userRepository, IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
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
        .Select(d => new {d.fullname, d.diversion, d.imgUrl,d.id}).ToList();
            
            return Ok(usersDedicatedForUser);
        }
        [HttpPut("giveTask")]
        public async Task<IActionResult> GiveTask([FromBody] SentTaskRequest taskRequest)
        {
            if (taskRequest == null)
            {
                return BadRequest("Task request cannot be null");
            }

            var taskId = taskRequest.taskId;
            var receiverId = taskRequest.receiveUserId;
            if (string.IsNullOrEmpty(taskId) || string.IsNullOrEmpty(receiverId))
            {

                Console.WriteLine(receiverId);
                return BadRequest($"TaskId and ReceiverId are required {taskId} {receiverId}");
            }

            try
            {
            var currentUser = await GetValidUserAsync();
                if(currentUser == null)
                {
                    return Unauthorized("Current User is invalid or Not authenticated");
                }
            var receiverUser = await _userRepository.GetByIdAsync(receiverId);
                if(receiverUser == null)
                {
                    return NotFound($"Receiver user with ID {receiverId} not found");
                }
            var taskById = await _tasksRepository.GetByIdAsync(taskId);
                if(taskById == null)
                {
                    return NotFound($"The Task with ID {taskById} not found");
                }

                var levelOfSender = $"level{currentUser.level}";
                var levelOfReceiver = $"level{receiverUser.level}";

                var senderProperty = taskById.dataFlow.GetType().GetProperty(levelOfSender);
                if (senderProperty != null)
                {
                    var senderLevel = (Tasks.Level)senderProperty.GetValue(taskById.dataFlow);
                    if(senderLevel != null)
                    {
                        senderLevel.userId = currentUser.id;
                        senderLevel.status = "onPending";
                        senderProperty.SetValue(taskById.dataFlow, senderLevel);
                    }
                }

                var receiverProperty = taskById.dataFlow.GetType().GetProperty(levelOfReceiver);
                if(receiverProperty != null)
                {
                    var receiverLevel = (Tasks.Level)receiverProperty.GetValue(taskById.dataFlow);
                    if(receiverLevel != null)
                    {
                        receiverLevel.userId = receiverUser.id;
                        receiverLevel.status = "onGoing";
                        receiverLevel.fromUserId = currentUser.id;
                        receiverProperty.SetValue(taskById.dataFlow, receiverLevel);
                    }
                }

                    if(taskById.id != null)
                {

                await _tasksRepository.UpdateAsync(taskById.id, taskById);
                }
                await SendData(receiverUser.id);
                

                return Ok("everything work well");
            }catch (Exception ex)
            {
                return StatusCode(500, $"internal server error: {ex.Message}");
            }
            
            
        }
        [HttpPut("endTask")]
        public async Task<IActionResult> EndTask([FromBody] OverTaskRequest OvertaskRequest)
        {

            Console.WriteLine(OvertaskRequest);
            if (OvertaskRequest == null)
            {
                return BadRequest("Task request cannot be null");
            }
            try
            {
                var taskId = OvertaskRequest.taskId;
                var task = await _tasksRepository.GetByIdAsync(taskId);
                var userId = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (userId != null)
                {
                    var userInfo = await _userRepository.GetByIdAsync(userId);
                    var userLevel = $"level{userInfo.level}";
                    var receiverLevel = $"level{userInfo.level+1}";

                    

                    if (task != null && userLevel != null && receiverLevel != null)
                    {
                        var senderProperty = task.dataFlow.GetType().GetProperty(userLevel);

                        var senderLevelValue = (Tasks.Level)senderProperty.GetValue(task.dataFlow);
                        if(senderLevelValue != null)
                        {
                            senderLevelValue.status = "waitApproval";
                            senderProperty.SetValue(task.dataFlow, senderLevelValue);
                        }

                        var receiverProperty = task.dataFlow.GetType().GetProperty(receiverLevel);

                        if(receiverProperty != null)
                        {
                            var receiverLevelValue = (Tasks.Level)receiverProperty.GetValue(task.dataFlow);
                            if(receiverLevelValue != null)
                            {
                                receiverLevelValue.status = "pendingApproval";
                                receiverProperty.SetValue(task.dataFlow, receiverLevelValue);

                                await _tasksRepository.UpdateAsync(task.id, task);
                                await SendData(receiverLevelValue.userId);
                                

                            }
                        
                        }

                    }

                }
                else
                {
                    Console.WriteLine("User ID is not available.");
                }





                return Ok("everything work well");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"internal server error: {ex.Message}");
            }


        }
        [HttpPut("declineTask")]
        public async Task<IActionResult> DeclinedTask([FromBody] OverTaskRequest OvertaskRequest)
        {

            Console.WriteLine(OvertaskRequest);
            if (OvertaskRequest == null)
            {
                return BadRequest("Task request cannot be null");
            }
            try
            {
                var taskId = OvertaskRequest.taskId;
                var task = await _tasksRepository.GetByIdAsync(taskId);
                var userId = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (userId != null)
                {
                    var userInfo = await _userRepository.GetByIdAsync(userId);
                    var userLevel = $"level{userInfo.level}";
                    var receiverLevel = $"level{userInfo.level - 1}";



                    if (task != null && userLevel != null && receiverLevel != null)
                    {
                        var senderProperty = task.dataFlow.GetType().GetProperty(userLevel);

                        var senderLevelValue = (Tasks.Level)senderProperty.GetValue(task.dataFlow);
                        if (senderLevelValue != null)
                        {
                            senderLevelValue.status = "onPending";
                            senderProperty.SetValue(task.dataFlow, senderLevelValue);
                        }

                        var receiverProperty = task.dataFlow.GetType().GetProperty(receiverLevel);

                        if (receiverProperty != null)
                        {
                            var receiverLevelValue = (Tasks.Level)receiverProperty.GetValue(task.dataFlow);
                            if (receiverLevelValue != null)
                            {
                                receiverLevelValue.status = "onGoing";
                                receiverProperty.SetValue(task.dataFlow, receiverLevelValue);

                                await _tasksRepository.UpdateAsync(task.id, task);
                                await SendData(receiverLevelValue.userId);
                                
                            }
                        }

                    }

                }
                else
                {
                    Console.WriteLine("User ID is not available.");
                }





                return Ok("everything work well");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"internal server error: {ex.Message}");
            }


        }
        //to get users access dedicated for them tasks
        private async Task<IActionResult> GetFilteredTasksAsync(string status)
        {
            var user = await GetValidUserAsync();
            if(user == null)
            {
                return BadRequest("userId is missing");
            }
            var userLevel = user.level;
            var userId = user.id;
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

                return levelData != null && levelData.status == status && levelData.userId == userId;
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


        public async Task SendData(string userId)
        {
            var connectionId = NotificationHub.GetConnectionId(userId);
            if (!string.IsNullOrEmpty(connectionId))
            {
                string uniqueId = Guid.NewGuid().ToString();
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveData", $"Update database - {uniqueId}");
            }

        }


        public async Task<Users> GetValidUserAsync()
        {
            var userId = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return null; // Return null if user ID is missing
            }

            // Directly return the user, no need for if-else here
            return await _userRepository.GetByIdAsync(userId);
        }
    }
}
