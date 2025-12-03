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
    [Authorize(Roles = "GiangVien")]
    public class InstructorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;

        public InstructorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Price")] Course course, IFormFile imageFile)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Lectures");
            ModelState.Remove("Enrollments");
            ModelState.Remove("Reviews");

            if (ModelState.IsValid)
            {
                course.UserId = GetCurrentUserId();
                course.IsApproved = true; // Tự động duyệt
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
                    course.ImageUrl = "/images/default-course.png";
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

        // GET: /Instructor/CreateLecture
        public IActionResult CreateLecture(int courseId)
        {
            return View(new Lecture { CourseID = courseId });
        }

        // POST: /Instructor/AddLecture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLecture([Bind("CourseID,Title,Order")] Lecture lecture, IFormFile videoFile)
        {
            ModelState.Remove("VideoUrl");
            ModelState.Remove("Course");
            ModelState.Remove("Quiz");
            ModelState.Remove("Discussions");
            ModelState.Remove("Content");

            if (ModelState.IsValid)
            {
                if (videoFile != null && videoFile.Length > 0)
                {
                    try
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
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi upload video: " + ex.Message);
                        return View("CreateLecture", lecture);
                    }
                }

                _context.Lectures.Add(lecture);
                await _context.SaveChangesAsync();

                // === GỬI THÔNG BÁO ===
                var course = await _context.Courses.FindAsync(lecture.CourseID);
                var enrolledUsers = await _context.Enrollments
                    .Where(e => e.CourseID == lecture.CourseID)
                    .Include(e => e.User)
                    .ToListAsync();

                if (enrolledUsers.Any())
                {
                    var notifications = new List<Notification>();
                    foreach (var enroll in enrolledUsers)
                    {
                        // 1. Thông báo Web
                        notifications.Add(new Notification
                        {
                            UserId = enroll.UserId,
                            Message = $"Bài giảng mới '{lecture.Title}' đã được thêm vào khóa học '{course?.Title}'.",
                            Url = $"/Courses/Watch?id={lecture.CourseID}",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });

                        // 2. Gửi Email
                        if (!string.IsNullOrEmpty(enroll.User.Email))
                        {
                            await _emailSender.SendEmailAsync(enroll.User.Email, "Bài học mới từ Giảng viên",
                                $"<h3>Xin chào {enroll.User.FullName},</h3><p>Khóa học <strong>{course?.Title}</strong> vừa có bài giảng mới: <strong>{lecture.Title}</strong>.</p><p><a href='https://elysia-elearning.com/Courses/Watch?id={lecture.CourseID}'>Vào học ngay</a></p>");
                        }
                    }
                    _context.Notifications.AddRange(notifications);
                    await _context.SaveChangesAsync();
                }
                // =====================

                return RedirectToAction(nameof(ManageCourse), new { id = lecture.CourseID });
            }
            return View("CreateLecture", lecture);
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
                    course.IsApproved = oldCourse.IsApproved;

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
            ModelState.Remove("Discussions");
            ModelState.Remove("Content");

            if (ModelState.IsValid)
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
                    _context.Update(lecture);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(ManageCourse), new { id = lecture.CourseID });
            }
            return View(lecture);
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
                .Include(l => l.Quiz).ThenInclude(q => q.Questions).ThenInclude(qn => qn.Answers)
                .FirstOrDefaultAsync(l => l.LectureID == lectureId);

            if (lecture == null) return NotFound();

            if (lecture.Quiz == null)
            {
                lecture.Quiz = new Quiz
                {
                    LectureID = lectureId,
                    Title = "Bài tập trắc nghiệm: " + lecture.Title,
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