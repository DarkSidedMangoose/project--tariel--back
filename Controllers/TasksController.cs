using System.Threading.Tasks;
using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Models;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Services;
using ASP.MongoDb.API.SignalIR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;




namespace ASP.MongoDb.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]

    public class TasksController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IUserRepository _userRepository;
        private ITasksRepository _tasksRepository;
        private readonly RedisExample _redisExample;



        public TasksController(ITasksRepository tasksRepository, IUserRepository userRepository, IHubContext<NotificationHub> hubContext, RedisExample redisExample)
        {
            _hubContext = hubContext;
            _userRepository = userRepository;
            _tasksRepository = tasksRepository;
            _redisExample = redisExample;
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
        [HttpGet("receiveApproval")]
        public async Task<IActionResult> GetReceiveApproval()
        {
            return await GetFilteredTasksAsync("receiveApproval");
        }
        [HttpGet("waitApproval")]
        public async Task<IActionResult> GetWaitApproval()
        {
            return await GetFilteredTasksAsync("waitApproval");
        }

        [HttpGet("getCommentDesireDatas")]
        public async Task<IActionResult> GetCommentDesireDatas([FromQuery] string taskId)
        {
            if(string.IsNullOrEmpty(taskId))
            {
                return BadRequest("Task Id Is missing");
            }
            var task = await _tasksRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                return BadRequest("There is not any task according to that id, some kind of problem");
            }
            var lastLog = task.dataLogs?.LastOrDefault(); // Access the last object safely


            return Ok(new
            {
                senterName = lastLog.receiverName,
                receiverName = lastLog.addedByName
            });


        }
        [HttpGet("getSpecificTaskData")]
        public async Task<IActionResult> GetSpecificTask([FromQuery] string taskId)
        {
            if(string.IsNullOrEmpty(taskId) )
            {
               
               return BadRequest("there is Error not taskId");
            }

            var specificTask = await _tasksRepository.GetByIdAsync(taskId);
            if(specificTask == null)
            {
                return BadRequest("there is not that task something error ");
            }

            return Ok(specificTask);
        }

        // get users for the tasks

        [HttpGet("getUsersForTasks")]
        public async Task<IActionResult> GetSpecificUsersForGiveTasks()
        {
            var user = await GetValidUserAsync();
            var users = await _userRepository.GetAllAsync();
            var usersDedicatedForUser = users.Where(d => d.level == user.level - 1 &&  (d.level == 6 || user.department == null || d.department == user.department)).Select(d => new {d.fullname, d.diversion, d.imgUrl,d.id, d.position, d.department}).ToList();
            
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
                    var senderLevel = senderProperty.GetValue(taskById.dataFlow) as Tasks.Level;
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

                taskById.dataLogs.Add(new Tasks.TaskLogEntry
                {
                    level = currentUser.level,
                    timestamp = DateTime.Now.ToString(),
                    addedByName = currentUser.fullname,
                    addedById = currentUser.id,
                    description = "დავალების გაცემა",
                    receiverName = receiverUser.fullname,
                    receiverId = receiverUser.id,
                    imgUrl = currentUser.imgUrl
                });
                if (taskById.id != null)
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
        public async Task<IActionResult> EndTask([FromBody] OverTaskRequest overtaskRequest)
        {
            if (overtaskRequest == null)
            {
                return BadRequest("Task request cannot be null");
            }
            try
            {
                var taskId = overtaskRequest.taskId;
                var taskById = await _tasksRepository.GetByIdAsync(taskId);
                var userId = await getUserIdViaSessionToken();

                Console.WriteLine($"{userId} userId");

                if (userId != null)
                {
                    var userInfo = await _userRepository.GetByIdAsync(userId);
                    var userLevel = $"level{userInfo.level}";
                    var receiverLevel = $"level{userInfo.level+1}";
                    Console.WriteLine(userInfo.level);
                    

                    if (taskById != null && userLevel != null && receiverLevel != null)
                    {
                        var senderProperty = taskById.dataFlow.GetType().GetProperty(userLevel);

                        var senderLevelValue = (Tasks.Level)senderProperty.GetValue(taskById.dataFlow);
                        if(senderLevelValue != null)
                        {
                            senderLevelValue.status = "waitApproval";
                            senderProperty.SetValue(taskById.dataFlow, senderLevelValue);
                        }

                        var receiverProperty = taskById.dataFlow.GetType().GetProperty(receiverLevel);
                        if (receiverProperty != null)
                        {
                            var receiverLevelValue = (Tasks.Level)receiverProperty.GetValue(taskById.dataFlow);
                            if(receiverLevelValue != null)
                            {
                                receiverLevelValue.status = "receiveApproval";
                                receiverProperty.SetValue(taskById.dataFlow, receiverLevelValue);

                        var receiverUser = await _userRepository.GetByIdAsync(receiverLevelValue.userId);
                                taskById.dataLogs.Add(new Tasks.TaskLogEntry
                                {
                                    level = userInfo.level,
                                    timestamp = DateTime.Now.ToString(),
                                    addedByName = userInfo.fullname,
                                    addedById = userInfo.id,
                                    description = "დავალების დასრულების მოთხოვნა",
                                    receiverName = receiverUser.fullname,
                                    receiverId = receiverUser.id,
                                    imgUrl = userInfo.imgUrl
                                });

                                await _tasksRepository.UpdateAsync(taskById.id, taskById);

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

        [HttpPost("addNewTask")]

        public async Task<IActionResult> AddNewTask([FromBody] AddTaskRequest request)
        {
            if(request.FirstArgument == null)
            {
                return BadRequest("Problem Tasks is null");
            }

            var task = request.FirstArgument;

            var userLevel = $"level{request.SecondArgument.level}";
           
            if(task != null && task.dataFlow != null)
            {

            var propertyName = task.dataFlow.GetType().GetProperty(userLevel);
                if(propertyName != null)
                {

                    var createrLevelValue = propertyName.GetValue(task.dataFlow) as Tasks.Level;
                if (createrLevelValue != null)
            {
                createrLevelValue.userId = request.SecondArgument.id;
                createrLevelValue.status = "onGoing";
                propertyName.SetValue(task.dataFlow, createrLevelValue);


            }
                }
                

               
            }


            await _tasksRepository.CreateAsync(task);
            
            return Ok("data has upload succesfully");
        }

        [HttpPut("declineTask")]
        public async Task<IActionResult> DeclinedTask([FromBody] DeclineTaskRequest declineTaskRequest)
        {

            Console.WriteLine(declineTaskRequest);
            if (declineTaskRequest == null)
            {
                return BadRequest("Task request cannot be null");
            }
            try
            {
                var taskId = declineTaskRequest.taskId;
                var taskById = await _tasksRepository.GetByIdAsync(taskId);
                var userId = await getUserIdViaSessionToken();

                
                if (userId != null)
                {
                    var userInfo = await _userRepository.GetByIdAsync(userId);
                    var userLevel = $"level{userInfo.level}";
                    var receiverLevel = $"level{userInfo.level - 1}";



                    if (taskById != null && userLevel != null && receiverLevel != null)
                    {
                        var senderProperty = taskById.dataFlow.GetType().GetProperty(userLevel);

                        var senderLevelValue = (Tasks.Level)senderProperty.GetValue(taskById.dataFlow);
                        if (senderLevelValue != null)
                        {
                            senderLevelValue.status = "onPending";
                            senderProperty.SetValue(taskById.dataFlow, senderLevelValue);
                        }

                        var receiverProperty = taskById.dataFlow.GetType().GetProperty(receiverLevel);

                        if (receiverProperty != null)
                        {
                            var receiverLevelValue = (Tasks.Level)receiverProperty.GetValue(taskById.dataFlow);
                            if (receiverLevelValue != null)
                            {
                                receiverLevelValue.status = "onGoing";
                                receiverProperty.SetValue(taskById.dataFlow, receiverLevelValue);

                                var receiverUser = await _userRepository.GetByIdAsync(receiverLevelValue.userId);

                                taskById.dataLogs.Add(new Tasks.TaskLogEntry
                                {
                                    level = userInfo.level,
                                    timestamp = DateTime.Now.ToString(),
                                    addedByName = userInfo.fullname,
                                    addedById = userInfo.id,
                                    description = "დავალების დასრულების მოთხოვნის უარყოფა",
                                    receiverName = receiverUser.fullname,
                                    receiverId = receiverUser.id,
                                    imgUrl = userInfo.imgUrl,
                                    comment = declineTaskRequest.comment
                                });

                                await _tasksRepository.UpdateAsync(taskById.id, taskById);
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
                t.objectIdentifierData?.identifyCode,
                t.objectIdentifierData?.fullName,
                t.addresses?.region,
                t.addresses?.factAddress,
                t.payerInfo?.iurPersonIncomeRotation,
                t.activityinformation?.workingDescription,
                t.activityinformation?.riskLevel,
                t.dataLogs
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

       public async Task<string> getUserIdViaSessionToken()
        {
            var sessionToken =  Request.Cookies["session-token"];
            var userId = await _redisExample.GetUserIdBySessionToken(sessionToken);

            Console.WriteLine(userId);

            if (string.IsNullOrEmpty(userId)) {
                return null;
            }

            return userId;
        }

        public async Task<Users> GetValidUserAsync()
        {
            var sessionToken = Request.Cookies["session-token"];
        if (string.IsNullOrEmpty(sessionToken))
        {
                return null;
            }

            var userId = await _redisExample.GetUserIdBySessionToken(sessionToken);

            if (string.IsNullOrEmpty(userId))
            {
                return null; // Return null if user ID is missing
            }

            return await _userRepository.GetByIdAsync(userId);
        }
    }
}
