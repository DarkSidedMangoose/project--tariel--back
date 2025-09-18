using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ASP.MongoDb.API.Repository
{
    public interface IUserRepository : IRepository<Users>
    {

    }


    public class UserRepository : Repository<Users>, IUserRepository
    {
        public UserRepository(MongoClient client, IOptions<MongoDbSettings> settings)
       : base(client, settings)
        {
        }
    }

}