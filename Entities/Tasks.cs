using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ASP.MongoDb.API.Entities
{
    public class Tasks
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]

        public string? id { get; set; }


        public string? workingCode { get; set; }
        public string? convicted { get; set; }
        public DateTime? timeSpan { get; set; }
        public string? registerDate { get; set; }
        public string? lawyer { get; set; }

        public bool? status { get; set; }
        public string? statusIdentifier { get; set; }
        public List<TaskLogEntry>? dataLogs { get; set; } = new List<TaskLogEntry>();
        public string? lawyerId { get; set; }
   


        public List<TaskAttachedData>? taskAttachedData { get; set; }
   
    public class TaskLogEntry
    {
        public int level { get; set; }
        public string? timestamp { get; set; }
        public string? addedByName { get; set; }
        public string? addedById { get; set; }
        public string? description { get; set; }
        public string? receiverName { get; set; }
        public string? receiverId { get; set; }
        public string? comment { get; set; }
        public string? imgUrl { get; set; }

    }

    public class TaskAttachedData
    {
        public DateTime timeSpan { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public string fileName { get; set; }

    }
    }



}



