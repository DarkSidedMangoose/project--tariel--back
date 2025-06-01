using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ASP.MongoDb.API.Entities
{
    public class Tasks
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]

        public string? id {  get; set; }
       
        public ObjectIdentifierData? objectIdentifierData { get; set; }
        public Levels? dataFlow {  get; set; }
        public List<TaskLogEntry>? dataLogs { get; set; } = new List<TaskLogEntry>();
        public Addresses? addresses { get; set; }
        public Activityinformation? activityinformation { get; set; }
        public ActivityForm? activityForm { get; set; }
        
        public TaxPayerInfo? payerInfo { get; set; }


        public class ObjectIdentifierData
        {
            public string? identifyCode { get; set; }
            public string? fullName { get; set; }
            public string? parentOrganization { get; set; }
            public string? parentOrganizationFullName { get; set; }
        }

        public class TaxPayerInfo {
            public bool? VAT { get; set; }
            public string? fizPersonIncome { get; set; }

            public string? iurPersonIncomeRotation { get; set;}
            public int? employedCount { get; set; }
        }

        public class ActivityForm
        {
            public string? registerOrgan { get; set; }
            public string? form { get; set; }
            public string? govermentalRegisterDate { get; set; }
            public string? lastChangeDate { get; set; }

        }
        public class Activityinformation
        {
            public string? workingCode { get; set; }
            public string? workingDescription { get; set; }
            public string? groupedName { get; set; }

            public string? riskLevel { get; set; } 
        }
        public class Addresses
        {
            public string? region { get; set; }
            public string? factAddress { get; set; }
            public string? iurAddress { get; set; }
            public string? streetFactAddress { get; set; }
            public string? streetIurAddress { get; set; }
            public string? postalCode { get; set; }
            public List<string>? addressesOfFactActions { get; set; } = new List<string>();


        }

        
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

        public class Levels
        {
            public Level? level7 { get; set; }
            public Level? level6 { get; set; }
            public Level? level5 { get; set; }
            public Level? level4 { get; set; }
            public Level? level3 { get; set; }
            public Level? level2 { get; set; }
            public Level? level1 { get; set; }
        }

        public class Level
        {
            public string? userId { get; set; }
            public string? status { get; set; }
            public string? fromUserId { get; set; }
        }
    }
}
