using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;

namespace ASP.MongoDb.API.Repository
{
    public interface IDataOfStructureRepository: IRepository<StructureNd>
    {

    }
    public class DataOfStuctureRepository : Repository<StructureNd>, IDataOfStructureRepository
    {
        public DataOfStuctureRepository(IOptions<MongoDbSettings> settings) : base(settings) { 
        
        }
    }
}
