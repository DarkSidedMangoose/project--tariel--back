using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ASP.MongoDb.API.Repository
{
    public interface IDataOfStructureRepository: IRepository<StructureNd>
    {

    }
    public class DataOfStuctureRepository : Repository<StructureNd>, IDataOfStructureRepository
    {
        public DataOfStuctureRepository(MongoClient client, IOptions<MongoDbSettings> settings)
       : base(client, settings) { }
    }
}
