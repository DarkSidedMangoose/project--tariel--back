using Microsoft.Extensions.Caching.Distributed;

namespace ASP.MongoDb.API.Services
{
    public class LoginAttemptService
    {
        private readonly IDistributedCache _cache;
        private readonly int _maxAttempts = 5;
        private readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(15);


        public LoginAttemptService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<bool> RegisterFailedAttemptAsync(string username)
        {
            var key = $"login:attemplts:{username}";
            var attempts = await _cache.GetStringAsync(key);
            int count = string.IsNullOrEmpty(attempts) ? 0 : int.Parse(attempts);
            count++;

            await _cache.SetStringAsync(key, count.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                }
                );
            
            if (count >= _maxAttempts)
            {
                await _cache.SetStringAsync($"login:lockout:{username}", "true",
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _lockoutDuration
                    }
                    );
                return false;
            }

            return true;
        }

        public async Task<bool> IsLockedOutAsync(string username)
        {
            var lockout = await _cache.GetStringAsync($"login:lockout:{username}");
            return lockout == "true";
        }

    }

}
