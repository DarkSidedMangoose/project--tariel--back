using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Security.Claims;
using ASP.MongoDb.API.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace ASP.MongoDb.API.SignalIR
{
    public class NotificationHub : Hub
    {
        

        // In-memory mapping between user IDs (from the 'sub' claim) and connection IDs
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();
        private readonly IDistributedCache _cache;

        public NotificationHub(IDistributedCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        // Called when a client connects
        public override async Task OnConnectedAsync()
        {
            // Retrieve the session token from cookies
            var userId = await UserId();
            UserConnections[userId] = Context.ConnectionId;
            Console.WriteLine($"User Connected: {userId}, ConnectionId: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        // Called when a client disconnects
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Retrieve the user ID from the session
            var userId = await UserId();

            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections.TryRemove(userId, out _); // Remove user ID from mapping
                Console.WriteLine($"User Disconnected: {userId}");
            }
            else
            {
                Console.WriteLine("User ID could not be retrieved during disconnection.");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Send data to a specific user based on their user ID
        public async Task SendDataToUser(string userId, string message)
        {
            if (UserConnections.TryGetValue(userId, out var connectionId))
            {
                Console.WriteLine($"Sending message to user: {userId}, ConnectionId: {connectionId}");
                await Clients.Client(connectionId).SendAsync("ReceiveData", message); // Send message to specific client
            }
            else
            {
                Console.WriteLine($"User ID {userId} is not connected.");
            }
        }

        // Retrieve the connection ID for a specific user ID
        public static string? GetConnectionId(string userId)
        {
            UserConnections.TryGetValue(userId, out var connectionId);
            return connectionId; // Return connection ID if exists
        }
        public void LogConnectedUsers()
        {
            Console.WriteLine("Connected Users:");
            foreach (var user in UserConnections)
            {
                Console.WriteLine($"UserId: {user.Key}, ConnectionId: {user.Value}");
            }
        }

        public async Task<string?> UserId()
        {
            var sessionToken = Context.GetHttpContext()?.Request.Cookies["session-token"];
            if (string.IsNullOrEmpty(sessionToken))
            {
                Console.WriteLine("Session token is missing. Connection not mapped.");
                return null; // Return null if session token is missing
            }

            var userId = await _cache.GetStringAsync($"session:{sessionToken}");
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine($"User ID not found in Redis for session token: {sessionToken}. Connection not mapped.");
                return null; // Return null if user ID is not found in Redis
            }

            return userId; // Return the user ID if found
        }

    }
}
