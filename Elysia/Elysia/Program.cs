using Elysia.Data;
using Elysia.Models;
using Elysia.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features; // Thêm thư viện này để cấu hình Form upload

var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// 0. Cấu hình giới hạn Upload (Quan trọng cho web E-learning)
// ==================================================================

// Cấu hình cho Kestrel server (chạy khi debug hoặc chạy độc lập)
builder.WebHost.ConfigureKestrel(options =>
{
    // Giới hạn khoảng 3GB (khớp với web.config)
    options.Limits.MaxRequestBodySize = 3221225472;
});

// Cấu hình cho Form upload (Multipart body)
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 3221225472; // 3GB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// ==================================================================
// 1. Cấu hình Database (SQL Server)
// ==================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Cấu hình Identity (Xác thực & Phân quyền)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    // Cấu hình đăng nhập: Yêu cầu xác nhận email
    options.SignIn.RequireConfirmedAccount = true;

    // Cấu hình mật khẩu (đơn giản hóa cho đồ án)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// 3. Đăng ký Email Service (MailKit)
builder.Services.AddTransient<IEmailSender, EmailSender>();

// 4. Đăng ký MVC Controllers và Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ==================================================================
var app = builder.Build();
// ==================================================================

// 5. Cấu hình HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 6. Kích hoạt Xác thực và Phân quyền
app.UseAuthentication();
app.UseAuthorization();

// 7. Seed Data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        Elysia.Models.DbSeeder.SeedRolesAndAdminAsync(services).Wait();
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Một lỗi đã xảy ra khi khởi tạo dữ liệu mẫu (Seeding DB).");
}

// 8. Định tuyến
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();