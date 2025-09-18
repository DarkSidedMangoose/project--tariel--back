
using ASP.MongoDb.API.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ASP.MongoDb.API.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;

        public Repository(MongoClient client, IOptions<MongoDbSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<T>(typeof(T).Name);
        }


        // Retrieves all documents and convert the result to a list
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            // Find all documents and convert the result to a list
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            // Convert the string 'id' to an ObjectId before querying
            var objectId = new ObjectId(id);

            // Find the document where the '_id' matches the provided ObjectId
            return await _collection.Find(Builders<T>.Filter.Eq("_id", objectId)).FirstOrDefaultAsync();
        }

        // Insert a new document in the collection
        public  async Task CreateAsync(T entity) 
        {
            // Add the provided entity to the collection
            await _collection.InsertOneAsync(entity);
            
        }
        // Updates an existing document by its ID
        public async Task UpdateAsync(string id, T entity)
        {
            // Convert the string 'id' to an ObjectId before querying 
            var objectId = new ObjectId(id);

            // Replace the document where the '_id' matches the provided ID with the new entity
            await _collection.ReplaceOneAsync(Builders<T>.Filter.Eq("_id", objectId), entity);
        }

        // Deletes a document by its ID

        public async Task DeleteAsync(string id)
        {
            // Convert the string 'id' to an ObjectId before querying
            var objectId = new ObjectId(id);

            // Remove the document where the '_id' matches the provided ID
            await _collection.DeleteOneAsync(Builders<T>.Filter.Eq("_id", objectId));
        }
    }
}
