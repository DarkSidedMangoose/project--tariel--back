using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ASP.MongoDb.API.Entities
{
    public class Tasks
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]

        public string? id {  get; set; }
        public string identifyCode { get; set; }
        public string wholeName {  get; set; }
        public string fizAddress { get; set; }
        public string region { get; set; }
        public string turnover {  get; set; }
        public string jobType { get; set; }
        public string riskLevel { get; set; }
        public Levels dataFlow {  get; set; }
        public List<TaskLogEntry> dataLogs { get; set; } = new List<TaskLogEntry>();

        public class TaskLogEntry
        {
            public string timestamp { get; set; }
            public string addedBy { get; set; }
            public string description { get; set; }
            public string? receiverTo { get; set; }

        }

        public class Levels
        {
            public Level level5 { get; set; }
            public Level level4 { get; set; }
            public Level level3 { get; set; }
            public Level level2 { get; set; }
            public Level level1 { get; set; }
        }

        public class Level
        {
            public string userId { get; set; }
            public string status { get; set; }
            public string? fromUserId { get; set; }
        }
    }
}
