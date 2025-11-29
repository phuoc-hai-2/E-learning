using Elysia.Data;
using Elysia.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Elysia.Controllers
{
    [Authorize(Roles = "GiangVien")]
    public class InstructorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public InstructorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET: /Instructor/Index
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            ViewBag.TotalCourses = await _context.Courses.CountAsync(c => c.UserId == userId);
            return View();
        }

        // GET: /Instructor/MyCourses
        public async Task<IActionResult> MyCourses()
        {
            var userId = GetCurrentUserId();
            var myCourses = await _context.Courses
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(myCourses);
        }

        // GET: /Instructor/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Instructor/Create
        // --- ĐÃ SỬA: ĐĂNG LÀ DUYỆT LUÔN ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Price")] Course course, IFormFile imageFile)
        {
            // Loại bỏ validation các trường hệ thống
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Lectures");
            ModelState.Remove("Enrollments");
            ModelState.Remove("Reviews");

            if (ModelState.IsValid)
            {
                course.UserId = GetCurrentUserId();

                // === SỬA TẠI ĐÂY: Cho phép hiển thị ngay lập tức ===
                course.IsApproved = true;
                // ===================================================

                course.CreatedAt = DateTime.Now;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string path = Path.Combine(wwwRootPath, "uploads", "courses", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    course.ImageUrl = "/uploads/courses/" + fileName;
                }
                else
                {
                    course.ImageUrl = "/images/default-course.jpg";
                }

                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyCourses));
            }
            return View(course);
        }

        // GET: /Instructor/ManageCourse/5
        public async Task<IActionResult> ManageCourse(int id)
        {
            var userId = GetCurrentUserId();
            var course = await _context.Courses
                .Include(c => c.Lectures.OrderBy(l => l.Order))
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserId == userId);

            if (course == null) return NotFound();
            return View(course);
        }

        // 1. GET: Hiển thị form thêm bài giảng
        public IActionResult CreateLecture(int courseId)
        {
            return View(new Lecture { CourseID = courseId });
        }

        // POST: /Instructor/AddLecture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLecture([Bind("CourseID,Title,Description,Order")] Lecture lecture, IFormFile videoFile)
        {
            ModelState.Remove("VideoUrl");
            ModelState.Remove("Course");
            ModelState.Remove("Quiz");

            if (ModelState.IsValid)
            {
                // 1. Upload Video
                if (videoFile != null && videoFile.Length > 0)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                    string path = Path.Combine(wwwRootPath, "uploads", "videos", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await videoFile.CopyToAsync(stream);
                    }
                    lecture.VideoUrl = "/uploads/videos/" + fileName;
                }

                // 2. Lưu Lecture
                _context.Lectures.Add(lecture);
                await _context.SaveChangesAsync();

                // 3. === GỬI THÔNG BÁO CHO SINH VIÊN ===
                // Lấy tên khóa học
                var course = await _context.Courses.FindAsync(lecture.CourseID);
                string courseTitle = course?.Title ?? "Khóa học";

                // Lấy danh sách sinh viên đã Enroll khóa này
                var studentIds = await _context.Enrollments
                    .Where(e => e.CourseID == lecture.CourseID)
                    .Select(e => e.UserId)
                    .ToListAsync();

                if (studentIds.Any())
                {
                    var notifications = new List<Notification>();
                    foreach (var sid in studentIds)
                    {
                        notifications.Add(new Notification
                        {
                            UserId = sid,
                            Message = $"Bài giảng mới '{lecture.Title}' đã được thêm vào khóa học '{courseTitle}'.",
                            Url = $"/Courses/Watch?id={lecture.CourseID}", // Link đến bài học
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });
                    }
                    _context.Notifications.AddRange(notifications);
                    await _context.SaveChangesAsync();
                }
                // ========================================

                return RedirectToAction(nameof(ManageCourse), new { id = lecture.CourseID });
            }
            return View("CreateLecture", lecture);
        }

        // POST: /Instructor/DeleteLecture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLecture(int lectureId)
        {
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture == null) return NotFound();

            int courseId = lecture.CourseID;
            _context.Lectures.Remove(lecture);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageCourse), new { id = courseId });
        }

        // GET: /Instructor/ManageQuiz
        public async Task<IActionResult> ManageQuiz(int lectureId)
        {
            var lecture = await _context.Lectures
                .Include(l => l.Quiz)
                    .ThenInclude(q => q.Questions)
                    .ThenInclude(qn => qn.Answers)
                .FirstOrDefaultAsync(l => l.LectureID == lectureId);

            if (lecture == null) return NotFound();

            if (lecture.Quiz == null)
            {
                lecture.Quiz = new Quiz
                {
                    LectureID = lectureId,
                    Title = "Quiz: " + lecture.Title,
                    Questions = new List<Question>()
                };
                _context.Quizzes.Add(lecture.Quiz);
                await _context.SaveChangesAsync();
            }
            return View(lecture.Quiz);
        }

        // POST: /Instructor/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int quizId, string questionText)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null) return NotFound();

            var q = new Question { QuizID = quizId, QuestionText = questionText };
            _context.Questions.Add(q);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageQuiz), new { lectureId = quiz.LectureID });
        }

        // GET: /Instructor/EditLecture
        public async Task<IActionResult> EditLecture(int lectureId)
        {
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture == null) return NotFound();
            return View(lecture);
        }

        // POST: /Instructor/EditLecture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecture(int id, Lecture lecture, IFormFile? videoFile)
        {
            if (id != lecture.LectureID) return NotFound();
            ModelState.Remove("VideoUrl");
            ModelState.Remove("Course");
            ModelState.Remove("Quiz");

            if (ModelState.IsValid)
            {
                try
                {
                    var oldLecture = await _context.Lectures.AsNoTracking().FirstOrDefaultAsync(l => l.LectureID == id);
                    if (oldLecture != null)
                    {
                        if (videoFile != null && videoFile.Length > 0)
                        {
                            string wwwRootPath = _webHostEnvironment.WebRootPath;
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                            string path = Path.Combine(wwwRootPath, "uploads", "videos", fileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            using (var stream = new FileStream(path, FileMode.Create)) { await videoFile.CopyToAsync(stream); }
                            lecture.VideoUrl = "/uploads/videos/" + fileName;
                        }
                        else
                        {
                            lecture.VideoUrl = oldLecture.VideoUrl;
                        }
                    }
                    _context.Update(lecture);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Lectures.Any(e => e.LectureID == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(ManageCourse), new { id = lecture.CourseID });
            }
            return View(lecture);
        }

        // GET: /Instructor/EditCourse/5
        public async Task<IActionResult> EditCourse(int id)
        {
            var userId = GetCurrentUserId();
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == id && c.UserId == userId);
            if (course == null) return NotFound();
            return View(course);
        }

        // POST: /Instructor/EditCourse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, [Bind("CourseID,Title,Description,Price")] Course course, IFormFile? imageFile)
        {
            if (id != course.CourseID) return NotFound();
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Lectures");
            ModelState.Remove("Enrollments");
            ModelState.Remove("Reviews");

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = GetCurrentUserId();
                    var oldCourse = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseID == id && c.UserId == userId);
                    if (oldCourse == null) return NotFound();

                    course.UserId = userId;
                    course.CreatedAt = oldCourse.CreatedAt;
                    course.IsApproved = oldCourse.IsApproved; // Giữ nguyên trạng thái cũ

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string path = Path.Combine(wwwRootPath, "uploads", "courses", fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        using (var stream = new FileStream(path, FileMode.Create)) { await imageFile.CopyToAsync(stream); }
                        course.ImageUrl = "/uploads/courses/" + fileName;
                    }
                    else
                    {
                        course.ImageUrl = oldCourse.ImageUrl;
                    }
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Courses.Any(e => e.CourseID == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(ManageCourse), new { id = course.CourseID });
            }
            return View(course);
        }

        // POST: /Instructor/AddAnswer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAnswer(int questionId, string answerText, bool isCorrect)
        {
            var q = await _context.Questions.Include(x => x.Quiz).FirstOrDefaultAsync(x => x.QuestionID == questionId);
            if (q == null) return NotFound();

            var a = new Answer { QuestionID = questionId, AnswerText = answerText, IsCorrect = isCorrect };
            _context.Answers.Add(a);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageQuiz), new { lectureId = q.Quiz.LectureID });
        }
    }
}