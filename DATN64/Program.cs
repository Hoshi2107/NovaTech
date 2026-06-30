using DATN64.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(context);
    // Force update iPhone 15 image to correct local uploads path
    try
    {
        context.Database.ExecuteSqlRaw("UPDATE dbo.SanPham SET HinhAnh = '/uploads/products/33c5b785-4ba9-4ecf-99fe-0ff1b988811e_Gemini_Generated_Image_hi4wiahi4wiahi4w.png' WHERE TenSanPham LIKE N'%iPhone 15%'");
        // Debug Customer and Orders
        var customer = context.KhachHangs.FirstOrDefault(k => k.Email == "haodvttb01628@gmail.com" || k.SoDienThoai == "0965419137");
        if (customer != null)
        {
            Console.WriteLine($"DB DEBUG -> Customer Found! ID: {customer.MaKhachHang}, Name: {customer.HoTen}, Phone: {customer.SoDienThoai}, Email: {customer.Email}");
        }
        else
        {
            Console.WriteLine("DB DEBUG -> Customer haodvttb01628@gmail.com OR 0965419137 NOT FOUND in dbo.KhachHang!");
        }

        var order63 = context.DonHangs.Include(d => d.KhachHang).FirstOrDefault(d => d.MaDonHang == 63);
        if (order63 != null)
        {
            Console.WriteLine($"DB DEBUG -> Order #63 Found! MaDonHang: {order63.MaDonHang}, CustID: {order63.MaKhachHang}, CustName: {order63.KhachHang?.HoTen}, CustPhone: {order63.KhachHang?.SoDienThoai}");
        }
        else
        {
            Console.WriteLine("DB DEBUG -> Order #63 NOT FOUND in dbo.DonHang!");
        }

        var phoneOrders = context.DonHangs.Include(d => d.KhachHang).Where(d => d.KhachHang.SoDienThoai == "0965419137").ToList();
        Console.WriteLine($"DB DEBUG -> Total Orders for Phone 0965419137: {phoneOrders.Count}");
        foreach(var o in phoneOrders)
        {
            Console.WriteLine($"DB DEBUG -> Phone Order ID: {o.MaDonHang}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error seeding iPhone image: " + ex.Message);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Online}/{action=Index}/{id?}");

app.Run();

