using Elysia.Data; // using thư mục Data
using Elysia.Models; // using thư mục Models
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Lấy chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Đăng ký DbContext (ApplicationDbContext) với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Đăng ký Identity (QUAN TRỌNG)
// Chúng ta dùng AddIdentity (thay vì AddDefaultIdentity)
// để chỉ định rõ lớp ApplicationUser và IdentityRole
// File: Program.cs

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// 4. Đăng ký dịch vụ Controllers và Views (cho MVC)
builder.Services.AddControllersWithViews();

// 5. Đăng ký Razor Pages (cho các trang Identity sau này)
builder.Services.AddRazorPages();


// ==================================================================
var app = builder.Build();
// ==================================================================


// 6. Cấu hình HTTP request pipeline (thứ tự quan trọng)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho phép dùng file CSS, JS, hình ảnh

app.UseRouting();

// 7. Bật Xác thực (Authentication) VÀ Phân quyền (Authorization)
// (QUAN TRỌNG) Phải có UseAuthentication() TRƯỚC UseAuthorization()
app.UseAuthentication();
app.UseAuthorization();

try
{
    // Tạo một "scope" để lấy dịch vụ
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        // Gọi hàm SeedRolesAndAdminAsync
        // Dùng .Wait() vì chúng ta đang ở trong hàm Main (đồng bộ)
        Elysia.Data.DbSeeder.SeedRolesAndAdminAsync(services).Wait();
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Một lỗi đã xảy ra khi seed CSDL.");
}

// 8. Map các Route
// Route mặc định cho MVC (Controllers)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map các trang Razor Pages (cho Identity UI)
app.MapRazorPages();


app.Run();