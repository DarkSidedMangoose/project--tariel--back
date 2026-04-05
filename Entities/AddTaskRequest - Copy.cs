namespace ASP.MongoDb.API.Entities
{
    public class AddTaskRequest
    {
        public Tasks FirstArgument { get; set; }
        public AuthenticatedUserInfo SecondArgument { get; set; }
    }

    public class AuthenticatedUserInfo
    {
        public string? id { get; set; }
        public string? fullname { get; set; }
        public int? level { get; set; }
    }
}
