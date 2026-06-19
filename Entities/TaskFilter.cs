namespace ASP.MongoDb.API.Models
{
    public class SearchInterface
    {
        public Toggle toggle { get; set; } = new Toggle();
        public SearchData datas { get; set; } = new SearchData();
    }

    public class SearchData
    {
        public string? workingCode { get; set; }
        public string? convicted { get; set; }
        public string? registerDate { get; set; }
        public string? lawyer { get; set; }
    }

    public class Toggle
    {
        public bool toggleValue { get; set; }
    }
}
