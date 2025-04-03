using ASP.MongoDb.API.Models;
using ASP.MongoDb.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

[ApiController]
[Route("[controller]")]
public class RedisTestController : ControllerBase
{
    private readonly IDistributedCache _cache;

    public RedisTestController(IDistributedCache cache)
    {
        _cache = cache;
    }

    [HttpGet("setSession")]
    public async Task<IActionResult> SetSession([FromBody] SentTaskRequest request)
    {
        if (string.IsNullOrEmpty(request.taskId) || string.IsNullOrEmpty(request.receiveUserId))
        {
            return BadRequest("taskId and receiveUserId are required.");
        }
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
        await _cache.SetStringAsync(request.taskId, request.receiveUserId, options);
        return Ok($"Session created for user {request.receiveUserId} with task ID {request.taskId}");
    }



}