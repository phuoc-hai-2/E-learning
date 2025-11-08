using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elysia.Models
{
    public class Question
    {
        [Key]
        public int QuestionID { get; set; }

        [Required]
        public string QuestionText { get; set; }

        // --- Khóa ngoại (Câu hỏi thuộc Quiz nào) ---
        public int QuizID { get; set; }
        [ForeignKey("QuizID")]
        public virtual Quiz Quiz { get; set; }

        // --- Quan hệ 1-Nhiều (1 Câu hỏi có nhiều Câu trả lời) ---
        public virtual ICollection<Answer> Answers { get; set; }
    }
}