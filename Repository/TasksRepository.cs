using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;

namespace ASP.MongoDb.API.Repository
{
    public interface ITasksRepository: IRepository<Tasks>
    {

    }


    public class TasksRepository : Repository<Tasks>, ITasksRepository
    {
        public TasksRepository(IOptions<MongoDbSettings> settings): base(settings) { 
        
        }
    }
}
