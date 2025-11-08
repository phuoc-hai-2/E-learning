using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

// Namespace: Elysia.Models
namespace Elysia.Models
{
    // Kế thừa từ IdentityUser để thêm các cột tùy chỉnh
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        public string? AvatarUrl { get; set; }

        // ----- CÁC MỐI QUAN HỆ -----
        // 1 Giảng viên (User) có thể tạo nhiều Khóa học
        public virtual ICollection<Course>? CreatedCourses { get; set; }

        // 1 Sinh viên (User) có thể đăng ký nhiều Khóa học
        public virtual ICollection<Enrollment>? Enrollments { get; set; }

        // 1 Sinh viên (User) có thể viết nhiều Đánh giá
        public virtual ICollection<Review>? Reviews { get; set; }
    }
}