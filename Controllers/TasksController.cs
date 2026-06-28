using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Models;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Services;
using ASP.MongoDb.API.SignalIR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ASP.MongoDb.API.Entities.Tasks;

namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IUserRepository _userRepository;
        private readonly ITasksRepository _tasksRepository;
        private readonly RedisExample _redisExample;
        private readonly IFileService _fileService;

        /// კონტროლერის კონსტრუქტორი - ინიციალიზაცია ხდება დამოკიდებულებების ინექციით
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

        [HttpGet("finished")]
        public async Task<IActionResult> GetOnFinished(int skip, int take)
        {
            return await GetFilteredTasksAsync(false, skip, take);

        }
        /// აბრუნებს მიმდინარე დავალებებს (onGoing)
        [HttpGet("onGoing")]
        public async Task<IActionResult> GetOnGoing(int skip, int take)
            => await GetFilteredTasksAsync(true, skip, take);

        /// აბრუნებს მომლოდინე დავალებებს (onPending)
       

        /// აბრუნებს ბოლო ლოგის გამგზავნისა და მიმღების სახელებს კომენტარისთვის
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

        /// აბრუნებს კონკრეტული დავალების სრულ ინფორმაციას ID-ის მიხედვით
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

        /// ფილტრაციისთვის საჭირო მოდელი
        public class GetFilteredDatas
        {
            public SearchInterface filterData { get; set; } = null!;
            public string choosedOption { get; set; } = null!;
            public int skip { get; set; }
            public int take { get; set; }
        }

        /// აბრუნებს დავალებებს მოცემული ფილტრების მიხედვით (მომხმარებლის დონისა და სტატუსის გათვალისწინებით)
        [HttpPost("getFilteredData")]
        public async Task<IActionResult> GetFilteredData([FromBody] GetFilteredDatas? filteredData)
        {
            if (filteredData == null || filteredData.filterData == null)
                return BadRequest("there is problem");

            var user = await GetValidUserAsync();
            if (user == null)
                return Unauthorized("User not found");

            var filterBuilder = Builders<Tasks>.Filter;
            var filters = new List<FilterDefinition<Tasks>>();

            // User-level filters (status + userId)
            if(filteredData.choosedOption == "finished")
            {
                Console.WriteLine(filteredData.skip);
            filters.Add(filterBuilder.Eq("status", false));
            }else
            {
                Console.WriteLine(filteredData.skip);

                filters.Add(filterBuilder.Eq("status", true));

            }


            // WorkingCode
            if (!string.IsNullOrEmpty(filteredData.filterData.datas.workingCode))
                filters.Add(filterBuilder.Regex("workingCode",
                    new BsonRegularExpression(filteredData.filterData.datas.workingCode, "i")));

            // Convicted
            if (!string.IsNullOrEmpty(filteredData.filterData.datas.convicted))
                filters.Add(filterBuilder.Regex("convicted",
                    new BsonRegularExpression(filteredData.filterData.datas.convicted, "i")));

            // RegisterDate
            if (!string.IsNullOrEmpty(filteredData.filterData.datas.registerDate))
                filters.Add(filterBuilder.Regex("registerDate",
                    new BsonRegularExpression(filteredData.filterData.datas.registerDate, "i")));

            // Lawyer
            if (!string.IsNullOrEmpty(filteredData.filterData.datas.lawyer))
                filters.Add(filterBuilder.Regex("lawyer",
                    new BsonRegularExpression(filteredData.filterData.datas.lawyer, "i")));

            var combinedFilter = filterBuilder.And(filters);

            var tasks = await _tasksRepository.GetPagedTasksAsync(
                combinedFilter,
                filteredData.skip,
                filteredData.take,
                $"level{user.level}"
            );

            var results = tasks.Select(t => new
            {
                t.id,
                t.workingCode,
                t.convicted,
                t.registerDate,
                t.lawyer,
                t.dataLogs,
                t.statusIdentifier
            });

            return Ok(results);
        }


        
        /// დავალების დასრულება და დამტკიცებისთვის გაგზავნა
        [HttpPut("endTask")]
        public async Task<IActionResult> EndTask([FromBody] OverTaskRequest? overtaskRequest)
        {
            if (overtaskRequest == null)
                return BadRequest("Task request cannot be null");


            try
            {
                var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
                var georgiaTime = TimeZoneInfo.ConvertTime(DateTime.Now, georgiaTimeZone);
                var taskId = overtaskRequest.taskId;
                if (string.IsNullOrEmpty(taskId))
                    return BadRequest("TaskId is required");

                var taskById = await _tasksRepository.GetByIdAsync(taskId);
                if (taskById == null)
                    return NotFound($"Task with ID {taskId} not found");

                var userId = await getUserIdViaSessionToken();

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in session");


                var lawyerUser = await _userRepository.GetByIdAsync(taskById.lawyerId);
                lawyerUser.amountOfOnGoingTasks -= 1;
                lawyerUser.amountOfFinishedTasks += 1;

                await _userRepository.UpdateAsync(taskById.lawyerId, lawyerUser);

                taskById.status = false;
                taskById.timeSpan = DateTime.UtcNow;

                taskById.statusIdentifier = "დამთავრებული";
                await _tasksRepository.UpdateAsync(overtaskRequest.taskId, taskById);

                return Ok("everything work well");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"internal server error: {ex.Message}");
            }
        }

        [HttpPut("recoverTask")]
        public async Task<IActionResult> RecoverTask([FromBody] OverTaskRequest? overtaskRequest)
        {
            if (overtaskRequest == null)
                return BadRequest("Task request cannot be null");


            try
            {
                var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
                var georgiaTime = TimeZoneInfo.ConvertTime(DateTime.Now, georgiaTimeZone);
                var taskId = overtaskRequest.taskId;
                if (string.IsNullOrEmpty(taskId))
                    return BadRequest("TaskId is required");

                var taskById = await _tasksRepository.GetByIdAsync(taskId);
                if (taskById == null)
                    return NotFound($"Task with ID {taskId} not found");

                var userId = await getUserIdViaSessionToken();

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in session");



                taskById.status = true;
                taskById.timeSpan = DateTime.UtcNow;
                taskById.statusIdentifier = "აღდგენილი";
                var lawyerUser = await _userRepository.GetByIdAsync(taskById.lawyerId);
                lawyerUser.amountOfFinishedTasks -= 1;
                lawyerUser.amountOfOnGoingTasks += 1;

                await _userRepository.UpdateAsync(taskById.lawyerId, lawyerUser);
                await _tasksRepository.UpdateAsync(overtaskRequest.taskId, taskById);

                return Ok("everything work well");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"internal server error: {ex.Message}");
            }
        }

        /// ახალი დავალების შექმნა
        [HttpPost("addNewTask")]
        public async Task<IActionResult> AddNewTask([FromBody] AddTaskRequest? request)
        {

            if (request?.FirstArgument == null)
                return BadRequest("Problem Tasks is null");

            var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
            var georgiaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, georgiaTimeZone);

            var task = request.FirstArgument;
            var userLevel = $"level{request.SecondArgument.level}";
            var user = await _userRepository.GetByIdAsync(request.SecondArgument.id);
            var receiverUser = await _userRepository.GetByIdAsync(request.thirdArgument);

            receiverUser.amountOfOnGoingTasks += 1;

            await _userRepository.UpdateAsync(receiverUser.id, receiverUser);
            
           
            var wholeData = new Tasks
            {
                workingCode = task.addNew.workingCode,
                convicted = task.addNew.convicted,
                lawyer = task.addNew.lawyer,
                registerDate = georgiaTime.ToString("yyyy-MM-dd"),
                status = true,
                taskAttachedData = [],
                lawyerId = request.thirdArgument,
                statusIdentifier = "დამატებული",
                timeSpan = DateTime.UtcNow,
                dataLogs = new List<Tasks.TaskLogEntry>
    {
        new Tasks.TaskLogEntry
        {
            level = user.level,
           timestamp = georgiaTime.ToString("yyyy-MM-dd HH:mm:ss"),
            addedByName = user.fullname,
            addedById = user.id,
            description = "საქმის დამატება ბაზაში",
            receiverName = user.fullname,
            receiverId = user.id,
            comment = "საქმე დამატებულია",
            imgUrl = user.imgUrl
        },
       
    }


            };

            await _tasksRepository.CreateAsync(wholeData!);
            return Ok("data has upload succesfully");
        }

        [HttpGet("getDataLogs")]
        public async Task<IActionResult> GetDataLogs([FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("id is not valid");
            var task = await _tasksRepository.GetByIdAsync(id);
            if (task == null)
                return BadRequest("we cant find task");

            return Ok(task.dataLogs);
        }


        /// ფაილის ატვირთვა დავალებასთან (სურათი, ვიდეო, word ან სხვა)
        [RequestSizeLimit(1073741824)]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string id)
        {
            if (file == null || file.Length == 0 || string.IsNullOrEmpty(id))
                return BadRequest("No file uploaded.");

            string subFolder = (file.ContentType.StartsWith("image") || file.ContentType.StartsWith("video")) ? "image-videos"
                             : (file.ContentType == "application/msword" ||
                                file.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document") ? "words"
                             : "others";

            var targetFolder = _fileService.GetTargetFolder(subFolder);
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(targetFolder, uniqueFileName);

            var currentTask = await _tasksRepository.GetByIdAsync(id);
            if (currentTask == null)
                return BadRequest("We can't find that task");

            try
            {
                await using var stream = System.IO.File.Create(filePath);
                await file.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"File save failed: {ex.Message}");
            }

            var attachmentData = new TaskAttachedData
            {
                timeSpan = DateTime.UtcNow,
                type = subFolder,
                url = Path.Combine(subFolder, uniqueFileName).Replace("\\", "/"),
                fileName = file.FileName
            };

            currentTask.taskAttachedData.Add(attachmentData);

            var userId = await getUserIdViaSessionToken();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in session");

            var userInfo = await _userRepository.GetByIdAsync(userId);
            if (userInfo == null)
                return Unauthorized("User information not found");

                var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
                var georgiaTime = TimeZoneInfo.ConvertTime(DateTime.Now, georgiaTimeZone);
                currentTask.dataLogs ??= new List<Tasks.TaskLogEntry>();
            currentTask.dataLogs.Add(new Tasks.TaskLogEntry
            {
                level = userInfo.level,
                timestamp = georgiaTime.ToString("M/d/yyyy, h:mm:ss tt"),
                addedByName = userInfo.fullname,
                addedById = userInfo.id,
                description = "დოკუმენტის ან ფოტო/ვიდეო-ს ატვირთვა",
                receiverName = "",
                receiverId = "",
                comment = $"{userInfo.fullname}-მა ატვირთა {file.FileName} ",
                imgUrl = userInfo.imgUrl
            });

            await _tasksRepository.UpdateAsync(id, currentTask);


            return Ok(new
            {
                message = "დოკუმენტი დაემატა წარმატებით",
                filePath = filePath,
                attachments = currentTask.taskAttachedData
            });

        }

        /// ფაილის ჩამოტვირთვა შენახული URL-ის მიხედვით
        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("URL parameter is required.");

            var filePath = _fileService.GetFullFilePath(url);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found.");

            var fileName = Path.GetFileName(url);

            try
            {
                return PhysicalFile(filePath, _fileService.GetContentType(filePath), fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Download failed: {ex.Message}");
            }
        }

        /// კონკრეტული დოკუმენტის წაშლა დავალებიდან (ჯერ სერვერიდან, შემდეგ ბაზიდან)
        [HttpDelete("removeDocumentFromTask")]
        public async Task<IActionResult> RemoveSpecificDocumentFromTasks([FromQuery] string taskId, [FromQuery] int number, [FromQuery] string fileName)
        {
            if (string.IsNullOrEmpty(taskId))
                return BadRequest("TaskId was not provided correctly.");

            var task = await _tasksRepository.GetByIdAsync(taskId);
            if (task == null)
                return NotFound("We couldn't find a task with that Id.");

            if (task.taskAttachedData == null || task.taskAttachedData.Count == 0)
                return BadRequest("Task has no attachments.");

            if (number < 0 || number >= task.taskAttachedData.Count)
                return BadRequest("Invalid index provided.");

            var attachmentToDelete = task.taskAttachedData[number];

            if (string.IsNullOrEmpty(attachmentToDelete.url))
                return BadRequest("Attachment URL is missing.");

            // Delete file physically
            bool fileDeleted = _fileService.DeleteFile(attachmentToDelete.url);

            // Remove from DB
            task.taskAttachedData.RemoveAt(number);

            var userId = await getUserIdViaSessionToken();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in session");

            var userInfo = await _userRepository.GetByIdAsync(userId);
            if (userInfo == null)
                return Unauthorized("User information not found");

            var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
            var georgiaTime = TimeZoneInfo.ConvertTime(DateTime.Now, georgiaTimeZone);

            task.dataLogs ??= new List<Tasks.TaskLogEntry>();
            task.dataLogs.Add(new Tasks.TaskLogEntry
            {
                level = userInfo.level,
                timestamp = georgiaTime.ToString("M/d/yyyy, h:mm:ss tt"),
                addedByName = userInfo.fullname,
                addedById = userInfo.id,
                description = "დოკუმენტის ან ფოტო/ვიდეო-ს წაშლა", // corrected description
                receiverName = "",
                receiverId = "",
                comment = $"{userInfo.fullname}-მა წაშალა {attachmentToDelete.fileName}", // ✅ use saved reference
                imgUrl = userInfo.imgUrl
            });

            await _tasksRepository.UpdateAsync(taskId, task);

            return Ok(new
            {
                message = "Document removed successfully",
                fileDeletedSuccessfully = fileDeleted,
                remainingAttachments = task.taskAttachedData
            });
        }

        /// აბრუნებს დავალებასთან დაკავშირებულ ყველა დოკუმენტს
        [HttpGet("getTaskDocuments")]
        public async Task<IActionResult> GetTaskDocuments([FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("uknown problem (დაუკავშირდით დეველოპერს)");

            var task = await _tasksRepository.GetByIdAsync(id);
            if (task == null)
                return BadRequest("საქმე ვერ მოიძებნა მონაცემთა ბაზაში");

            return Ok(new
            {
                message = "დოკუმენტები და ფოტო/ვიდეო მასალა ჩაიტვირთა წარმატებით",
                attachments = task.taskAttachedData ?? new List<TaskAttachedData>()
            });
        }

        
        /// შიდა მეთოდი - აბრუნებს დავალებებს სტატუსის მიხედვით (გამოიყენება ზემოთ არსებული Get მეთოდებში)
        private async Task<IActionResult> GetFilteredTasksAsync(bool status, int skip, int take)
        {
            Console.WriteLine(status);
            var user = await GetValidUserAsync();
            if (user == null)
                return Unauthorized("userId is missing");

            var filterBuilder = Builders<Tasks>.Filter;
            var filters = new List<FilterDefinition<Tasks>>();

            filters.Add(filterBuilder.Eq("status", status));

            var combinedFilter = filterBuilder.And(filters);

            var tasks = await _tasksRepository.GetPagedTasksAsync(combinedFilter, skip, take, $"level{user.level}");

            var results = tasks.Select(t => new
            {
                t.id,
                t.workingCode,
                t.convicted,
                t.lawyer,
                t.registerDate,
                t.dataLogs,
                t.statusIdentifier
            });

            return Ok(results);
        }


        /// SignalR-ის საშუალებით აგზავნის განახლების შეტყობინებას მითითებულ მომხმარებელთან
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

        /// აბრუნებს მომხმარებლის ID-ს სესიის ტოკენიდან (Redis-იდან)
        public async Task<string?> getUserIdViaSessionToken()
        {
            var sessionToken = Request.Cookies["session-token"];
            if (string.IsNullOrEmpty(sessionToken))
                return null;

            return await _redisExample.GetUserIdBySessionToken(sessionToken);
        }

        /// აბრუნებს ავტორიზებული მომხმარებლის სრულ ინფორმაციას
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