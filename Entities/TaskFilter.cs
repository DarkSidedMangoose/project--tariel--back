namespace ASP.MongoDb.API.Entities
{
    public class SearchInterface
    {
        public ObjectIdentifierData ObjectIdentifierData { get; set; }
        public Addresses Addresses { get; set; }
        public ActivityInformation ActivityInformation { get; set; }
        public ActivityForm ActivityForm { get; set; }
        public PayerInfo PayerInfo { get; set; }
    }

    public class ObjectIdentifierData
    {
        public string FullName { get; set; }
        public string IdentifyCode { get; set; }
    }

    public class Addresses
    {
        public string Region { get; set; }
        public string AdressesOfFactActions { get; set; }
    }

    public class ActivityInformation
    {
        public string WorkingCode { get; set; }
        public string WorkingDescription { get; set; }
        public string RiskLevel { get; set; }
    }

    public class ActivityForm
    {
        public string Form { get; set; }
        public string GovermentalRegisterDate { get; set; }
    }

    public class PayerInfo
    {
        public string IurPersonIncomeRotation { get; set; }
        public int EmployeeMin { get; set; }
        public int EmployeeMax { get; set; }
    }

    public class DataLog
    {
        public string Timestamp { get; set; }
        public string AddedByName { get; set; }
        public string Description { get; set; }
        public string ReceiverName { get; set; }
        public string ImgUrl { get; set; }
        public string Comment { get; set; }
    }

    public class ReturnForBaseShowdown
    {
        public string Id { get; set; }
        public string IdentifyCode { get; set; }
        public string FullName { get; set; }
        public string Region { get; set; }
        public string FactAddress { get; set; }
        public string IurPersonIncomeRotation { get; set; }
        public string WorkingDescription { get; set; }
        public string RiskLevel { get; set; }
        public List<DataLog> DataLogs { get; set; }
    }
}

