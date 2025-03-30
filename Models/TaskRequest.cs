using MongoDB.Bson.Serialization.Attributes;

namespace ASP.MongoDb.API.Models
{
    public class SentTaskRequest
    {
        

        public string taskId { get; set; }
        public string receiveUserId { get; set; }
        
        
    }
    public class OverTaskRequest
    {
        public string taskId { get; set; }
    }
}
