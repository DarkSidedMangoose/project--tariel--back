using System.Net.Http.Headers;
using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP.MongoDb.API.Controllers
{
    [Authorize(Policy = "SuperAdminPolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _repository;

        public UsersController(IUserRepository userRepository)
        {
            _repository = userRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
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
        [HttpPost]
        public async Task <IActionResult> Create(Users user)
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
            if(exactUser == null)
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
            return Ok(new { success = true, message = "product deleted" });
        }
    }
}
