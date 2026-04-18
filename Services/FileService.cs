namespace ASP.MongoDb.API.Services
{
    public class FileService : IFileService
    {
        private readonly string _basePath;

        public FileService(IConfiguration config)
        {
            _basePath = config["Storage:BasePath"]
                ?? throw new ArgumentNullException(nameof(config), "Storage:BasePath არ არის კონფიგურირებული appsettings.json-ში");
        }

        /// აბრუნებს სამიზნე საქაღალდეს ქვეფოლდერის მიხედვით
        public string GetTargetFolder(string subFolder)
        {
            var folder = Path.Combine(_basePath, subFolder);
            Directory.CreateDirectory(folder); // ქმნის საქაღალდეს, თუ არ არსებობს
            return folder;
        }

        /// აღადგენს სრულ ფიზიკურ გზას შენახული შედარებითი URL-დან
        /// მაგალითი: "image-videos/xxx.jpg" =>  "C:\Storage\image-videos\xxx.jpg"
        public string GetFullFilePath(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                throw new ArgumentException("შედარებითი URL არ უნდა იყოს ცარიელი.");

            // ნორმალიზაცია და სახიფათო სიმბოლოების გაფილტვრა
            var cleanPath = relativeUrl.Replace("\\", "/").TrimStart('/');

            // უსაფრთხოება: გზის ტრავერსალის (path traversal) თავიდან აცილება
            if (cleanPath.Contains("..") ||
                cleanPath.StartsWith("/") ||
                cleanPath.Contains(":") ||
                cleanPath.Contains("\\"))
            {
                throw new ArgumentException("არასწორი ფაილის გზა.");
            }

            return Path.Combine(_basePath, cleanPath);
        }

        /// შლის ფაილს სერვერიდან  URL-ის მიხედვით
        public bool DeleteFile(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return false;

            try
            {
                var fullPath = GetFullFilePath(relativeUrl);

                if (!File.Exists(fullPath))
                    return true; // ფაილი უკვე არ არსებობს → წაშლილად ჩაეთვლება

                File.Delete(fullPath);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// აბრუნებს სწორ MIME ტიპს ბრაუზერისთვის
        public string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}