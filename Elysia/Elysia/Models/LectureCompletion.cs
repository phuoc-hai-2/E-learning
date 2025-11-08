using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elysia.Models
{
    // Bảng trung gian ghi lại việc Sinh viên đã hoàn thành Bài giảng
    public class LectureCompletion
    {
        [Key]
        public int CompletionID { get; set; }

        public DateTime CompletedDate { get; set; } = DateTime.Now;

        // --- Khóa ngoại (Sinh viên nào hoàn thành) ---
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // --- Khóa ngoại (Bài giảng nào được hoàn thành) ---
        public int LectureID { get; set; }
        [ForeignKey("LectureID")]
        public virtual Lecture Lecture { get; set; }
    }
}