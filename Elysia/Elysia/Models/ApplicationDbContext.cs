using Elysia.Models;
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
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Discussion> Discussions { get; set; }
        public DbSet<LectureCompletion> LectureCompletions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }

        // === CẤU HÌNH QUAN HỆ (Fluent API) ===
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Giảng viên (User) -> Khóa học (Course)
            builder.Entity<Course>()
                .HasOne(c => c.User)
                .WithMany(u => u.CreatedCourses)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Sinh viên (User) -> Đánh giá (Review)
            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. Sinh viên (User) -> Enrollment
            builder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 4. Khóa học (Course) -> Enrollment
            builder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseID)
                .OnDelete(DeleteBehavior.Cascade);

            // 5. Thanh toán (Payment) -> Đăng ký (Enrollment) [ĐÃ SỬA LỖI]
            builder.Entity<Payment>()
                .HasOne(p => p.Enrollment)
                .WithMany(e => e.Payments) // <--- QUAN TRỌNG: Phải trỏ đúng vào biến Payments
                .HasForeignKey(p => p.EnrollmentID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}