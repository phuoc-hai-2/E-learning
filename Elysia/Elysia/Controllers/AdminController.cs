using Elysia.Data;
using Elysia.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Elysia.Controllers
{
    [Authorize(Roles = "Admin")] // Khóa toàn bộ Controller chỉ cho Admin
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
        // Dashboard Admin
        public IActionResult Index()
        {
            // TODO: Lấy data thống kê (tổng user, tổng khóa học...)
            // Trả về View tại: Views/Admin/Index.cshtml
            return View();
        }

        // GET: /Admin/ApproveInstructors
        // Duyệt Giảng viên
        [HttpGet]
        public async Task<IActionResult> ApproveInstructors()
        {
            // TODO: Code logic lấy Giảng viên chờ duyệt (EmailConfirmed = false)
            // (Đã có ở các prompt trước)

            // Trả về View tại: Views/Admin/ApproveInstructors.cshtml
            var pendingInstructors = await _context.Users.Where(u => !u.EmailConfirmed).ToListAsync(); // Cần logic lọc Role
            return View(pendingInstructors);
        }

        // POST: /Admin/Approve
        // Xử lý duyệt Giảng viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string userId)
        {
            // TODO: Code logic set user.EmailConfirmed = true
            // (Đã có ở các prompt trước)
            return RedirectToAction(nameof(ApproveInstructors));
        }

        // GET: /Admin/ApproveCourses
        // Trang duyệt khóa học
        [HttpGet]
        public async Task<IActionResult> ApproveCourses()
        {
            // TODO: Lấy các khóa học có IsApproved == false
            var pendingCourses = await _context.Courses
                .Where(c => !c.IsApproved)
                .Include(c => c.User) // Kèm tên Giảng viên
                .ToListAsync();

            // Trả về View tại: Views/Admin/ApproveCourses.cshtml
            return View(pendingCourses);
        }

        // POST: /Admin/ApproveCourse
        // Xử lý duyệt 1 khóa học
        [HttpGet]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCourse(int courseId)
        {
            // TODO: Tìm khóa học và set course.IsApproved = true
            var course = await _context.Courses.FindAsync(courseId);
            if (course != null)
            {
                course.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ApproveCourses));
        }

        // POST: /Admin/RejectCourse
        // Xử lý HỦY/XÓA 1 khóa học
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCourse(int courseId)
        {
            // TODO: Tìm và XÓA khóa học (cẩn thận)
            var course = await _context.Courses.FindAsync(courseId);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ApproveCourses));
        }

        // GET: /Admin/ManageUsers
        // Quản lý tất cả người dùng
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            // TODO: Lấy danh sách TOÀN BỘ người dùng
            var users = await _context.Users.ToListAsync();

            // Trả về View tại: Views/Admin/ManageUsers.cshtml
            return View(users);
        }

        // POST: /Admin/DeleteUser
        // Xóa 1 người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            // TODO: Tìm và XÓA 1 user
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(ManageUsers));
        }
    }
}