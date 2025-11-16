using Elysia.Data;
using Elysia.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // <-- Cần cho .Include()
using System.Security.Claims; // Để lấy User ID
using System.IO; // Cần cho xử lý File Upload

namespace Elysia.Controllers
{
    [Authorize(Roles = "GiangVien")] // Khóa toàn bộ Controller chỉ cho Giảng viên
    public class InstructorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment; // Dùng để Upload file

        public InstructorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // GET: /Instructor/Index
        // Dashboard Giảng viên
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            // Lấy thống kê của Giảng viên
            ViewBag.TotalCourses = await _context.Courses.CountAsync(c => c.UserId == userId);
            // TODO: Thống kê tổng số học viên (phức tạp hơn)

            return View();
        }

        // GET: /Instructor/MyCourses
        // Danh sách khóa học CỦA TÔI
        public async Task<IActionResult> MyCourses()
        {
            var userId = GetCurrentUserId();
            // Lấy các khóa học có UserId == userId, sắp xếp mới nhất lên trên
            var myCourses = await _context.Courses
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(myCourses);
        }

        // GET: /Instructor/Create
        // Trang tạo khóa học MỚI (hiện form)
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Instructor/Create
        // Xử lý tạo khóa học MỚI
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Sửa: Thêm IFormFile imageFile để nhận file upload
        public async Task<IActionResult> Create([Bind("Title,Description,Price")] Course course, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                course.UserId = GetCurrentUserId();
                course.IsApproved = false; // Chờ Admin duyệt
                course.CreatedAt = DateTime.Now;

                // Xử lý upload file "imageFile"
                if (imageFile != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                    string extension = Path.GetExtension(imageFile.FileName);
                    string uniqueFileName = fileName + "_" + Guid.NewGuid().ToString() + extension;
                    string path = Path.Combine(wwwRootPath, "uploads/courses", uniqueFileName);

                    // Tạo thư mục nếu chưa có
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    // Lưu đường dẫn web vào CSDL
                    course.ImageUrl = "/uploads/courses/" + uniqueFileName;
                }

                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyCourses));
            }
            return View(course);
        }

        // GET: /Instructor/ManageCourse/5
        // Trang Quản lý 1 khóa học (Thêm/Sửa/Xóa Bài giảng)
        public async Task<IActionResult> ManageCourse(int id)
        {
            var userId = GetCurrentUserId();
            // Lấy khóa học (phải của Giảng viên này) và danh sách Bài giảng
            var course = await _context.Courses
                .Include(c => c.Lectures.OrderBy(l => l.Order)) // Lấy bài giảng theo thứ tự
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserId == userId);

            if (course == null)
            {
                return NotFound("Không tìm thấy khóa học hoặc bạn không có quyền.");
            }

            return View(course);
        }

        // POST: /Instructor/AddLecture
        // Xử lý Thêm bài giảng MỚI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLecture([Bind("CourseID,Title,Order")] Lecture lecture, IFormFile videoFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload file "videoFile"
                if (videoFile != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + videoFile.FileName;
                    string path = Path.Combine(wwwRootPath, "uploads/videos", uniqueFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await videoFile.CopyToAsync(fileStream);
                    }
                    lecture.VideoUrl = "/uploads/videos/" + uniqueFileName;
                }

                _context.Lectures.Add(lecture);
                await _context.SaveChangesAsync();
            }
            // Quay lại trang quản lý của khóa học đó
            return RedirectToAction(nameof(ManageCourse), new { id = lecture.CourseID });
        }

        // POST: /Instructor/DeleteLecture/5
        // Xử lý Xóa 1 bài giảng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLecture(int lectureId)
        {
            // TODO: Tìm và xóa bài giảng
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture == null)
            {
                return NotFound();
            }

            // <-- ĐÃ SỬA: Lấy CourseID TRƯỚC KHI XÓA
            int courseId = lecture.CourseID;

            // (Tùy chọn nâng cao: Xóa file video vật lý)
            if (!string.IsNullOrEmpty(lecture.VideoUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                var oldPath = Path.Combine(wwwRootPath, lecture.VideoUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            _context.Lectures.Remove(lecture);
            await _context.SaveChangesAsync();

            // <-- ĐÃ SỬA: Dùng CourseID đã lưu để quay lại
            return RedirectToAction(nameof(ManageCourse), new { id = courseId });
        }

        // GET: /Instructor/ManageQuiz/5
        // Trang quản lý Quiz cho 1 Bài giảng (lectureId)
        public async Task<IActionResult> ManageQuiz(int lectureId)
        {
            // Lấy thông tin Bài giảng và Quiz (cùng các Câu hỏi, Câu trả lời)
            var lecture = await _context.Lectures
                .Include(l => l.Quiz)
                    .ThenInclude(q => q.Questions)
                    .ThenInclude(qt => qt.Answers)
                .FirstOrDefaultAsync(l => l.LectureID == lectureId);

            if (lecture == null) return NotFound();

            // Nếu bài giảng này chưa có Quiz, tạo mới
            if (lecture.Quiz == null)
            {
                lecture.Quiz = new Quiz { LectureID = lectureId, Title = $"Quiz cho bài: {lecture.Title}" };
                _context.Quizzes.Add(lecture.Quiz);
                await _context.SaveChangesAsync();
            }

            return View(lecture);
        }

        // POST: /Instructor/AddQuestion
        // Xử lý Thêm câu hỏi MỚI cho Quiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int quizId, string questionText)
        {
            // <-- ĐÃ SỬA: Lấy quiz để tìm lectureId
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null)
            {
                return NotFound();
            }
            int lectureId = quiz.LectureID; // Lưu lại lectureId để quay về

            // Tạo Question mới, gán vào Quiz
            var newQuestion = new Question
            {
                QuizID = quizId,
                QuestionText = questionText
            };
            _context.Questions.Add(newQuestion);
            await _context.SaveChangesAsync();

            // <-- ĐÃ SỬA: Dùng lectureId đã lưu
            return RedirectToAction(nameof(ManageQuiz), new { lectureId = lectureId });
        }

        // POST: /Instructor/AddAnswer
        // Xử lý Thêm câu trả lời MỚI cho Question
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAnswer(int questionId, string answerText, bool isCorrect)
        {
            // <-- ĐÃ SỬA: Tìm Question -> Quiz -> LectureID
            var question = await _context.Questions
                                    .Include(q => q.Quiz) // Lấy Quiz
                                    .FirstOrDefaultAsync(q => q.QuestionID == questionId);

            if (question == null)
            {
                return NotFound();
            }
            int lectureId = question.Quiz.LectureID; // Lưu lại lectureId

            // Tạo Answer mới, gán vào Question
            var newAnswer = new Answer
            {
                QuestionID = questionId,
                AnswerText = answerText,
                IsCorrect = isCorrect
            };
            _context.Answers.Add(newAnswer);
            await _context.SaveChangesAsync();

            // <-- ĐÃ SỬA: Dùng lectureId đã lưu
            return RedirectToAction(nameof(ManageQuiz), new { lectureId = lectureId });
        }
    }
}