using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Globalization;

namespace ASP.MongoDb.API.Repository
{
    
        public interface ICalendarRepository : IRepository<CalendarEvent>
        {

        }


        public class CalendarRepository : Repository<CalendarEvent>, ICalendarRepository
        {
            public CalendarRepository(MongoClient client, IOptions<MongoDbSettings> settings)
           : base(client, settings)
            {
            }
        }
    
}
