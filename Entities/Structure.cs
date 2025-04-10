using System;
using System.Security.Cryptography;
using MongoDB.Bson.Serialization.Attributes;

namespace ASP.MongoDb.API.Entities
{
    public class Structure
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]

        public string? id { get; set; }
        [BsonElement("levels")]
        public List<int> Levels { get; set; }
        public SystemOfStruct systemStruct { get; set; }



        public class SystemOfStruct
        {
            public Department1 department1 { get; set; }
            public Department2 department2 { get; set; }
        }

        public class Department1
        {
            public string name { get; set; }
            public Diversions diversions { get; set; }
        }
        public class Department2
        {
            
            public string name { get; set; }
            public Diversions diversions { get; set; }
        }
    }

        public class Diversions
        {
            public Diversion diversion1 { get; set; }
            public Diversion diversion2 { get; set; }
            public Diversion diversion3 { get; set; }
            public Diversion diversion4 { get; set; }
            public Diversion? diversion5 { get; set; }

    }
        public class Diversion { 
            public string name { get; set; }
            public Sections sections { get; set; }
        }
        public class Sections
        {
            public string section1 { get; set; }
            public string? section2 { get; set; }
        }
}



