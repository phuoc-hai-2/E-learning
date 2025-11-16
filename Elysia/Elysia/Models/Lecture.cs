using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Namespace: Elysia.Models
namespace Elysia.Models
{
    public class Lecture
    {
        [Key]
        public int LectureID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string? VideoUrl { get; set; } // Đường dẫn file video
        public string? Content { get; set; } // Nội dung bài giảng (text/html)

        // Dùng để sắp xếp thứ tự bài giảng
        public int Order { get; set; }

        // ----- KHÓA NGOẠI -----
        public int CourseID { get; set; }

        // Thuộc tính Navigation đến Khóa học
        [ForeignKey("CourseID")]
        public virtual Course Course { get; set; }
        public virtual Quiz Quiz { get; set; }
    }
}