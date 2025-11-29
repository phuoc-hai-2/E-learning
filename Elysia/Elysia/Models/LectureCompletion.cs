using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elysia.Models
{
    public class LectureCompletion
    {
        [Key]
        public int CompletionID { get; set; }

        // --- SỬA TÊN: CompletedDate -> CompletionDate ---
        public DateTime CompletionDate { get; set; } = DateTime.Now;

        // --- Khóa ngoại ---
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public int LectureID { get; set; }
        [ForeignKey("LectureID")]
        public virtual Lecture Lecture { get; set; }
    }
}