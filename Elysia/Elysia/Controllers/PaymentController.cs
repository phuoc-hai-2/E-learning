using Elysia.Data;
using Elysia.Models;
using Elysia.Services.VnPay;
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
        private readonly ILogger<PaymentController> _logger; // Added Logger

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

            // Check if payment is already completed to prevent double payment
            if (payment.Status == "Completed")
            {
                return RedirectToAction("PaymentCallback", new { vnp_ResponseCode = "00", vnp_TxnRef = payment.PaymentID.ToString() });
            }

            var vnPay = new VnPayLibrary();
            var timeNow = DateTime.Now;
            var tick = DateTime.Now.Ticks.ToString();

            // Get Config
            string vnp_TmnCode = _configuration["VnPay:TmnCode"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];
            string vnp_Url = _configuration["VnPay:BaseUrl"];
            string vnp_Returnurl = Url.Action("PaymentCallback", "Payment", null, Request.Scheme);

            // Build VNPAY Request
            vnPay.AddRequestData("vnp_Version", VnPayLibrary.VERSION); // Assuming VERSION const exists or use "2.1.0"
            vnPay.AddRequestData("vnp_Command", "pay");
            vnPay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnPay.AddRequestData("vnp_Amount", ((long)(payment.Amount * 100)).ToString()); // Use long to avoid overflow
            vnPay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            vnPay.AddRequestData("vnp_CurrCode", "VND");
            vnPay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(HttpContext) ?? "127.0.0.1"); // Use helper or fallback
            vnPay.AddRequestData("vnp_Locale", "vn");

            // Order Info - Ensure no illegal characters if necessary
            vnPay.AddRequestData("vnp_OrderInfo", $"Thanh toan khoa hoc {payment.Enrollment.Course.Title}");
            vnPay.AddRequestData("vnp_OrderType", "other"); // electronic device/other

            vnPay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnPay.AddRequestData("vnp_TxnRef", payment.PaymentID.ToString()); // Transaction Reference (Unique per day ideally)

            // Create Payment URL with Signature
            string paymentUrl = vnPay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            _logger.LogInformation($"VNPAY URL Generated: {paymentUrl}");

            return Redirect(paymentUrl);
        }

        // 2. Process Return Data from VNPAY
        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            var response = Request.Query;
            if (response.Count == 0)
            {
                return Content("No data received from VNPAY.");
            }

            var vnPay = new VnPayLibrary();
            foreach (var s in response)
            {
                // Populate response data into library for signature validation
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
                return View("PaymentFail", new { Message = "Chữ ký không hợp lệ. Giao dịch có thể đã bị can thiệp." });
            }

            // 2. Get Data
            string vnp_ResponseCode = vnPay.GetResponseData("vnp_ResponseCode");
            string vnp_TxnRef = vnPay.GetResponseData("vnp_TxnRef");
            string vnp_TransactionNo = vnPay.GetResponseData("vnp_TransactionNo");
            string vnp_OrderInfo = vnPay.GetResponseData("vnp_OrderInfo");

            // 3. Find Payment in DB
            if (!int.TryParse(vnp_TxnRef, out int paymentId))
            {
                _logger.LogError($"Invalid vnp_TxnRef: {vnp_TxnRef}");
                return View("PaymentFail", new { Message = "Mã giao dịch không hợp lệ." });
            }

            var payment = await _context.Payments
                .Include(p => p.Enrollment).ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

            if (payment == null)
            {
                _logger.LogError($"Payment not found for ID: {paymentId}");
                return NotFound();
            }

            // 4. Check Payment Status
            // Only update if status is not already 'Completed' to avoid re-processing
            if (payment.Status != "Completed")
            {
                if (vnp_ResponseCode == "00") // 00 = Success
                {
                    payment.Status = "Completed";
                    payment.PaymentMethod = "VnPay";
                    payment.TransactionId = vnp_TransactionNo;

                    // Unlock logic: If you had logic to hide content until paid, it's handled here by status

                    // Update Context
                    _context.Payments.Update(payment);
                    await _context.SaveChangesAsync();

                    // Send Notifications
                    var user = await _userManager.FindByIdAsync(payment.Enrollment.UserId);
                    await SendPaymentSuccessNotifications(user, payment.Enrollment.Course);

                    _logger.LogInformation($"Payment {paymentId} success. Transaction: {vnp_TransactionNo}");
                }
                else
                {
                    // Payment Failed or Cancelled
                    payment.Status = "Failed";
                    _context.Payments.Update(payment);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning($"Payment {paymentId} failed. Code: {vnp_ResponseCode}");
                }
            }

            // Return View (Confirmation)
            return View("Confirmation", payment);
        }

        private async Task SendPaymentSuccessNotifications(ApplicationUser user, Course course)
        {
            // 1. Web Notification
            var noti = new Notification
            {
                UserId = user.Id,
                Message = $"Thanh toán thành công khóa học: {course.Title}",
                Url = $"/Courses/Watch?id={course.CourseID}",
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(noti);
            await _context.SaveChangesAsync();

            // 2. Email Notification
            if (!string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    await _emailSender.SendEmailAsync(user.Email, "Thanh toán thành công - Elysia",
                        $@"
                        <h3>Cảm ơn {user.FullName},</h3>
                        <p>Bạn đã thanh toán thành công khóa học <strong>{course.Title}</strong>.</p>
                        <p>Mã khóa học: {course.CourseID}</p>
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

    // Helper class to get IP Address (Optional, can put in Utils folder)
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