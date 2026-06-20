using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Globalization;


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


        [HttpGet("getDate")]
        public async Task<IActionResult> GetDate(DateTime? inputDate = null)
        {
            // Use input date if provided, otherwise current UTC
            var utcDate = inputDate ?? DateTime.UtcNow;

            return await MethodToTakeDesireInfoForCalendar(utcDate);
        }
        [HttpGet("getCalendarDaysMonthAndEvents")]
        public async Task<IActionResult> GetCurrentInfo([FromQuery] DateTime date)
        {
            return await MethodToTakeDesireInfoForCalendar(date);
        }


        [HttpGet("getAllEvents")]
        public async Task<IActionResult> GetAllEvent()
        {
            var allData = await _calendarRepository.GetAllAsync();
            return Ok(allData);

        }

        [HttpGet("getNextEvent")]
        public async Task<IActionResult> GetNextEvent([FromQuery] DateTime referenceDate)
        {
            Console.WriteLine(referenceDate);
            var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
            var georgiaReference = TimeZoneInfo.ConvertTimeFromUtc(referenceDate, georgiaTimeZone);

            var nextEvent = await _calendarRepository.GetNextEventAsync(georgiaReference);

            if (nextEvent == null)
                return NotFound("No upcoming events after the given date.");

            if (nextEvent?.date == null)
                return BadRequest("Event has no valid date.");

            return await MethodToTakeDesireInfoForCalendar(nextEvent.date.Value);
        }

        private async Task<IActionResult> MethodToTakeDesireInfoForCalendar(DateTime date)
        {
            // Convert input date to Georgia timezone
            var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
            var georgiaDate = TimeZoneInfo.ConvertTimeFromUtc(date, georgiaTimeZone);

            // Georgian culture
            var georgianCulture = new CultureInfo("ka-GE");

            // Build 7-day window (3 before, the date itself, 3 after)
            var days = Enumerable.Range(-3, 7)
                .Select(offset =>
                {
                    var d = georgiaDate.AddDays(offset);
                    return new
                    {
                        FullDate = d.ToString("yyyy-MM-dd"),
                        DateIso = d.ToString("o"),
                        Year = d.Year,
                        Month = d.Month,
                        Day = d.Day,
                        WeekDay = d.ToString("dddd", georgianCulture) // weekday in Georgian
                    };
                })
                .ToList();

            // Collect distinct month names involved (in Georgian)
            var monthsInvolved = days
                .Select(d => new DateTime(d.Year, d.Month, 1).ToString("MMMM", georgianCulture))
                .Distinct()
                .ToList();

            var result = new
            {
                Days = days,
                MonthsInvolved = monthsInvolved
            };

            return Ok(result);
        }

    }
}
