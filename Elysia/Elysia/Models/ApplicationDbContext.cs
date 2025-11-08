using Elysia.Models; // Quan trọng: Phải using namespace Models
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

// Namespace: Elysia.Data
namespace Elysia.Data
{
    // Kế thừa từ IdentityDbContext<ApplicationUser>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // === KHAI BÁO TẤT CẢ CÁC BẢNG (DbSet) ===

        // (Các bảng Identity như AspNetUsers, AspNetRoles được tự động thêm)

        // Bảng Quản lý Khóa học & Nội dung
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lecture> Lectures { get; set; }

        // Bảng Quản lý Trắc nghiệm (Quiz)
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }

        // Bảng Quản lý Học tập & Tương tác
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Discussion> Discussions { get; set; }
        public DbSet<LectureCompletion> LectureCompletions { get; set; }

        // Bảng Quản lý Thanh toán
        public DbSet<Payment> Payments { get; set; }


        // === CẤU HÌNH QUAN HỆ (Fluent API) ===
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Phải gọi base.OnModelCreating(builder) đầu tiên
            base.OnModelCreating(builder);

            // Cấu hình cho quan hệ 1-Nhiều: Giảng viên (User) -> Khóa học (Course)
            builder.Entity<Course>()
                .HasOne(c => c.User) // Một Course chỉ thuộc 1 User (Giảng viên)
                .WithMany(u => u.CreatedCourses) // Một User có thể tạo nhiều Course
                .HasForeignKey(c => c.UserId) // Khóa ngoại là UserId
                .OnDelete(DeleteBehavior.Restrict); // QUAN TRỌNG: Không cho xóa Giảng viên nếu họ còn Khóa học

            // Cấu hình cho quan hệ 1-Nhiều: Sinh viên (User) -> Đánh giá (Review)
            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa User thì xóa Review của họ

            // Cấu hình cho quan hệ N-N: Sinh viên (User) <-> Khóa học (Course)
            // thông qua bảng trung gian Enrollment
            builder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa User thì xóa Enrollment

            builder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseID)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Course thì xóa Enrollment

            // Cấu hình cho quan hệ 1-Nhiều: Thanh toán (Payment) -> Đăng ký (Enrollment)
            builder.Entity<Payment>()
                .HasOne(p => p.Enrollment)
                .WithMany() // Một Enrollment có thể có nhiều Payment (nếu thanh toán thất bại)
                .HasForeignKey(p => p.EnrollmentID)
                .OnDelete(DeleteBehavior.Cascade); // XT
        }
    }
}