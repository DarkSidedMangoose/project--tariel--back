using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Globalization;

namespace ASP.MongoDb.API.Repository
{
    
        public interface ICalendarRepository : IRepository<CalendarEvent>
        {
        Task<CalendarEvent?> GetNextEventAsync(DateTime referenceDate);
        Task<CalendarEvent?> GetPreviousEventAsync(DateTime referenceDate);
        Task<List<CalendarEvent>> GetEventsInRangeAsync(DateTime start, DateTime end);

    }


    public class CalendarRepository : Repository<CalendarEvent>, ICalendarRepository
    {
        private readonly IMongoCollection<CalendarEvent> _calendarCollection;

        public CalendarRepository(MongoClient client, IOptions<MongoDbSettings> settings)
            : base(client, settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _calendarCollection = database.GetCollection<CalendarEvent>(nameof(CalendarEvent));
        }

        public async Task<CalendarEvent?> GetNextEventAsync(DateTime referenceDate)
        {
            var filter = Builders<CalendarEvent>.Filter.Gt(e => e.date, referenceDate);
            var sort = Builders<CalendarEvent>.Sort.Ascending(e => e.date);

            return await _calendarCollection
                .Find(filter)
                .Sort(sort)
                .FirstOrDefaultAsync();
        }

        public async Task<CalendarEvent?> GetPreviousEventAsync(DateTime referenceDate)
        {
            // Find events with a date less than the reference
            var filter = Builders<CalendarEvent>.Filter.Lt(e => e.date, referenceDate);

            // Sort descending so the most recent before referenceDate comes first
            var sort = Builders<CalendarEvent>.Sort.Descending(e => e.date);

            return await _calendarCollection
                .Find(filter)
                .Sort(sort)
                .FirstOrDefaultAsync();
        }
        public async Task<List<CalendarEvent>> GetEventsInRangeAsync(DateTime start, DateTime end)
        {
            var filter = Builders<CalendarEvent>.Filter.And(
                Builders<CalendarEvent>.Filter.Gte(e => e.date, start),
                Builders<CalendarEvent>.Filter.Lte(e => e.date, end)
            );

            return await _calendarCollection
                .Find(filter)
                .SortBy(e => e.date)
                .ToListAsync();
        }
    }


}
