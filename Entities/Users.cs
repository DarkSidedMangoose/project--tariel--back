using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ASP.MongoDb.API.Entities
{
    public class Users
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]

        public string? id { get; set; }
        public string fullname { get; set; }
        public string username { get; set; }
        public string passwordHash { get; set; }
        public string role { get; set; }
        public string diversion { get; set; }
        public string position { get; set; }
        public int level { get; set; }
        public string imgUrl { get; set; }
        public string? department { get; set; }
    }
}
