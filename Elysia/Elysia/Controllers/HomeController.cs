using Elysia.Data; // using DbContext
using Elysia.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Cần cho .Include()
using System.Diagnostics;

namespace Elysia.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Inject DbContext

        // Yêu cầu (Inject) DbContext và Logger qua constructor
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Trang chủ sẽ hiển thị danh sách khóa học
        public async Task<IActionResult> Index()
        {
            // Truy vấn CSDL:
            // Lấy các khóa học ĐÃ ĐƯỢC DUYỆT (IsApproved == true)
            // Dùng .Include() để load tên Giảng viên (từ bảng User)
            var courses = await _context.Courses
                                .Where(c => c.IsApproved == true)
                                .Include(c => c.User) // "User" là thuộc tính navigation trong model Course
                                .ToListAsync();

            // Gửi danh sách khóa học này tới View
            return View(courses);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}