using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ASP.MongoDb.API.Entities
{
    public class StructureNd
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("levels")]
        public List<int> Levels { get; set; } = new List<int>();

        [BsonElement("departments")]
        public List<Department> Departments { get; set; } = new List<Department>();
        public class Department
        {
            [BsonElement("name")]
            public string Name { get; set; } = string.Empty;

            [BsonElement("diversions")]
            public List<Diversion> Diversions { get; set; } = new List<Diversion>();
        }
        public class Diversion
        {
            [BsonElement("name")]
            public string Name { get; set; } = string.Empty;

            [BsonElement("sections")]
            public List<string>? Sections { get; set; } = new List<string>();
        }

    }
}
