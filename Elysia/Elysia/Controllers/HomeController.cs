using Elysia.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Elysia.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // LOGIC BỔ SUNG: Nếu người dùng đã đăng nhập rồi, đẩy họ về đúng trang Dashboard luôn
            // thay vì bắt họ xem lại trang giới thiệu.
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
                if (User.IsInRole("GiangVien")) return RedirectToAction("Index", "Instructor");
                if (User.IsInRole("SinhVien")) return RedirectToAction("Index", "Courses");
            }

            // Nếu là khách (chưa đăng nhập), trả về View giới thiệu bình thường
            return View();
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