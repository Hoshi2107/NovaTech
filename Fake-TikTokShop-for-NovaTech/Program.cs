using FakeTikTokShop.Hubs;
using FakeTikTokShop.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add SignalR for real-time livestream streaming (replaces HTTP polling)
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 1024 * 5; // 5MB - enough for video frames + audio
    options.EnableDetailedErrors = true;
});

// SQLite Database Configuration
var connectionString = builder.Configuration.GetConnectionString("SQLiteConnection") ?? "Data Source=tiktok_shop_fake.db";
builder.Services.AddDbContext<TikTokDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

// Auto-create SQLite database and tables on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TikTokDbContext>();
    try
    {
        context.Database.EnsureCreated();
        
        // Check if LivestreamProducts table exists, if not it will throw an exception
        try
        {
            _ = context.LivestreamProducts.Count();
        }
        catch (Exception)
        {
            // Table doesn't exist, let's recreate the database to apply new schema
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        // Seed default products if cache is empty
        if (!context.ProductCaches.Any())
        {
            context.ProductCaches.AddRange(new List<TikTokProductCache>
            {
                new() { ProductId = 1, Name = "Laptop Dell XPS 13 9320 Plus", Sku = "DELL-XPS13-9320", Price = 38500000, Stock = 15, ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?q=80&w=300" },
                new() { ProductId = 2, Name = "Laptop ASUS ROG Strix G16", Sku = "ASUS-ROG-G16", Price = 32900000, Stock = 8, ImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?q=80&w=300" },
                new() { ProductId = 3, Name = "MacBook Air M3 13-inch (8GB/256GB)", Sku = "MAC-AIR-M3", Price = 27990000, Stock = 20, ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?q=80&w=300" },
                new() { ProductId = 4, Name = "Bàn phím cơ Akko 3098B Multi-mode", Sku = "AKKO-3098B", Price = 2150000, Stock = 45, ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?q=80&w=300" },
                new() { ProductId = 5, Name = "Chuột Gaming Logitech G502 X Plus", Sku = "LOGI-G502X", Price = 3450000, Stock = 30, ImageUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?q=80&w=300" },
                new() { ProductId = 6, Name = "Tai nghe HyperX Cloud III Wireless", Sku = "HYPERX-CLOUD3", Price = 2990000, Stock = 25, ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?q=80&w=300" }
            });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating SQLite database: {ex.Message}");
    }
}

// Configure HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// SignalR Hub route
app.MapHub<LivestreamHub>("/hubs/livestream");

// Route mappings
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
