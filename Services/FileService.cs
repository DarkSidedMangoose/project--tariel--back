namespace ASP.MongoDb.API.Services
{
    public class FileService : IFileService
    {
        private readonly string _basePath;

        public FileService(IConfiguration config)
        {
            _basePath = config["Storage:BasePath"]
                ?? throw new ArgumentNullException(nameof(config), "Storage:BasePath is not configured in appsettings.json");
        }

        public string GetTargetFolder(string subFolder)
        {
            var folder = Path.Combine(_basePath, subFolder);
            Directory.CreateDirectory(folder);   // creates folder if it doesn't exist
            return folder;
        }

        // ====================== NEW METHODS ======================

        /// <summary>
        /// Reconstructs the full physical path from the stored relative URL
        /// Example: "image-videos/xxxxxxxx-xxxx.jpg" → "C:\Storage\image-videos\xxxxxxxx-xxxx.jpg"
        /// </summary>
        public string GetFullFilePath(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                throw new ArgumentException("Relative URL cannot be empty.");

            // Normalize path and remove dangerous patterns
            var cleanPath = relativeUrl.Replace("\\", "/").TrimStart('/');

            // SECURITY: Prevent path traversal attacks (.. / \ : etc.)
            if (cleanPath.Contains("..") ||
                cleanPath.StartsWith("/") ||
                cleanPath.Contains(":") ||
                cleanPath.Contains("\\"))
            {
                throw new ArgumentException("Invalid file path.");
            }

            return Path.Combine(_basePath, cleanPath);
        }

        /// <summary>
        /// Returns correct MIME type for the browser
        /// </summary>
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