using Elysia.Models;
using Microsoft.AspNetCore.Identity;

namespace Elysia.Data
{
    public static class DbSeeder
    {
        // Hàm này sẽ được gọi từ Program.cs
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            // Lấy các dịch vụ cần thiết
            var userManager = service.GetService<UserManager<ApplicationUser>>();
            var roleManager = service.GetService<RoleManager<IdentityRole>>();

            // --- 1. TẠO CÁC VAI TRÒ (ROLES) ---
            string[] roleNames = { "Admin", "GiangVien", "SinhVien" };

            foreach (var roleName in roleNames)
            {
                // Kiểm tra xem Role đã tồn tại chưa
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    // Nếu chưa, tạo Role mới
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // --- 2. TẠO TÀI KHOẢN ADMIN MẶC ĐỊNH ---

            // Email của Admin
            string adminEmail = "admin@elysia.com";

            // Kiểm tra xem user Admin đã tồn tại chưa
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // Nếu Admin chưa tồn tại, tạo mới
                var newAdminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản Trị Viên",
                    EmailConfirmed = true // Xác thực email luôn
                };

                // Tạo user với mật khẩu
                // (Hãy đổi "Admin@123" thành một mật khẩu an toàn)
                var result = await userManager.CreateAsync(newAdminUser, "Admin@123");

                if (result.Succeeded)
                {
                    // Gán vai trò "Admin" cho user vừa tạo
                    await userManager.AddToRoleAsync(newAdminUser, "Admin");
                }
            }
        }
    }
}