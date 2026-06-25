using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using static ASP.MongoDb.API.Entities.templateChildValue;

namespace ASP.MongoDb.API.Entities
{
   
    
    public class CalendarEvent
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? id { get; set; }


        public string? title { get; set; }
        public string? time { get; set; }
        public string? location { get; set; }
        public string? color { get; set; }
        public DateTime? date { get; set; }
        public int day { get; set; }
        public double? duration { get; set; }
        public double? startHour { get; set; }




    }
    public class TakeWeeksDay
    {
        public DateOnly week1 { get; set; }
        public DateOnly week2 { get; set; }
        public DateOnly week3 { get; set; }
        public DateOnly week4 { get; set; }
        public DateOnly week5 { get; set; }
        public DateOnly week6 { get; set; }
        public DateOnly week7 { get; set; }

    }

    


}
