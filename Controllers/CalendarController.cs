using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Repository;
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
            if (arg == null)
                return BadRequest("Event is null");
            if (arg.date.HasValue)
            {
                // Ensure it's stored as UTC midnight
                arg.date = DateTime.SpecifyKind(arg.date.Value, DateTimeKind.Utc);
            }
            await _calendarRepository.CreateAsync(arg);
            return Ok("Event created successfully");
        }

        [HttpGet("getDate")]
        public async Task<IActionResult> GetDate(DateTime? inputDate = null)
        {
            var utcDate = inputDate ?? DateTime.UtcNow;
            return await MethodToTakeDesireInfoForCalendar(utcDate);
        }

        [HttpGet("getCalendarDaysMonthAndEvents")]
        public async Task<IActionResult> GetCurrentInfo([FromQuery] DateTime date)
        {
            return await MethodToTakeDesireInfoForCalendar(date);
        }

        [HttpGet("getCurrentWeekEvents")]
        public async Task<IActionResult> GetAllEvent([FromQuery] TakeWeeksDay datas)
        {
            Console.WriteLine(datas.week1);
            var allData = await _calendarRepository.GetAllAsync();

            // Convert incoming DateTime values to DateOnly
            var weekDates = new List<DateOnly>
            {
                DateOnly.FromDateTime(datas.week1),
                DateOnly.FromDateTime(datas.week2),
                DateOnly.FromDateTime(datas.week3),
                DateOnly.FromDateTime(datas.week4),
                DateOnly.FromDateTime(datas.week5),
                DateOnly.FromDateTime(datas.week6),
                DateOnly.FromDateTime(datas.week7)
            };

            var filteredEvents = allData
                .Where(e => e.date.HasValue &&
                            weekDates.Contains(DateOnly.FromDateTime(e.date.Value)))
                .ToList();

            return Ok(filteredEvents);
        }

        [HttpGet("getNextEvent")]
        public async Task<IActionResult> GetNextEvent([FromQuery] DateTime referenceDate)
        {
            var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
            var georgiaReference = TimeZoneInfo.ConvertTimeFromUtc(referenceDate, georgiaTimeZone);

            var nextEvent = await _calendarRepository.GetNextEventAsync(georgiaReference);

            if (nextEvent == null)
                return NotFound("No upcoming events after the given date.");

            if (nextEvent?.date == null)
                return BadRequest("Event has no valid date.");

            return await MethodToTakeDesireInfoForCalendar(nextEvent.date.Value);
        }

        [HttpGet("getPreviousEvent")]
        public async Task<IActionResult> GetPreviousEvent([FromQuery] DateTime referenceDate)
        {
            var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
            var georgiaReference = TimeZoneInfo.ConvertTimeFromUtc(referenceDate, georgiaTimeZone);

            var nextEvent = await _calendarRepository.GetPreviousEventAsync(georgiaReference);

            if (nextEvent == null)
                return NotFound("No upcoming events after the given date.");

            if (nextEvent?.date == null)
                return BadRequest("Event has no valid date.");

            return await MethodToTakeDesireInfoForCalendar(nextEvent.date.Value);
        }

        private async Task<IActionResult> MethodToTakeDesireInfoForCalendar(DateTime date)
        {
            var georgiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time");
            var georgiaDate = TimeZoneInfo.ConvertTimeFromUtc(date, georgiaTimeZone);

            var georgianCulture = new CultureInfo("ka-GE");

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
                        WeekDay = d.ToString("dddd", georgianCulture)
                    };
                })
                .ToList();

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

    // Updated TakeWeeksDay to use DateTime for binding
    public class TakeWeeksDay
    {
        public DateTime week1 { get; set; }
        public DateTime week2 { get; set; }
        public DateTime week3 { get; set; }
        public DateTime week4 { get; set; }
        public DateTime week5 { get; set; }
        public DateTime week6 { get; set; }
        public DateTime week7 { get; set; }
    }
}
