using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;


namespace ASP.MongoDb.API.Repository
{

    public interface IStructureOfSystemRepository: IRepository<Structure>
    {

    }

    public class StructureOfSystemRepository : Repository<Structure>, IStructureOfSystemRepository

    {
        public StructureOfSystemRepository(MongoClient client, IOptions<MongoDbSettings> settings)
       : base(client, settings) { }
    }
}
