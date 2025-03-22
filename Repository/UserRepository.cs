using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;

namespace ASP.MongoDb.API.Repository
{
    public interface IUserRepository: IRepository<Users>
    {

    }

    
    public class UserRepository : Repository<Users>, IUserRepository
    {
        public UserRepository(IOptions<MongoDbSettings> settings) : base(settings)
        {

        }
    }
}
