using Elysia.Data;
using Elysia.Models;
using Microsoft.AspNetCore.Authorization; // Bắt buộc có
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Elysia.Controllers
{
    // --- QUAN TRỌNG: Dòng này biến Controller này thành khu vực riêng cho Admin ---
    [Authorize(Roles = "Admin")]
    // ------------------------------------------------------------------------------
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
            // Thống kê cơ bản
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalCourses = _context.Courses.Count();
            return View();
        }

        // GET: /Admin/ApproveInstructors
        public async Task<IActionResult> ApproveInstructors()
        {
            // Lấy danh sách User là Giảng viên (Role) NHƯNG chưa xác thực email (EmailConfirmed = false)
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
                // Phê duyệt bằng cách set EmailConfirmed = true
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction(nameof(ApproveInstructors));
        }

        // GET: /Admin/ApproveCourses
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

        // POST: /Admin/RejectCourse (Xóa khóa học không đạt)
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
    }
}