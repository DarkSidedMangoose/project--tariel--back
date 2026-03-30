using System.Net.Http.Headers;
using System.Security.Claims;
using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Models;
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


       


        [HttpPost("editUser")]
        public async Task<IActionResult> EditUserData(Users user)
        {




            if(user != null )
            {
                if(string.IsNullOrEmpty(user.id))
                {
                    return NotFound("data doesnt include Id or there is some problem fix it");
                }else
                {
                    if (user.level == 7)
                    {
                        user.position = "მთავარი შრომის ინსპექტორი";
                        user.role = "superAdmin";
                        user.department = "წვდომა ყველა დეპარტამენტზე";
                        user.diversion = "წვდომა ყველა სამართველოზე";
                        user.section = "წვდომა ყველა განყოფილებაზე";
                    }
                    else if (user.level == 6)
                    {
                        user.role = "admin";
                        if (user.department == "შრომითი უფლებების ზედამხედველობის დეპარტამენტი")
                        {
                            user.position = "მთავარი შრომის ინსპექტორის 1-ლი მოადგილე";

                        }
                        else
                        {
                            user.position = "მთავარი შრომის ინსპექტორის მოადგილე";
                        }
                        if (user.department == "შრომითი უფლებების ზედამხედველობის დეპარტამენტი")
                        {
                            user.diversion = "წვდომა შრომითი უფლებების ზედამხედველობის ყველა სამართველოზე";
                            user.section = "წვდომა შრომითი უფლებების ზედამხედველობის ყველა განყოფილებაზე";
                        }
                        else
                        {
                            user.diversion = "წვდომა შრომის უსაფრთხოებაზე ზედამხედველობის ყველა სამართველოზე";
                            user.section = "წვდომა შრომის უსაფრთხოებაზე ზედამხედველობის ყველა განყოფილებაზე";
                        }
                    }
                    else if (user.level == 5)
                    {
                        user.role = "departmentHead";
                        user.position = "დეპარტამენტის უფროსი";
                        if (user.department == "შრომითი უფლებების ზედამხედველობის დეპარტამენტი")
                        {
                            user.diversion = "წვდომა შრომითი უფლებების ზედამხედველობის ყველა სამართველოზე";
                            user.section = "წვდომა შრომითი უფლებების ზედამხედველობის ყველა განყოფილებაზე";
                        }
                        else
                        {
                            user.diversion = "წვდომა შრომის უსაფრთხოებაზე ზედამხედველობის ყველა სამართველოზე";
                            user.section = "წვდომა შრომის უსაფრთხოებაზე ზედამხედველობის ყველა განყოფილებაზე";
                        }

                    }
                    else if (user.level == 4)
                    {
                        user.role = "divisionHead";
                        user.position = "სამართველოს უფროსი";
                        user.section = $"წვდომა {user.diversion}-ს ყველა განყოფილებაზე";
                        if (user.department == "შრომითი უფლებების ზედამხედველობის დეპარტამენტი")
                        {
                        }
                        else
                        {
                            user.section = "წვდომა შრომის უსაფრთხოებაზე ზედამხედველობის ყველა განყოფილებაზე";
                        }
                    }
                    else if (user.level == 3)
                    {
                        user.role = "groupManager";

                        user.position = "განყოფილების უფროსი";
                    }
                    else if (user.level == 2)
                    {
                        user.role = "seniorInspector";

                        user.position = "უფროსი შრომის ინსპექტორი";
                    }
                    else if (user.level == 1)
                    {
                        user.role = "inspector";

                        user.position = "შრომის ინსპექტორი";
                    }

                    user.status = "აქტიური";

                    // Call the repository's CreateAsync method to save the user

                    user.rating = "5.0";
                    user.giveWarnings = 0;
                    user.amountOfFinedCompanies = 0;
                    user.stoppedCompanyAmount = 0;
                    await _repository.UpdateAsync(user.id, user);

                }
            return Ok("user info edit succesfully");

            }else
            {
                return NotFound("there is some problem we cant update ");
            }
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
            if(string.IsNullOrEmpty(sessionToken))
            {
                return NotFound("Token is Expired");

            }
            var userId = await _redisExample.GetUserIdBySessionToken(sessionToken);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound("there is finding user in redis via token problem");
            }
            var userInfo = await _repository.GetByIdAsync(userId);
            if(userInfo == null)
            {
                return NotFound("in database that user doesnt exist");
            }

            return Ok(userInfo);
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
        public async Task<IActionResult> Create(Users user)
        {
            Console.WriteLine(user);
            //Hash the plain text password using BCrypt
            user.passwordHash = BCrypt.Net.BCrypt.HashPassword(user.passwordHash);

            if(user.level == 7)
            {
                user.position = "მთავარი შრომის ინსპექტორი";
                user.role = "superAdmin";
                user.department = "წვდომა ყველა დეპარტამენტზე";
                user.diversion = "წვდომა ყველა სამართველოზე";
                user.section = "წვდომა ყველა განყოფილებაზე";
            }else if(user.level == 6 ) 
            {
                    user.role = "admin";
                if (user.department == "შრომითი უფლებების ზედამხედველობის დეპარტამენტი")
                {
                user.position = "მთავარი შრომის ინსპექტორის 1-ლი მოადგილე";

                }else
                {
                    user.position = "მთავარი შრომის ინსპექტორის მოადგილე";
                }
                if(user.department == "შრომითი უფლებების ზედამხედველობის დეპარტამენტი")
                {
                    user.diversion = "წვდომა შრომითი უფლებების ზედამხედველობის ყველა სამართველოზე";
                    user.section = "წვდომა შრომითი უფლებების ზედამხედველობის ყველა განყოფილებაზე";
                }else
                {
                    user.diversion = "წვდომა შრომის უსაფრთხოებაზე ზედამხედველობის ყველა სამართველოზე";
                    user.section = "წვდომა შრომის უსაფრთხოებაზე ზედამხედველობის ყველა განყოფილებაზე";
                }
            }else if (user.level == 5)
            {
                user.role = "departmentHead";
                user.position = "დეპარტამენტის უფროსი";
                if(user.department == "შრომითი უფლებების ზედამხედველობის დეპარტამენტი")
                {
                    user.diversion = "წვდომა შრომითი უფლებების ზედამხედველობის ყველა სამართველოზე";
                    user.section = "წვდომა შრომითი უფლებების ზედამხედველობის ყველა განყოფილებაზე";
                }else
                {
                    user.diversion = "წვდომა შრომის უსაფრთხოებაზე ზედამხედველობის ყველა სამართველოზე";
                    user.section = "წვდომა შრომის უსაფრთხოებაზე ზედამხედველობის ყველა განყოფილებაზე";
                }

            }
            else if (user.level == 4)
            {
                user.role = "divisionHead";
                user.position = "სამართველოს უფროსი";
                    user.section = $"წვდომა {user.diversion}-ს ყველა განყოფილებაზე";
                if (user.department == "შრომითი უფლებების ზედამხედველობის დეპარტამენტი")
                {
                }
                else
                {
                    user.section = "წვდომა შრომის უსაფრთხოებაზე ზედამხედველობის ყველა განყოფილებაზე";
                }
            }
            else if (user.level == 3)
            {
                user.role = "groupManager";
                
                user.position = "განყოფილების უფროსი";
            }else if (user.level == 2)
            {
                user.role = "seniorInspector";

                user.position = "უფროსი შრომის ინსპექტორი";
            }else if (user.level == 1)
            {
                user.role = "inspector";

                user.position = "შრომის ინსპექტორი";
            }

                user.status = "აქტიური";

            // Call the repository's CreateAsync method to save the user

            user.rating = "5.0";
            user.giveWarnings = 0;
            user.amountOfFinedCompanies = 0;
            user.stoppedCompanyAmount = 0;


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

    }
}
