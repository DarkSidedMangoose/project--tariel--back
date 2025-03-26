using MongoDB.Bson.Serialization.Attributes;

namespace ASP.MongoDb.API.Models
{
    public class TaskRequest
    {
        

        public string taskId { get; set; }
        public string receiveUserId { get; set; }
        
        
    }
}
