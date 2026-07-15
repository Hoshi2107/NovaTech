using Microsoft.AspNetCore.SignalR;

namespace FakeTikTokShop.Hubs
{
    public class LivestreamHub : Hub
    {
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
