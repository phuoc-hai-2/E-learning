using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elysia.Models
{
    public class Answer
    {
        [Key]
        public int AnswerID { get; set; }

        [Required]
        public string AnswerText { get; set; }

        // Cột này xác định đây có phải đáp án đúng hay không
        public bool IsCorrect { get; set; } = false;

        // --- Khóa ngoại (Câu trả lời thuộc Câu hỏi nào) ---
        public int QuestionID { get; set; }
        [ForeignKey("QuestionID")]
        public virtual Question Question { get; set; }
    }
}