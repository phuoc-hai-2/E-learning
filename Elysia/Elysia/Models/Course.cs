using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Namespace: Elysia.Models
namespace Elysia.Models
{
    public class Course
    {
        [Key] // Khóa chính (tự động tăng)
        public int CourseID { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")] // Kiểu dữ liệu cho tiền tệ
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        // Cột này để Admin duyệt (true = đã duyệt)
        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ----- KHÓA NGOẠI -----
        // ID của Giảng viên (từ ApplicationUser)
        public string UserId { get; set; }

        // Thuộc tính Navigation đến Giảng viên
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // ----- QUAN HỆ 1-NHIỀU -----
        public virtual ICollection<Lecture> Lectures { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
    }
}