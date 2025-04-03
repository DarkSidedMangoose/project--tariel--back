using Microsoft.Extensions.Caching.Distributed;

namespace ASP.MongoDb.API.Services
{
    public class RedisExample
    {
        private readonly IDistributedCache _cache;

        public RedisExample(IDistributedCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        // Create a session for a user
       public async Task CreateSessionAsync(string userId, string sessionId, TimeSpan? expiration = null)
        {
            if(string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentNullException(nameof(sessionId));

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
            };

            await _cache.SetStringAsync(sessionId, userId, options);
            Console.WriteLine($"Session created for user {userId} with session ID {sessionId}");

        }

        public async Task<string?> GetUserIdBySessionToken(string sessionToken)
        {
            if(string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentNullException(nameof(sessionToken));
            }

            return await _cache.GetStringAsync($"session:{sessionToken}"); 

        }
        
    }

}


