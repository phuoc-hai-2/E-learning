using Elysia.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Elysia.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // API lấy 5 thông báo mới nhất
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new {
                    n.NotificationID,
                    n.Message,
                    n.Url,
                    n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("dd/MM HH:mm")
                })
                .ToListAsync();

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new { notifications, unreadCount });
        }

        // API đánh dấu đã đọc khi click vào
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var noti = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == id && n.UserId == userId);

            if (noti != null)
            {
                noti.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}