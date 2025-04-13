using MongoDB.Bson.Serialization.Attributes;

namespace ASP.MongoDb.API.Models
{
    public class CreateNewUser
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]

        public string? id { get; set; }
        public string username { get; set; }
        public string passwordHash { get; set; }

        public string fullName { get; set; }
        
        public int level { get; set; }

        public string department { get; set; }
        public string diversion { get; set; }

        public string section { get; set; }
    }
}
