using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Models;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Services;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Policy;
using static ASP.MongoDb.API.Entities.Tasks;

namespace ASP.MongoDb.API.Controllers
{
    //[Authorize(Policy = "SuperAdminPolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RedisExample _redisExample;
        private readonly IUserRepository _repository;
        private readonly IFileService _fileService;


        public UsersController(IUserRepository userRepository, RedisExample redisExample, IFileService fileService)
        {
            _repository = userRepository;
            _redisExample = redisExample;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUser()
        {
            var users = await _repository.GetAllAsync();
            return Ok(users);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var user = await _repository.GetByIdAsync(id);
            return Ok(user);
        }



        [HttpGet("checkToken")]
        public async Task<IActionResult> CheckToken()
        {
            var sessionToken = Request.Cookies["session-token"];

            if (sessionToken == null)
            {
                Console.WriteLine("i am here");
                return NotFound("Session token is missing");
            }

            var userId = await _redisExample.GetUserIdBySessionToken(sessionToken);

            if (userId != null)
            {
                
                    return Ok("Session token found");
                
            }
            else
            {
                return NotFound("No user found for this session token");
            }
        }




        [HttpPost("editUser")]
        public async Task<IActionResult> EditUserData([FromForm] Users user)
        {
            if (user == null)
                return NotFound("No user data provided.");

            if (string.IsNullOrEmpty(user.id))
                return NotFound("User Id is missing.");

            var currentUser = await _repository.GetByIdAsync(user.id);
            if (currentUser == null)
                return NotFound("User not found.");

            // Handle file upload
            if (user.file != null && user.file.Length > 0 && user.file.ContentType.StartsWith("image"))
            {
                string subFolder = "userImages";
                var targetFolder = _fileService.GetTargetFolder(subFolder);
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(user.file.FileName)}";
                var filePath = Path.Combine(targetFolder, uniqueFileName);

                try
                {
                    // Delete old file first (if exists)
                    if (!string.IsNullOrEmpty(currentUser.imgUrl))
                    {
                        _fileService.DeleteFile(currentUser.imgUrl); // relative path
                    }

                    // Save new file
                    await using var stream = System.IO.File.Create(filePath);
                    await user.file.CopyToAsync(stream);

                    // Store relative path in DB
                    currentUser.imgUrl = Path.Combine(subFolder, uniqueFileName).Replace("\\", "/");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"File save failed: {ex.Message}");
                }
            }

            // Update other fields
            currentUser.fullname = user.fullname;
            currentUser.userId = user.userId;
            currentUser.dateOfBirth = user.dateOfBirth;
            currentUser.phone = user.phone;
            currentUser.email = user.email;
            currentUser.username = user.username;

            if (!string.IsNullOrEmpty(user.passwordHash))
            {
                currentUser.passwordHash = BCrypt.Net.BCrypt.HashPassword(user.passwordHash);
            }

            currentUser.status = user.status;

            await _repository.UpdateAsync(user.id, currentUser);

