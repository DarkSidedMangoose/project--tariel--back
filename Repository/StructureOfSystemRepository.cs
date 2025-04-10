using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;


namespace ASP.MongoDb.API.Repository
{

    public interface IStructureOfSystemRepository: IRepository<Structure>
    {

    }

    public class StructureOfSystemRepository : Repository<Structure>, IStructureOfSystemRepository

    {
        public StructureOfSystemRepository(IOptions<MongoDbSettings> settings) : base(settings)
        {

        }
    }
}
