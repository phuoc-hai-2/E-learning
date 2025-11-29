using Elysia.Data;
using Elysia.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Elysia.Controllers
{
    [Authorize(Roles = "SinhVien")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var payments = await _context.Payments
                .Include(p => p.Enrollment).ThenInclude(e => e.Course)
                .Where(p => p.Enrollment.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }
    }
}