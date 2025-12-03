using Elysia.Data;
using Elysia.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Elysia.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Index
        public IActionResult Index()
        {
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalCourses = _context.Courses.Count();
            return View();
        }

        // GET: /Admin/ApproveInstructors
        public async Task<IActionResult> ApproveInstructors()
        {
            var pendingInstructors = await _context.Users
                .Where(u => !u.EmailConfirmed && _context.UserRoles
                    .Any(ur => ur.UserId == u.Id && ur.RoleId == _context.Roles.FirstOrDefault(r => r.Name == "GiangVien").Id))
                .ToListAsync();
            return View(pendingInstructors);
        }

        // POST: /Admin/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction(nameof(ApproveInstructors));
        }

        // GET: /Admin/ApproveCourses
        // (Trang này sẽ trống nếu tất cả khóa học đều Auto-Approve, nhưng giữ lại để phòng hờ)
        public async Task<IActionResult> ApproveCourses()
        {
            var pendingCourses = await _context.Courses
                .Where(c => !c.IsApproved)
                .Include(c => c.User)
                .ToListAsync();
            return View(pendingCourses);
        }

        // POST: /Admin/ApproveCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCourse(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course != null)
            {
                course.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ApproveCourses));
        }

        // POST: /Admin/RejectCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCourse(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ApproveCourses));
        }

        // GET: /Admin/ManageUsers
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        // =======================================================================
        // --- PHẦN MỚI THÊM: QUẢN LÝ TOÀN BỘ KHÓA HỌC (VÌ KHÔNG CÒN CHỜ DUYỆT) ---
        // =======================================================================

        // GET: /Admin/ManageCourses
        // Trang liệt kê TẤT CẢ khóa học đang có trên hệ thống
        public async Task<IActionResult> ManageCourses()
        {
            var allCourses = await _context.Courses
                .Include(c => c.User) // Lấy tên giảng viên
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(allCourses);
        }

        // POST: /Admin/DeleteCourse
        // Admin dùng cái này để gỡ bỏ khóa học vi phạm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course != null)
            {
                // Xóa khóa học
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
            // Quay lại trang quản lý toàn bộ
            return RedirectToAction(nameof(ManageCourses));
        }
        // POST: /Admin/LockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Khóa tài khoản vĩnh viễn (hoặc 100 năm)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                await _userManager.SetLockoutEnabledAsync(user, true); // Đảm bảo tính năng lock được bật
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        // POST: /Admin/UnlockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Mở khóa ngay lập tức
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            return RedirectToAction(nameof(ManageUsers));
        }
    }
}