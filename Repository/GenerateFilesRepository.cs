using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ASP.MongoDb.API.Repository
{
    public interface IGenerateFilesRepository : IRepository<GenerateFiles> { }
    public class GenerateFilesRepository : Repository<GenerateFiles>, IGenerateFilesRepository
    {
        public GenerateFilesRepository(MongoClient client, IOptions<MongoDbSettings> settings)
       : base(client, settings) { }
    }
}
