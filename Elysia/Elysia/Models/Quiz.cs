using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elysia.Models
{
    public class Quiz
    {
        [Key]
        public int QuizID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        // --- Khóa ngoại (Gắn Quiz vào Bài giảng) ---
        public int LectureID { get; set; }
        [ForeignKey("LectureID")]
        public virtual Lecture Lecture { get; set; }

        // --- Quan hệ 1-Nhiều (1 Quiz có nhiều Câu hỏi) ---
        public virtual ICollection<Question> Questions { get; set; }
    }
}