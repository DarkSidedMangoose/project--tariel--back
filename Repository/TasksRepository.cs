using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ASP.MongoDb.API.Repository
{
    public interface ITasksRepository: IRepository<Tasks>
    {

    }


    public class TasksRepository : Repository<Tasks>, ITasksRepository
    {
        public TasksRepository(MongoClient client, IOptions<MongoDbSettings> settings)
       : base(client, settings) { }
    }
}
