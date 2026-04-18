namespace ASP.MongoDb.API.Services
{
    public interface IFileService
    {
        string GetTargetFolder(string subFolder);
        string GetFullFilePath(string relativeUrl);
        string GetContentType(string filePath);
        bool DeleteFile(string relativeUrl);

    }
}