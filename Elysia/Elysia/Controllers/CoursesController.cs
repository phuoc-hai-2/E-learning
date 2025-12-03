using Elysia.Data;
using Elysia.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Elysia.Controllers
{
    [Authorize(Roles = "SinhVien")]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public CoursesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Dashboard Sinh Viên
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var myEnrollments = await _context.Enrollments
                .Where(e => e.UserId == userId)
                .Include(e => e.Course).ThenInclude(c => c.User)
                .Include(e => e.Payments)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync();

            // Hiển thị cả khóa học Pending để sinh viên biết và thanh toán tiếp
            return View(myEnrollments);
        }

        // 2. Tìm kiếm khóa học
        public async Task<IActionResult> Search(string searchQuery)
        {
            var userId = GetCurrentUserId();

            // Chỉ lọc những khóa đã thanh toán thành công (Success) hoặc Miễn phí
            var activeCourseIds = await _context.Enrollments
                .Where(e => e.UserId == userId &&
                           (e.Course.Price == 0 || e.Payments.Any(p => p.Status == "00" || p.Status == "Success")))
                .Select(e => e.CourseID)
                .ToListAsync();

            var courses = _context.Courses
                .Where(c => c.IsApproved && !activeCourseIds.Contains(c.CourseID));

            if (!string.IsNullOrEmpty(searchQuery))
            {
                courses = courses.Where(c => c.Title.Contains(searchQuery));
            }

            return View(await courses.Include(c => c.User).ToListAsync());
        }

        // 3. Chi tiết khóa học
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.User)
                .Include(c => c.Lectures)
                .Include(c => c.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.IsApproved);

            if (course == null) return NotFound();
            return View(course);
        }

        // 4. Xử lý Đăng ký & Thanh toán
        public async Task<IActionResult> Enroll(int id)
        {
            var userId = GetCurrentUserId();
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var existingEnrollment = await _context.Enrollments
                .Include(e => e.Payments)
                .FirstOrDefaultAsync(e => e.CourseID == id && e.UserId == userId);

            if (existingEnrollment != null)
            {
                if (course.Price > 0)
                {
                    // Kiểm tra xem đã thanh toán thành công chưa (Status == "Success" hoặc "00")
                    bool hasPaid = existingEnrollment.Payments.Any(p => p.Status == "00" || p.Status == "Success");

                    if (hasPaid)
                    {
                        return RedirectToAction(nameof(Watch), new { id = id });
                    }
                    else
                    {
                        // Tìm giao dịch Pending cũ để thanh toán tiếp
                        var pendingPayment = existingEnrollment.Payments
                            .OrderByDescending(p => p.PaymentDate)
                            .FirstOrDefault(p => p.Status == "Pending");

                        if (pendingPayment != null)
                        {
                            return RedirectToAction("CreatePayment", "Payment", new { paymentId = pendingPayment.PaymentID });
                        }

                        // Nếu lỗi, xóa enrollment cũ làm lại
                        _context.Enrollments.Remove(existingEnrollment);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Watch), new { id = id });
                }
            }

            // Tạo mới Enrollment
            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseID = id,
                EnrollmentDate = DateTime.Now,
                ProgressPercent = 0
            };

            if (course.Price > 0)
            {
                var payment = new Payment
                {
                    Enrollment = enrollment,
                    Amount = course.Price,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "VnPay",
                    Status = "Pending"
                };

                _context.Payments.Add(payment);
                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                // Gửi Email xác nhận
                var user = await _userManager.FindByIdAsync(userId);
                await _emailSender.SendEmailAsync(user.Email, "Xác nhận đăng ký",
                    $"Bạn đã đăng ký <strong>{course.Title}</strong>. Vui lòng thanh toán để vào học.");

                return RedirectToAction("CreatePayment", "Payment", new { paymentId = payment.PaymentID });
            }
            else
            {
                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Watch), new { id = id });
            }
        }

        // 5. Màn hình học (Watch) - CHẶN HỌC CHÙA
        public async Task<IActionResult> Watch(int id)
        {
            var userId = GetCurrentUserId();

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Payments)
                .FirstOrDefaultAsync(e => e.CourseID == id && e.UserId == userId);

            bool canAccess = false;

            if (enrollment != null)
            {
                if (enrollment.Course.Price == 0)
                {
                    canAccess = true;
                }
                else
                {
                    // BẮT BUỘC: Phải có Status là Success hoặc 00 mới được xem
                    canAccess = enrollment.Payments.Any(p => p.Status == "00" || p.Status == "Success");
                }
            }

            if (!canAccess)
            {
                TempData["ErrorMessage"] = "Bạn chưa hoàn tất thanh toán hoặc chưa đăng ký khóa học này.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var courseContent = await _context.Courses
                .Include(c => c.Lectures)
                .ThenInclude(l => l.Discussions).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            return View(courseContent);
        }

        // 6. API: Đánh dấu hoàn thành bài học
        [HttpPost]
        public async Task<IActionResult> CompleteLecture(int lectureId, int courseId)
        {
            var userId = GetCurrentUserId();

            if (!await _context.LectureCompletions.AnyAsync(lc => lc.LectureID == lectureId && lc.UserId == userId))
            {
                _context.LectureCompletions.Add(new LectureCompletion
                {
                    UserId = userId,
                    LectureID = lectureId,
                    CompletionDate = DateTime.Now
                });
                await _context.SaveChangesAsync();

                var total = await _context.Lectures.CountAsync(l => l.CourseID == courseId);
                var completed = await _context.LectureCompletions
                    .CountAsync(lc => lc.UserId == userId && _context.Lectures.Any(l => l.LectureID == lc.LectureID && l.CourseID == courseId));

                var enrollment = await _context.Enrollments
                    .Include(e => e.Course) // Include Course để lấy UserId Giảng viên
                    .FirstOrDefaultAsync(e => e.CourseID == courseId && e.UserId == userId);

                if (enrollment != null && total > 0)
                {
                    enrollment.ProgressPercent = Math.Round(((decimal)completed / total) * 100, 2);
                    await _context.SaveChangesAsync();

                    if (enrollment.ProgressPercent >= 100)
                    {
                        var user = await _userManager.FindByIdAsync(userId);

                        // Thông báo cho Sinh viên
                        _context.Notifications.Add(new Notification
                        {
                            UserId = userId,
                            Message = $"Chúc mừng! Bạn đã hoàn thành khóa học '{enrollment.Course.Title}'.",
                            Url = $"/Courses/Details/{courseId}"
                        });

                        // === 🔔 GỬI THÔNG BÁO: Sinh viên học xong -> Báo Giảng viên ===
                        _context.Notifications.Add(new Notification
                        {
                            UserId = enrollment.Course.UserId, // ID Giảng viên
                            Message = $"Tuyệt vời! Sinh viên {user.FullName ?? user.UserName} vừa hoàn thành 100% khóa học '{enrollment.Course.Title}' của bạn.",
                            Url = $"/Instructor/ManageCourse/{courseId}",
                            CreatedAt = DateTime.Now
                        });
                        // ==============================================================

                        await _context.SaveChangesAsync();

                        await _emailSender.SendEmailAsync(user.Email, "Chúc mừng hoàn thành khóa học",
                            $"Tuyệt vời! Bạn đã hoàn thành xuất sắc khóa học <strong>{enrollment.Course.Title}</strong>.");
                    }

                    return Json(new { success = true, progress = enrollment.ProgressPercent });
                }
            }
            return Json(new { success = true });
        }

        // 7. Gửi thảo luận
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiscussionComment(int lectureId, string commentText)
        {
            var lecture = await _context.Lectures
                .Include(l => l.Course) // Include Course để lấy ID giảng viên
                .FirstOrDefaultAsync(l => l.LectureID == lectureId);

            if (lecture != null && !string.IsNullOrWhiteSpace(commentText))
            {
                var userId = GetCurrentUserId();
                var user = await _userManager.FindByIdAsync(userId);

                // Lưu bình luận
                _context.Discussions.Add(new Discussion
                {
                    LectureID = lectureId,
                    UserId = userId,
                    Content = commentText,
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                // === 🔔 GỬI THÔNG BÁO: Sinh viên hỏi -> Báo Giảng viên ===
                // Tránh trường hợp Giảng viên tự comment rồi tự nhận thông báo
                if (lecture.Course.UserId != userId)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = lecture.Course.UserId,
                        Message = $"{user.FullName ?? user.UserName} đã hỏi trong bài '{lecture.Title}': \"{commentText}\"",
                        Url = $"/Courses/Watch?id={lecture.CourseID}",
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }
                // =========================================================

                return RedirectToAction(nameof(Watch), new { id = lecture.CourseID });
            }
            return BadRequest();
        }

        // 8. Gửi đánh giá (Review)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int courseId, int rating, string comment)
        {
            var userId = GetCurrentUserId();
            if (await _context.Enrollments.AnyAsync(e => e.CourseID == courseId && e.UserId == userId))
            {
                _context.Reviews.Add(new Review
                {
                    CourseID = courseId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = courseId });
        }

        // 9. LÀM QUIZ
        public async Task<IActionResult> DoQuiz(int lectureId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions).ThenInclude(qn => qn.Answers)
                .Include(q => q.Lecture)
                .FirstOrDefaultAsync(q => q.LectureID == lectureId);

            if (quiz == null) return NotFound("Chưa có bài tập cho bài giảng này.");
            return View(quiz);
        }

        // 10. NỘP QUIZ & TÍNH ĐIỂM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz(int quizId, Dictionary<int, int> answers)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions).ThenInclude(qn => qn.Answers)
                .Include(q => q.Lecture)
                .FirstOrDefaultAsync(q => q.QuizID == quizId);

            if (quiz == null) return NotFound();

            int correctCount = 0;
            int totalQuestions = quiz.Questions.Count;

            foreach (var question in quiz.Questions)
            {
                if (answers.ContainsKey(question.QuestionID))
                {
                    int selectedAnswerId = answers[question.QuestionID];
                    bool isCorrect = question.Answers.Any(a => a.AnswerID == selectedAnswerId && a.IsCorrect);
                    if (isCorrect) correctCount++;
                }
            }

            double score = totalQuestions == 0 ? 0 : ((double)correctCount / totalQuestions) * 100;

            return View("QuizResult", new
            {
                Score = (int)score,
                Correct = correctCount,
                Total = totalQuestions,
                QuizId = quizId,
                CourseId = quiz.Lecture?.CourseID
            });
        }
    }
}