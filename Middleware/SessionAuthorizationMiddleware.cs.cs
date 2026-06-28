    using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;
namespace ASP.MongoDb.API.Middleware;

public class SessionAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;

    public SessionAuthorizationMiddleware(RequestDelegate next, IDistributedCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        // Allow the /login and /verify-mfa endpoints to bypass authentication
        if (path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/auth/verify-mfa", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/auth/reset-mfa", StringComparison.OrdinalIgnoreCase)
           
            )
        {
            await _next(context);
            return;
        }

        var sessionToken = context.Request.Cookies["session-token"];
        if (string.IsNullOrEmpty(sessionToken))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Session token is missing.");
            return;
        }

       var userId = await _cache.GetStringAsync($"session:{sessionToken}");
        if (userId == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized access.");
            return;
        }

        await _next(context);
    }

}
