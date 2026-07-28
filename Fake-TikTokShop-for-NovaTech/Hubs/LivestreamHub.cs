using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FakeTikTokShop.Hubs
{
    public class LivestreamHub : Hub
    {
        // Track all connected SignalR connection IDs and their role
        // "viewer" = LiveViewer page, "host" = Livestream (streamer) page
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        /// <summary>
        /// Returns the number of VIEWER connections (excludes host connections).
        /// </summary>
        public static int GetViewerCount()
        {
            return _connections.Values.Count(role => role == "viewer");
        }

        // ====== CONNECTION LIFECYCLE ======

        public override async Task OnConnectedAsync()
        {
            // Client announces its role: viewer or host
            // Default to "viewer" on first connect — client calls SetRole() right after
            _connections[Context.ConnectionId] = "viewer";

            // Update ViewerCount in shared state immediately
            Controllers.LiveStreamState.ViewerCount = GetViewerCount();

            // Broadcast real viewer count to all clients
            await Clients.All.SendAsync("StatsUpdated",
                Controllers.LiveStreamState.ViewerCount,
                Controllers.LiveStreamState.LikesCount);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _connections.TryRemove(Context.ConnectionId, out _);

            // Update ViewerCount in shared state
            Controllers.LiveStreamState.ViewerCount = GetViewerCount();

            // Broadcast updated count to all remaining clients
            await Clients.All.SendAsync("StatsUpdated",
                Controllers.LiveStreamState.ViewerCount,
                Controllers.LiveStreamState.LikesCount);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client calls this immediately after connecting to declare its role: "viewer" or "host".
        /// Host connections are excluded from the viewer count.
        /// </summary>
        public async Task SetRole(string role)
        {
            _connections[Context.ConnectionId] = role == "host" ? "host" : "viewer";

            // Recalculate and broadcast the real count
            Controllers.LiveStreamState.ViewerCount = GetViewerCount();
            await Clients.All.SendAsync("StatsUpdated",
                Controllers.LiveStreamState.ViewerCount,
                Controllers.LiveStreamState.LikesCount);
        }

        // ====== RELAY METHODS ======

        // Streamer sends a frame → server broadcasts to ALL viewers
        public async Task SendFrame(string frameData)
        {
            await Clients.Others.SendAsync("ReceiveFrame", frameData);
        }

        // Streamer sends audio chunk → server broadcasts to ALL viewers
        public async Task SendAudio(string audioData)
        {
            await Clients.Others.SendAsync("ReceiveAudio", audioData);
        }

        // Broadcast live state change (start/stop)
        public async Task BroadcastLiveState(bool isLive, int viewerCount, int likesCount)
        {
            await Clients.All.SendAsync("LiveStateChanged", isLive, viewerCount, likesCount);
        }

        // Broadcast a new chat message to all viewers
        public async Task BroadcastComment(string username, string text, string color)
        {
            await Clients.All.SendAsync("ReceiveComment", username, text, color);
        }

        // Broadcast product list update to all viewers
        public async Task BroadcastProductsUpdate(object products)
        {
            await Clients.All.SendAsync("ProductsUpdated", products);
        }

        // Broadcast likes/viewer count update
        public async Task BroadcastStats(int viewerCount, int likesCount)
        {
            await Clients.All.SendAsync("StatsUpdated", viewerCount, likesCount);
        }
    }
}

