namespace ASP.MongoDb.API.Entities
{
    public class AddTaskRequest
    {
        public FirstArgumentClass FirstArgument { get; set; }
        public AuthenticatedUserInfo SecondArgument { get; set; }
    }

    public class FirstArgumentClass { 
        public AddNew? addNew { get; set; }
    }

    public class AddNew {
        public string workingCode { get; set; }
     public string convicted { get; set; }
        public string registerDate { get; set; }
        public string lawyer { get; set; }
    
    }

    public class AuthenticatedUserInfo
    {
        public string? id { get; set; }
        public string? fullname { get; set; }
        public int? level { get; set; }
    }
}
