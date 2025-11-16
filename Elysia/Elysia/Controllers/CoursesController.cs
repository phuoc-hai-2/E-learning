using Elysia.Data;
using Elysia.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // <-- LỖI 1: Đã sửa (Microsoft_FrameworkCore -> Microsoft.EntityFrameworkCore)
using System.Security.Claims;
using System.Linq; // Cần cho .Where(), .Select()

namespace Elysia.Controllers
{
    // === KHÓA TOÀN BỘ CONTROLLER CHỈ CHO SINH VIÊN ===
    [Authorize(Roles = "SinhVien")]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoursesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // ======================================================
        // CHỨC NĂNG TỪ STUDENTCONTROLLER (CŨ)
        // ======================================================

        // GET: /Courses/Index
        // Dashboard Sinh viên (Các khóa học CỦA TÔI)
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            var myEnrollments = await _context.Enrollments
                .Where(e => e.UserId == userId)
                .Include(e => e.Course)
                .ThenInclude(c => c.User)
                .ToListAsync();

            return View(myEnrollments);
        }

        // GET: /Courses/MyProgress
        // Trang xem Tiến độ chi tiết
        public async Task<IActionResult> MyProgress()
        {
            var userId = GetCurrentUserId();
            // TODO: Lấy các Enrollment và tính toán tiến độ

            return View();
        }

        // ======================================================
        // CHỨC NĂNG TỪ COURSESCONTROLLER (CŨ)
        // ======================================================

        // GET: /Courses/Search
        // Trang TÌM KIẾM & XEM DANH SÁCH (Để đăng ký khóa mới)
        public async Task<IActionResult> Search(string searchQuery)
        {
            var userId = GetCurrentUserId();

            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.UserId == userId)
                .Select(e => e.CourseID)
                .ToListAsync();

            // --- LỖI 2: ĐÃ SỬA LOGIC TRUY VẤN ---
            // 1. Bắt đầu truy vấn (IQueryable)
            IQueryable<Course> availableCoursesQuery = _context.Courses
                .Where(c => c.IsApproved && !enrolledCourseIds.Contains(c.CourseID));

            // 2. Thêm điều kiện tìm kiếm nếu có
            if (!String.IsNullOrEmpty(searchQuery))
            {
                availableCoursesQuery = availableCoursesQuery.Where(c => c.Title.Contains(searchQuery));
            }

            // 3. Include Giảng viên và thực thi
            var availableCourses = await availableCoursesQuery
                                        .Include(c => c.User)
                                        .ToListAsync();
            // --- KẾT THÚC SỬA LỖI 2 ---

            return View(availableCourses);
        }

        // GET: /Courses/Details/5
        // Trang XEM CHI TIẾT khóa học
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.User)
                .Include(c => c.Lectures)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.IsApproved);

            if (course == null) return NotFound();

            return View(course);
        }

        // GET: /Courses/Enroll/5
        // Trang xác nhận ĐĂNG KÝ HỌC (miễn phí hoặc trả phí)
        public async Task<IActionResult> Enroll(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var userId = GetCurrentUserId();

            bool isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseID == id && e.UserId == userId);
            if (isEnrolled)
            {
                return RedirectToAction(nameof(Watch), new { id = id });
            }

            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseID = id,
                EnrollmentDate = DateTime.Now,
                ProgressPercent = 0
            };

            if (course.Price > 0)
            {
                // TODO: Chuyển sang trang Thanh toán (hoặc giả lập)
                var payment = new Payment
                {
                    Enrollment = enrollment,
                    Amount = course.Price,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "Demo",
                    Status = "Completed"
                };
                _context.Payments.Add(payment);

                // TODO: Gửi HÓA ĐƠN (Email)
            }

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Watch), new { id = id });
        }

        // GET: /Courses/Watch/5
        // Trang XEM BÀI GIẢNG (Phòng học)
        public async Task<IActionResult> Watch(int id) // id là CourseID
        {
            var userId = GetCurrentUserId();
            bool isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseID == id && e.UserId == userId);
            if (!isEnrolled)
            {
                return Forbid();
            }

            // TODO: Lấy thông tin khóa học (include Bài giảng, Quiz)
            var course = await _context.Courses.Include(c => c.Lectures).FirstOrDefaultAsync(c => c.CourseID == id);
            return View(course);
        }

        // POST: /Courses/CompleteLecture
        // Đánh dấu 1 bài giảng đã HOÀN THÀNH (Theo dõi tiến độ)
        [HttpPost]
        public async Task<IActionResult> CompleteLecture(int lectureId, int courseId)
        {
            var userId = GetCurrentUserId();

            // TODO: Thêm record vào bảng LectureCompletions (UserId, LectureId)
            // Tính toán lại ProgressPercent cho Enrollment

            return Json(new { success = true, progress = 50.5 });
        }

        // GET: /Courses/DoQuiz/5
        // Trang LÀM QUIZ (quizId)
        public async Task<IActionResult> DoQuiz(int quizId)
        {
            // TODO: Lấy Quiz (include Câu hỏi, Câu trả lời)
            return View();
        }

        // POST: /Courses/SubmitQuiz/5
        // Xử lý NỘP BÀI Quiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz(int quizId, IFormCollection form)
        {
            // TODO: Lấy các câu trả lời của user từ 'form'
            // So sánh với đáp án đúng, Tính điểm

            var score = 90; // Giả lập điểm

            // --- LỖI 3: ĐÃ SỬA (thay '...' bằng 'score') ---
            return RedirectToAction("QuizResult", new { quizId = quizId, score = score });
        }

        // GET: /Courses/QuizResult
        // Trang KẾT QUẢ Quiz
        public IActionResult QuizResult(int quizId, int score)
        {
            ViewBag.Score = score;
            return View();
        }

        // POST: /Courses/AddReview
        // Thêm ĐÁNH GIÁ (sao + bình luận)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int courseId, int rating, string comment)
        {
            // TODO: Tạo Review mới, lưu vào CSDL
            return RedirectToAction(nameof(Details), new { id = courseId });
        }

        // POST: /Courses/AddDiscussionComment
        // Thêm bình luận (FORUM)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiscussionComment(int lectureId, string commentText)
        {
            // TODO: Tạo Discussion mới, lưu vào CSDL

            // --- LỖI 3: ĐÃ SỬA (thay '...' bằng logic lấy CourseID) ---
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture == null)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Watch), new { id = lecture.CourseID });
        }
    }
}