using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Security.Claims;

namespace ASP.MongoDb.API.SignalIR
{
    public class NotificationHub : Hub
    {
        // In-memory mapping between user IDs (from the 'sub' claim) and connection IDs
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();

        // Called when a client connects
        public override Task OnConnectedAsync()
        {
            // Retrieve the user ID from the 'sub' claim
            var userId = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"User Connected: {userId}, ConnectionId: {Context.ConnectionId}");

            if (userId != null)
            {
                UserConnections[userId] = Context.ConnectionId; // Map user ID to connection ID
            }
            else
            {
                Console.WriteLine("User ID is null. Connection not mapped.");
            }
            return base.OnConnectedAsync();
        }

        // Called when a client disconnects
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // Retrieve the user ID from the 'sub' claim
            var userId = Context.User?.FindFirst("sub")?.Value;

            if (userId != null)
            {
                UserConnections.TryRemove(userId, out _); // Remove user ID from mapping
                Console.WriteLine($"User Disconnected: {userId}");
            }

            return base.OnDisconnectedAsync(exception);
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
    }
}
