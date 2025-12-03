using Elysia.Data;
using Elysia.Models;
using Elysia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Elysia.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _configuration = configuration;
            _emailSender = emailSender;
            _userManager = userManager;
            _logger = logger;
        }

        // 1. Create Payment URL and Redirect to VNPAY
        public IActionResult CreatePayment(int paymentId)
        {
            var payment = _context.Payments
                .Include(p => p.Enrollment).ThenInclude(e => e.Course)
                .FirstOrDefault(p => p.PaymentID == paymentId);

            if (payment == null) return NotFound();

            // Nếu đã thanh toán rồi thì không cho thanh toán lại
            if (payment.Status == "Success" || payment.Status == "Completed")
            {
                return RedirectToAction("PaymentCallback", new { vnp_ResponseCode = "00", vnp_TxnRef = payment.PaymentID.ToString() });
            }

            var vnPay = new VnPayLibrary();
            var timeNow = DateTime.Now;

            // Get Config
            string vnp_TmnCode = _configuration["VnPay:TmnCode"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];
            string vnp_Url = _configuration["VnPay:BaseUrl"];
            string vnp_Returnurl = Url.Action("PaymentCallback", "Payment", null, Request.Scheme);

            // Build VNPAY Request
            vnPay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnPay.AddRequestData("vnp_Command", "pay");
            vnPay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnPay.AddRequestData("vnp_Amount", ((long)(payment.Amount * 100)).ToString());
            vnPay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            vnPay.AddRequestData("vnp_CurrCode", "VND");
            vnPay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(HttpContext) ?? "127.0.0.1");
            vnPay.AddRequestData("vnp_Locale", "vn");
            vnPay.AddRequestData("vnp_OrderInfo", $"Thanh toan khoa hoc {payment.Enrollment.Course.Title}");
            vnPay.AddRequestData("vnp_OrderType", "other");
            vnPay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnPay.AddRequestData("vnp_TxnRef", payment.PaymentID.ToString());

            string paymentUrl = vnPay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            _logger.LogInformation($"VNPAY URL Generated: {paymentUrl}");

            return Redirect(paymentUrl);
        }

        // 2. Process Return Data from VNPAY
        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            var response = Request.Query;
            if (response.Count == 0) return Content("No data received from VNPAY.");

            var vnPay = new VnPayLibrary();
            foreach (var s in response)
            {
                if (!string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("vnp_"))
                {
                    vnPay.AddResponseData(s.Key, s.Value);
                }
            }

            // 1. Validate Signature
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];
            string inputHash = response["vnp_SecureHash"];
            bool checkSignature = vnPay.ValidateSignature(inputHash, vnp_HashSecret);

            if (!checkSignature)
            {
                _logger.LogWarning("Invalid Signature from VNPAY response.");
                return View("PaymentFail", new { Message = "Chữ ký không hợp lệ." });
            }

            // 2. Get Data
            string vnp_ResponseCode = vnPay.GetResponseData("vnp_ResponseCode");
            string vnp_TxnRef = vnPay.GetResponseData("vnp_TxnRef");
            string vnp_TransactionNo = vnPay.GetResponseData("vnp_TransactionNo");

            // 3. Find Payment in DB
            if (!int.TryParse(vnp_TxnRef, out int paymentId)) return View("PaymentFail");

            var payment = await _context.Payments
                .Include(p => p.Enrollment).ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

            if (payment == null) return NotFound();

            // 4. Update Status (Chỉ cập nhật nếu chưa Success)
            if (payment.Status != "Success")
            {
                if (vnp_ResponseCode == "00") // 00 = Success
                {
                    // QUAN TRỌNG: Đổi trạng thái thành "Success" để khớp với logic CoursesController
                    payment.Status = "Success";
                    payment.PaymentMethod = "VnPay";
                    payment.TransactionId = vnp_TransactionNo;

                    _context.Payments.Update(payment);
                    await _context.SaveChangesAsync();

                    // Send Notifications
                    var user = await _userManager.FindByIdAsync(payment.Enrollment.UserId);

                    // 🔔 Báo cho Sinh viên + Giảng viên
                    await SendPaymentSuccessNotifications(user, payment.Enrollment.Course);

                    _logger.LogInformation($"Payment {paymentId} success. Transaction: {vnp_TransactionNo}");
                }
                else
                {
                    payment.Status = "Failed";
                    _context.Payments.Update(payment);
                    await _context.SaveChangesAsync();
                    _logger.LogWarning($"Payment {paymentId} failed. Code: {vnp_ResponseCode}");
                }
            }

            return View("Confirmation", payment);
        }

        // Helper: Gửi thông báo khi thanh toán thành công
        private async Task SendPaymentSuccessNotifications(ApplicationUser student, Course course)
        {
            // 1. Web Notification -> Sinh viên
            _context.Notifications.Add(new Notification
            {
                UserId = student.Id,
                Message = $"Thanh toán thành công khóa học: {course.Title}",
                Url = $"/Courses/Watch?id={course.CourseID}",
                CreatedAt = DateTime.Now,
                IsRead = false
            });

            // === 🔔 2. Web Notification -> Giảng viên (Ting ting!) ===
            _context.Notifications.Add(new Notification
            {
                UserId = course.UserId, // ID Giảng viên
                Message = $"Ting ting! Sinh viên {student.FullName ?? student.UserName} vừa mua khóa học '{course.Title}'.",
                Url = "/Instructor/Index", // Link tới Dashboard doanh thu
                CreatedAt = DateTime.Now,
                IsRead = false
            });
            // ========================================================

            await _context.SaveChangesAsync();

            // 3. Email Notification -> Sinh viên
            if (!string.IsNullOrEmpty(student.Email))
            {
                try
                {
                    await _emailSender.SendEmailAsync(student.Email, "Thanh toán thành công - Elysia",
                        $@"
                        <h3>Cảm ơn {student.FullName},</h3>
                        <p>Bạn đã thanh toán thành công khóa học <strong>{course.Title}</strong>.</p>
                        <p>Chúc bạn học tập tốt!</p>
                        <p><a href='https://elysia-elearning.com/Courses/Watch?id={course.CourseID}'>Bắt đầu học ngay</a></p>
                        ");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send payment success email.");
                }
            }
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

    public static class Utils
    {
        public static string GetIpAddress(HttpContext context)
        {
            var ipAddress = string.Empty;
            try
            {
                var remoteIpAddress = context.Connection.RemoteIpAddress;
                if (remoteIpAddress != null)
                {
                    if (remoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        remoteIpAddress = System.Net.Dns.GetHostEntry(remoteIpAddress).AddressList
                            .FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    }
                    if (remoteIpAddress != null) ipAddress = remoteIpAddress.ToString();
                    return ipAddress;
                }
            }
            catch (Exception ex)
            {
                return "Invalid IP:" + ex.Message;
            }
            return "127.0.0.1";
        }
    }
}