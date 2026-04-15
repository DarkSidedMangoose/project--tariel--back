using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Models;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Services;
using ASP.MongoDb.API.SignalIR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ASP.MongoDb.API.Entities.Tasks;

namespace ASP.MongoDb.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IUserRepository _userRepository;
        private readonly ITasksRepository _tasksRepository;
        private readonly RedisExample _redisExample;
        private readonly IFileService _fileService;

        

        public TasksController(
            ITasksRepository tasksRepository,
            IUserRepository userRepository,
            IHubContext<NotificationHub> hubContext,
            RedisExample redisExample,
            IFileService fileService)
           
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tasksRepository = tasksRepository ?? throw new ArgumentNullException(nameof(tasksRepository));
            _redisExample = redisExample ?? throw new ArgumentNullException(nameof(redisExample));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }


        [HttpGet("onGoing")]
        public async Task<IActionResult> GetOnGoing(int skip, int take) => await GetFilteredTasksAsync("onGoing", skip, take);

        [HttpGet("onPending")]
        public async Task<IActionResult> GetOnPending(int skip, int take) => await GetFilteredTasksAsync("onPending", skip, take);

        [HttpGet("receiveApproval")]
        public async Task<IActionResult> GetReceiveApproval(int skip, int take) => await GetFilteredTasksAsync("receiveApproval", skip, take);

        [HttpGet("waitApproval")]
        public async Task<IActionResult> GetWaitApproval(int skip, int take) => await GetFilteredTasksAsync("waitApproval", skip, take);

        [HttpGet("getCommentDesireDatas")]
        public async Task<IActionResult> GetCommentDesireDatas([FromQuery] string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
                return BadRequest("Task Id Is missing");

            var task = await _tasksRepository.GetByIdAsync(taskId);
            if (task == null)
                return BadRequest("There is not any task according to that id");

            var lastLog = task.dataLogs?.LastOrDefault();

            return Ok(new
            {
                senterName = lastLog?.receiverName,
                receiverName = lastLog?.addedByName
            });
        }

        [HttpGet("getSpecificTaskData")]
        public async Task<IActionResult> GetSpecificTask([FromQuery] string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
                return BadRequest("there is Error not taskId");

            var specificTask = await _tasksRepository.GetByIdAsync(taskId);
            if (specificTask == null)
                return BadRequest("there is not that task something error");

            return Ok(specificTask);
        }

        public class GetFilteredDatas
        {
            public SearchInterface filterData { get; set; } = null!; // or make it nullable if appropriate
            public string choosedOption { get; set; } = null!;
            public int skip { get; set; } 
            public int take { get; set; } 
        }

        
        [HttpPost("getFilteredData")]
        public async Task<IActionResult> GetFilteredData([FromBody] GetFilteredDatas? filteredData)
        {
            if (filteredData == null || filteredData.filterData == null)
                return BadRequest("there is problem");

            var user = await GetValidUserAsync();
            if (user == null)
                return Unauthorized("User not found");

            // Build MongoDB filter dynamically
            var filterBuilder = Builders<Tasks>.Filter;
            var filters = new List<FilterDefinition<Tasks>>();

            filters.Add(filterBuilder.Eq($"dataFlow.level{user.level}.status", filteredData.choosedOption));
            filters.Add(filterBuilder.Eq($"dataFlow.level{user.level}.userId", user.id));

            if (!string.IsNullOrEmpty(filteredData.filterData.ObjectIdentifierData.FullName))
                filters.Add(filterBuilder.Regex("objectIdentifierData.fullName",
                    new MongoDB.Bson.BsonRegularExpression(filteredData.filterData.ObjectIdentifierData.FullName, "i")));

            if (!string.IsNullOrEmpty(filteredData.filterData.ObjectIdentifierData.IdentifyCode))
                filters.Add(filterBuilder.Regex("objectIdentifierData.identifyCode",
                    new MongoDB.Bson.BsonRegularExpression(filteredData.filterData.ObjectIdentifierData.IdentifyCode, "i")));

            // …repeat for other fields (ActivityForm.Form, Addresses.Region, etc.)
            // For ranges like EmployeeMin/EmployeeMax:
            filters.Add(filterBuilder.Gte("payerInfo.employedCount", filteredData.filterData.PayerInfo.EmployeeMin));
            filters.Add(filterBuilder.Lte("payerInfo.employedCount", filteredData.filterData.PayerInfo.EmployeeMax));

            var combinedFilter = filterBuilder.And(filters);
            var levelString = $"level{user.level}";

            // Call repository with pagination
            var tasks = await _tasksRepository.GetPagedTasksAsync(combinedFilter, filteredData.skip, filteredData.take, levelString);

            var results = tasks.Select(t => new
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
            });

            return Ok(results);
        }

        [HttpGet("getUsersForTasks")]
        public async Task<IActionResult> GetSpecificUsersForGiveTasks()
        {
            var user = await GetValidUserAsync();
            if (user == null)
                return Unauthorized("Current user is invalid or not authenticated");

            var users = await _userRepository.GetAllAsync();

            var usersDedicatedForUser = users
                .Where(d => d.level == user.level - 1 &&
                           (d.level == 6  || (d.department == user.department && (d.level == 5 || d.level == 4 )) || (d.department == user.department && d.level == 3 && d.diversion == user.diversion ) || (d.department == user.department && d.level < 3 && d.diversion == user.diversion && d.section == user.section )) ) 
                .Select(d => new
                {
                    d.fullname,
                    d.diversion,
                    d.imgUrl,
                    d.id,
                    d.position,
                    d.department
                })
                .ToList();

            return Ok(usersDedicatedForUser);
        }

        [HttpPut("giveTask")]
        public async Task<IActionResult> GiveTask([FromBody] SentTaskRequest? taskRequest)
        {
            if (taskRequest == null)
                return BadRequest("Task request cannot be null");

            var taskId = taskRequest.taskId;
            var receiverId = taskRequest.receiveUserId;

            if (string.IsNullOrEmpty(taskId) || string.IsNullOrEmpty(receiverId))
                return BadRequest($"TaskId and ReceiverId are required. Received: TaskId={taskId}, ReceiverId={receiverId}");

            try
            {
                var currentUser = await GetValidUserAsync();
                if (currentUser == null)
                    return Unauthorized("Current User is invalid or Not authenticated");

                var receiverUser = await _userRepository.GetByIdAsync(receiverId);
                if (receiverUser == null)
                    return NotFound($"Receiver user with ID {receiverId} not found");

                var taskById = await _tasksRepository.GetByIdAsync(taskId);
                if (taskById == null)
                    return NotFound($"The Task with ID {taskId} not found");

                var levelOfSender = $"level{currentUser.level}";
                var levelOfReceiver = $"level{receiverUser.level}";

                // Update sender level
                var senderProperty = taskById.dataFlow.GetType().GetProperty(levelOfSender);
                if (senderProperty != null)
                {
                    if (senderProperty.GetValue(taskById.dataFlow) is Tasks.Level senderLevel)
                    {
                        senderLevel.userId = currentUser.id;
                        senderLevel.status = "onPending";
                        senderLevel.timeSpan = DateTime.UtcNow;
                        senderProperty.SetValue(taskById.dataFlow, senderLevel);
                    }
                }

                // Update receiver level
                var receiverProperty = taskById.dataFlow.GetType().GetProperty(levelOfReceiver);
                if (receiverProperty != null)
                {
                    if (receiverProperty.GetValue(taskById.dataFlow) is Tasks.Level receiverLevel)
                    {
                        receiverLevel.userId = receiverUser.id;
                        receiverLevel.status = "onGoing";
                        receiverLevel.fromUserId = currentUser.id;
                        receiverLevel.timeSpan = DateTime.UtcNow;
                        receiverProperty.SetValue(taskById.dataFlow, receiverLevel);
                    }
                }

                taskById.dataLogs ??= new List<Tasks.TaskLogEntry>(); // ensure list is initialized

                var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
                var georgiaTime = TimeZoneInfo.ConvertTime(DateTime.Now, georgiaTimeZone);

                taskById.dataLogs.Add(new Tasks.TaskLogEntry
                {
                    level = currentUser.level,
                    timestamp = georgiaTime.ToString("M/d/yyyy, h:mm:ss tt"),
                    addedByName = currentUser.fullname,
                    addedById = currentUser.id,
                    description = "დავალების გაცემა",
                    receiverName = receiverUser.fullname,
                    receiverId = receiverUser.id,
                    imgUrl = currentUser.imgUrl
                });

                await _tasksRepository.UpdateAsync(taskById.id!, taskById); // id! after null check above
                await SendData(receiverUser.id);

                return Ok("everything work well");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"internal server error: {ex.Message}");
            }
        }

        [HttpPut("endTask")]
        public async Task<IActionResult> EndTask([FromBody] OverTaskRequest? overtaskRequest)
        {
            if (overtaskRequest == null)
                return BadRequest("Task request cannot be null");

            try
            {
                var taskId = overtaskRequest.taskId;
                if (string.IsNullOrEmpty(taskId))
                    return BadRequest("TaskId is required");

                var taskById = await _tasksRepository.GetByIdAsync(taskId);
                if (taskById == null)
                    return NotFound($"Task with ID {taskId} not found");

                var userId = await getUserIdViaSessionToken();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in session");

                var userInfo = await _userRepository.GetByIdAsync(userId);
                if (userInfo == null)
                    return Unauthorized("User information not found");

                var userLevelStr = $"level{userInfo.level}";
                var receiverLevelStr = $"level{userInfo.level + 1}";

                var senderProperty = taskById.dataFlow.GetType().GetProperty(userLevelStr);
                if (senderProperty?.GetValue(taskById.dataFlow) is Tasks.Level senderLevel)
                {
                    senderLevel.status = "waitApproval";
                    senderLevel.timeSpan = DateTime.UtcNow;
                    senderProperty.SetValue(taskById.dataFlow, senderLevel);
                }

                var receiverProperty = taskById.dataFlow.GetType().GetProperty(receiverLevelStr);
                if (receiverProperty?.GetValue(taskById.dataFlow) is Tasks.Level receiverLevelValue)
                {
                    receiverLevelValue.status = "receiveApproval";
                    receiverLevelValue.timeSpan = DateTime.UtcNow;
                    receiverProperty.SetValue(taskById.dataFlow, receiverLevelValue);

                    var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
                    var georgiaTime = TimeZoneInfo.ConvertTime(DateTime.Now, georgiaTimeZone);
                    var receiverUser = await _userRepository.GetByIdAsync(receiverLevelValue.userId);
                    if (receiverUser != null)
                    {
                        taskById.dataLogs ??= new List<Tasks.TaskLogEntry>();
                        taskById.dataLogs.Add(new Tasks.TaskLogEntry
                        {
                            level = userInfo.level,
                            timestamp = georgiaTime.ToString("M/d/yyyy, h:mm:ss tt"),
                            addedByName = userInfo.fullname,
                            addedById = userInfo.id,
                            description = "დავალების დასრულების მოთხოვნა",
                            receiverName = receiverUser.fullname,
                            receiverId = receiverUser.id,
                            imgUrl = userInfo.imgUrl
                        });
                    }

                    await _tasksRepository.UpdateAsync(taskById.id!, taskById);
                    if (receiverLevelValue.userId != null)
                        await SendData(receiverLevelValue.userId);
                }

                return Ok("everything work well");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"internal server error: {ex.Message}");
            }
        }

        [HttpPost("addNewTask")]
        public async Task<IActionResult> AddNewTask([FromBody] AddTaskRequest? request)
        {
            if (request?.FirstArgument == null)
                return BadRequest("Problem Tasks is null");

            var task = request.FirstArgument;
            var userLevel = $"level{request.SecondArgument.level}";

            if (task?.dataFlow != null)
            {
                var property = task.dataFlow.GetType().GetProperty(userLevel);
                if (property != null)
                {
                    if (property.GetValue(task.dataFlow) is Tasks.Level createrLevelValue)
                    {
                        createrLevelValue.userId = request.SecondArgument.id;
                        createrLevelValue.status = "onGoing";
                        createrLevelValue.timeSpan = DateTime.UtcNow;
                        property.SetValue(task.dataFlow, createrLevelValue);
                    }
                }
            }

            await _tasksRepository.CreateAsync(task!);
            return Ok("data has upload succesfully");
        }

        [RequestSizeLimit(1073741824)]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string id)
        {
            if (file == null || file.Length == 0 || string.IsNullOrEmpty(id))
                return BadRequest("No file uploaded.");

            // Decide subfolder based on file type
            string subFolder;
            if (file.ContentType.StartsWith("image") || file.ContentType.StartsWith("video"))
            {
                subFolder = "image-videos";
            }
            else if (file.ContentType == "application/msword" ||
                     file.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                subFolder = "words";
            }
            else
            {
                subFolder = "others"; // fallback for unsupported types
            }

            // Use FileService to get target folder (instead of hardcoding Desktop)
            var targetFolder = _fileService.GetTargetFolder(subFolder);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(targetFolder, uniqueFileName);

            var currentTask = await _tasksRepository.GetByIdAsync(id);
            if (currentTask == null)
                return BadRequest("We can't find that task");

            try
            {
                // Stream file directly to disk (efficient for large files)
                await using var stream = System.IO.File.Create(filePath);
                await file.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"File save failed: {ex.Message}");
            }

            // Build attachment metadata
            var attachmentData = new TaskAttachedData
            {
                timeSpan = DateTime.UtcNow,
                type = subFolder,
                url = Path.Combine(subFolder, uniqueFileName).Replace("\\", "/"), // normalize to forward slashes     
                fileName = file.FileName // original name
            };

            currentTask.taskAttachedData.Add(attachmentData);

            await _tasksRepository.UpdateAsync(id, currentTask);

            return Ok(new { filePath, attachments = currentTask.taskAttachedData });
        }
        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("URL parameter is required.");

            // Reconstruct the full physical path
            var filePath = _fileService.GetFullFilePath(url);   // ← Important helper (see below)

            // Security: Prevent path traversal attacks
            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found.");

            // Optional: Check if the current user has access to this task/attachment
            // Example: await CheckTaskAccessAsync(url);  // implement if needed

            var fileName = Path.GetFileName(url); // or fetch original fileName from DB if you prefer

            try
            {
                // PhysicalFileResult is the most efficient for files on disk
                return PhysicalFile(filePath, _fileService.GetContentType(filePath), fileName);
            }
            catch (Exception ex)
            {
                // Log the exception in production
                return StatusCode(500, $"Download failed: {ex.Message}");
            }
        }



        [HttpGet("getTaskDocuments")]
        public async Task<IActionResult> GetTaskDocuments([FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("we have error to get Task id");

            var c = await _tasksRepository.GetByIdAsync(id);
            if (c == null)
                return BadRequest("task with that id is not in database");


            var documents = c.taskAttachedData;
            if (documents == null)
                return BadRequest("there is problem");

            return Ok(documents);
        }

        [HttpPut("declineTask")]
        public async Task<IActionResult> DeclinedTask([FromBody] DeclineTaskRequest? declineTaskRequest)
        {
            if (declineTaskRequest == null)
                return BadRequest("Task request cannot be null");

            try
            {
                var taskId = declineTaskRequest.taskId;
                if (string.IsNullOrEmpty(taskId))
                    return BadRequest("TaskId is required");

                var taskById = await _tasksRepository.GetByIdAsync(taskId);
                if (taskById == null)
                    return NotFound($"Task with ID {taskId} not found");

                var userId = await getUserIdViaSessionToken();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in session");

                var userInfo = await _userRepository.GetByIdAsync(userId);
                if (userInfo == null)
                    return Unauthorized("User information not found");

                var userLevelStr = $"level{userInfo.level}";
                var receiverLevelStr = $"level{userInfo.level - 1}";

                var senderProperty = taskById.dataFlow.GetType().GetProperty(userLevelStr);
                if (senderProperty?.GetValue(taskById.dataFlow) is Tasks.Level senderLevel)
                {
                    senderLevel.status = "onPending";
                    senderLevel.timeSpan = DateTime.UtcNow;
                    senderProperty.SetValue(taskById.dataFlow, senderLevel);
                }

                var receiverProperty = taskById.dataFlow.GetType().GetProperty(receiverLevelStr);
                if (receiverProperty?.GetValue(taskById.dataFlow) is Tasks.Level receiverLevelValue)
                {
                    receiverLevelValue.status = "onGoing";
                    receiverLevelValue.timeSpan = DateTime.UtcNow;
                    receiverProperty.SetValue(taskById.dataFlow, receiverLevelValue);


                    var receiverUser = await _userRepository.GetByIdAsync(receiverLevelValue.userId);
                    if (receiverUser != null)
                    {
                        var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
                        var georgiaTime = TimeZoneInfo.ConvertTime(DateTime.Now, georgiaTimeZone);
                        taskById.dataLogs ??= new List<Tasks.TaskLogEntry>();
                        taskById.dataLogs.Add(new Tasks.TaskLogEntry
                        {
                            level = userInfo.level,
                            timestamp = georgiaTime.ToString("M/d/yyyy, h:mm:ss tt"),
                            addedByName = userInfo.fullname,
                            addedById = userInfo.id,
                            description = "დავალების დასრულების მოთხოვნის უარყოფა",
                            receiverName = receiverUser.fullname,
                            receiverId = receiverUser.id,
                            imgUrl = userInfo.imgUrl,
                            comment = declineTaskRequest.comment
                        });
                    }

                    await _tasksRepository.UpdateAsync(taskById.id!, taskById);
                    if (receiverLevelValue.userId != null)
                        await SendData(receiverLevelValue.userId);
                }

                return Ok("everything work well");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"internal server error: {ex.Message}");
            }
        }

       

        private async Task<IActionResult> GetFilteredTasksAsync(string status, int skip, int take)
        {
            {
                var user = await GetValidUserAsync();
                if (user == null)
                    return Unauthorized("userId is missing");

                var levelString = $"level{user.level}";

                var filter = Builders<Tasks>.Filter.Eq($"dataFlow.level{user.level}.status", status) &
                             Builders<Tasks>.Filter.Eq($"dataFlow.level{user.level}.userId", user.id);

                var tasks = await _tasksRepository.GetPagedTasksAsync(filter, skip, take, levelString);

                var result = tasks.Select(t => new
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
                });

                return Ok(result);
            }
        }

        public async Task SendData(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var connectionId = NotificationHub.GetConnectionId(userId);
            if (!string.IsNullOrEmpty(connectionId))
            {
                string uniqueId = Guid.NewGuid().ToString();
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveData", $"Update database - {uniqueId}");
            }
        }

        public async Task<string?> getUserIdViaSessionToken()
        {
            var sessionToken = Request.Cookies["session-token"];
            if (string.IsNullOrEmpty(sessionToken))
                return null;

            return await _redisExample.GetUserIdBySessionToken(sessionToken);
        }

        public async Task<Users?> GetValidUserAsync()
        {
            var sessionToken = Request.Cookies["session-token"];
            if (string.IsNullOrEmpty(sessionToken))
                return null;

            var userId = await _redisExample.GetUserIdBySessionToken(sessionToken);
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _userRepository.GetByIdAsync(userId);
        }
    }
}