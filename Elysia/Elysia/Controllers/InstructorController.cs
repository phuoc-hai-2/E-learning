using Elysia.Data;
using Elysia.Models;
using Microsoft.AspNetCore.Authorization; // Bắt buộc có
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Cần cho IFormFile
using Microsoft.AspNetCore.Hosting; // Cần cho IWebHostEnvironment

namespace Elysia.Controllers
{
    // --- QUAN TRỌNG: Dòng này khóa Controller chỉ cho Giảng viên ---
    [Authorize(Roles = "GiangVien")]
    // ---------------------------------------------------------------
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Price")] Course course, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                course.UserId = GetCurrentUserId();
                course.IsApproved = false;
                course.CreatedAt = DateTime.Now;

                if (imageFile != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string path = Path.Combine(wwwRootPath, "uploads/courses", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (var stream = new FileStream(path, FileMode.Create)) { await imageFile.CopyToAsync(stream); }
                    course.ImageUrl = "/uploads/courses/" + fileName;
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
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserId == userId);

            if (course == null) return NotFound();
            return View(course);
        }

        // POST: /Instructor/AddLecture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLecture([Bind("CourseID,Title,Order")] Lecture lecture, IFormFile videoFile)
        {
            if (ModelState.IsValid)
            {
                if (videoFile != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                    string path = Path.Combine(wwwRootPath, "uploads/videos", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (var stream = new FileStream(path, FileMode.Create)) { await videoFile.CopyToAsync(stream); }
                    lecture.VideoUrl = "/uploads/videos/" + fileName;
                }
                _context.Lectures.Add(lecture);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageCourse), new { id = lecture.CourseID });
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
                lecture.Quiz = new Quiz { LectureID = lectureId, Title = "Quiz: " + lecture.Title };
                _context.Quizzes.Add(lecture.Quiz);
                await _context.SaveChangesAsync();
            }
            return View(lecture);
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
            var q = await _context.Questions.Include(q => q.Quiz).FirstOrDefaultAsync(x => x.QuestionID == questionId);
            if (q == null) return NotFound();

            var a = new Answer { QuestionID = questionId, AnswerText = answerText, IsCorrect = isCorrect };
            _context.Answers.Add(a);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageQuiz), new { lectureId = q.Quiz.LectureID });
        }
    }
}