            return Ok("User info edited successfully");
        }


        [HttpGet("getUserById/{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            Console.WriteLine(id);
            if(string.IsNullOrEmpty(id))
            {
                return NotFound("id missed");
            }
            var newData = await _repository.GetByIdAsync(id);
            Console.WriteLine("text");
            if(newData == null)
            {
                return NotFound("there is not that id ");
            }
            return Ok(newData);
        }

        [HttpGet("getUserInfoDetails")]
        public async Task<IActionResult> GetUserInfoDetails()
        {
            var sessionToken = Request.Cookies["session-token"];
            if (string.IsNullOrEmpty(sessionToken))
            {
                return NotFound("Token is Expired");
            }



            var userId = await _redisExample.GetUserIdBySessionToken(sessionToken);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound("there is finding user in redis via token problem");
            }

            var userInfo = await _repository.GetByIdAsync(userId);
            if (userInfo == null)
            {
                return NotFound("in database that user doesnt exist");
            }

            if (userInfo.imgUrl != null)
            {

                var filePath = _fileService.GetFullFilePath(userInfo.imgUrl);
                var userData = new
                {
                    imgUrl = userInfo.imgUrl,
                    fullname = userInfo.fullname,
                    userId = userInfo.userId,
                    phone = userInfo.phone,
                    email = userInfo.email,
                    dateOfBirth = userInfo.dateOfBirth,
                    rating = userInfo.rating,
                    amountOfFinishedTasks = userInfo.amountOfFinishedTasks,
                    amountOfOnGoingTasks = userInfo.amountOfOnGoingTasks,

                };
            return Ok(userData);

            }
            else {
                var userData = new
                {
                    imgUrl = userInfo.imgUrl,
                    fullname = userInfo.fullname,
                    userId = userInfo.userId,
                    phone = userInfo.phone,
                    email = userInfo.email,
                    dateOfBirth = userInfo.dateOfBirth,
                    rating = userInfo.rating,
                    amountOfFinishedTasks = userInfo.amountOfFinishedTasks,
                    amountOfOnGoingTasks = userInfo.amountOfOnGoingTasks,

                };

            return Ok(userData);
            }



            // Project only the properties you want to expose
           

        }

        [HttpGet("getLawyers")]
        public async Task<IActionResult> GetLawyers()
        {
            var users = await _repository.GetAllAsync();
            if(users == null)
            {
                return BadRequest("users getting  problem from mongo");
            }
            // Get Georgia timezone
            var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");

            // Current UTC time
            var utcNow = DateTime.UtcNow;

            // Convert to Georgia local time
            var georgiaNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, georgiaTimeZone);
            var result = users.Select(u => new
            {
                u.id,
                u.fullname,

            });
            return Ok(new
            {
                date = georgiaNow.ToString("yyyy-MM-dd HH:mm:ss"),
                response = result
            });
        }

        

        [HttpGet("getUserInfo")]
        public async Task<IActionResult> GetUserInfo()
        {
            var sessionToken = Request.Cookies["session-token"];
            if (string.IsNullOrEmpty(sessionToken))
            {
                return NotFound("Token is expired");
            }
            var userId = await _redisExample.GetUserIdBySessionToken(sessionToken);

            if (string.IsNullOrEmpty(userId))
            {
                return NotFound("Token is expired ");
            }

            if (string.IsNullOrEmpty(userId))
            {
                return NotFound("not found user id");
            }

            var userInfo = await _repository.GetByIdAsync(userId);

            if (userInfo == null)
            {
                return NotFound("User not found");
            }

            var desireInfo = new
            {
                userInfo.id,
                userInfo.fullname,
                userInfo.level,
                userInfo.imgUrl
            };
            return Ok(desireInfo);


           


        }

        

        [HttpPost("addUsers")]
        public async Task<IActionResult> Create([FromForm] Users user)
        {
            Console.WriteLine(user.passwordHash); 

            //Hash the plain text password using BCrypt
            user.passwordHash = BCrypt.Net.BCrypt.HashPassword(user.passwordHash);

            if (user == null )
            {
                return BadRequest("something is wrong");
            }

            if (user.file != null && user.file.Length > 0 && user.file.ContentType.StartsWith("image"))
            {
                string subFolder = "userImages";
                var targetFolder = _fileService.GetTargetFolder(subFolder);
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(user.file.FileName)}";
                var filePath = Path.Combine(targetFolder, uniqueFileName);
                try
                {
                    await using var stream = System.IO.File.Create(filePath);
                    await user.file.CopyToAsync(stream);
                    user.imgUrl = Path.Combine(subFolder, uniqueFileName).Replace("\\", "/");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"File save failed: {ex.Message}");
                }
            }

                // Call the repository's CreateAsync method to save the user

                user.rating = "5.0";
            user.amountOfFinishedTasks = 0;
            user.amountOfOnGoingTasks = 0;
            user.position = "ადვოკატი";
            user.level = 1;
            user.status = "აქტიური";
            


                await _repository.CreateAsync(user);
            return Ok("user Created succesfully");

            
        }
        [HttpPut("{id}")]

        public async Task<IActionResult> Update(string id, Users user)
        {
            var exactUser = await _repository.GetByIdAsync(id);
            if (exactUser == null)
            {
                return BadRequest("there is not such an user");
            }
            exactUser.username = user.username;
            exactUser.role = user.role;
            exactUser.level = user.level;
            exactUser.passwordHash = BCrypt.Net.BCrypt.HashPassword(user.passwordHash);

            await _repository.UpdateAsync(id, exactUser);
            return Ok(exactUser);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _repository.DeleteAsync(id);
            return Ok(new { success = true, message = "deleted" });
        }
        [HttpGet("AllUsersDesiredData")]
        public async Task <IActionResult> GetDataForConfigurations()
        {
            var users = await _repository.GetAllAsync();
            

            var desiredInfoOfUsers = users.Select(d => new { d.id, d.fullname, d.position, d.level,  d.status, }).ToList();
            

            return Ok(desiredInfoOfUsers);
        }

        public class GetData {
            public string? searchData { get; set; }
        }


        [HttpGet("getFilteredUsers")]
        public async Task<IActionResult> GetFilteredUsers([FromQuery] GetData filterData)
        {
            Console.WriteLine(filterData.searchData);
            if (filterData == null)
                return BadRequest();

            var users = await _repository.GetAllAsync();

            if (users == null)
                return BadRequest("users getting problem from mongo");

            var filteredUsers = users;

            if (!string.IsNullOrWhiteSpace(filterData.searchData))
            {
                filteredUsers = users.Where(u =>
                    u.fullname != null &&
                    u.fullname.Contains(
                        filterData.searchData,
                        StringComparison.OrdinalIgnoreCase));
            }

            var result = filteredUsers.Select(u => new
            {
                u.id,
                u.fullname,
                u.position,
                u.level,
                u.status
            });

            return Ok(result);
        }

        [HttpGet("LogOut")]
        public async Task<IActionResult> LogOut([FromQuery] string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                return BadRequest("token is missed");
            }

            Console.WriteLine("everything work well");
            HttpContext.Session.Remove("session-token");

            // Expire the HttpOnly cookie
            Response.Cookies.Append("session-token", "", new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1), // Forces expiration
                HttpOnly = true, // Keeps it secure
                Secure = true, // Required if using HTTPS
                SameSite = SameSiteMode.Strict // Adjust based on security needs
            });

            return Ok("Session-token cookie removed successfully.");
        }

        public class Image
        {
            public string? identifier { get; set; } 
            public string? image { get; set; }
        }

       

    }
}
