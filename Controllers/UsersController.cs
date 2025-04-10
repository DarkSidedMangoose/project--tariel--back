using System.Net.Http.Headers;
using System.Security.Claims;
using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP.MongoDb.API.Controllers
{
    //[Authorize(Policy = "SuperAdminPolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RedisExample _redisExample;
        private readonly IUserRepository _repository;

        public UsersController(IUserRepository userRepository, RedisExample redisExample)
        {
            _repository = userRepository;
            _redisExample = redisExample;
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

        [HttpGet("getUserInfo")]
        public async Task<IActionResult> GetUserInfo()
        {
            var sessionToken = Request.Cookies["session-token"];
            if (string.IsNullOrEmpty(sessionToken))
            {
                return NotFound("Token is expired");
            }
            var userId = await _redisExample.GetUserIdBySessionToken(sessionToken);

            if (string.IsNullOrEmpty(sessionToken))
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
                userInfo.level
            };
            return Ok(desireInfo);


           


        }
        [HttpPost]
        public async Task<IActionResult> Create(Users user)
        {
            //Hash the plain text password using BCrypt
            user.passwordHash = BCrypt.Net.BCrypt.HashPassword(user.passwordHash);

            // Call the repository's CreateAsync method to save the user
            await _repository.CreateAsync(user);

            return CreatedAtAction(nameof(Get), new { id = user.id }, new
            {
                user.id,
                user.fullname,
                user.username,
                user.passwordHash,
                user.role,
                user.level,
                user.diversion,
                user.imgUrl
            }
                );
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
            exactUser.diversion = user.diversion;

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
            

            var desiredInfoOfUsers = users.Select(d => new { d.id, d.fullname, d.position, d.level, d.department, d.diversion, d.status, d.section }).ToList();
            

            return Ok(desiredInfoOfUsers);
        }
    }
}
