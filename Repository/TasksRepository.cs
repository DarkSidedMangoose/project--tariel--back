using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ASP.MongoDb.API.Repository
{
    public interface ITasksRepository : IRepository<Tasks>
    {
        Task<IEnumerable<Tasks>> GetPagedTasksAsync(
            FilterDefinition<Tasks> filter,
            int skip,
            int take,
            string authenticatedUserLevel // <-- add parameter
        );
    }

    public class TasksRepository : Repository<Tasks>, ITasksRepository
    {
        public TasksRepository(MongoClient client, IOptions<MongoDbSettings> settings)
            : base(client, settings) { }

        public async Task<IEnumerable<Tasks>> GetPagedTasksAsync(
            FilterDefinition<Tasks> filter,
            int skip,
            int take,
            string authenticatedUserLevel // e.g. "level3"
        )
        {
            // Build the sort path dynamically based on the user’s level
            var sortField = $"dataFlow.{authenticatedUserLevel}.timeSpan";

            return await _collection
                .Find(filter)
                .Sort(Builders<Tasks>.Sort.Descending(sortField)) // newest first by user’s level
                .Skip(skip)
                .Limit(take)
                .ToListAsync();
        }
    }
}
