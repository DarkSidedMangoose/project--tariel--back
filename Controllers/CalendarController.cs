using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Services;
using Microsoft.AspNetCore.Mvc;
using ASP.MongoDb.API.Entities;


namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarRepository _calendarRepository;

        public CalendarController(ICalendarRepository calendarRepository)
        {
            _calendarRepository = calendarRepository;
        }

        [HttpPost("addNew")]
        public async Task<IActionResult> AddNewEvent([FromBody] CalendarEvent arg)

        {
            Console.WriteLine(arg.title);
            if (arg == null)
            {
                return BadRequest("arg is null");
            }

             await _calendarRepository.CreateAsync(arg);

            Console.WriteLine("ssad");
            

            return Ok("everything work well"); // return the created event or its ID
        }
        

        [HttpGet("getAllEvents")]
        public async Task<IActionResult> GetAllEvent()
        {
            var allData = await _calendarRepository.GetAllAsync();
            return Ok(allData);

        }
        

    }
}
