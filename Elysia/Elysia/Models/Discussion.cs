using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elysia.Models
{
    public class Discussion
    {
        [Key]
        public int DiscussionID { get; set; }

        [Required]
        [StringLength(2000)]
        public string CommentText { get; set; }

        public DateTime CommentDate { get; set; } = DateTime.Now;

        // --- Khóa ngoại (Ai là người bình luận) ---
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // --- Khóa ngoại (Bình luận cho bài giảng nào) ---
        public int LectureID { get; set; }
        [ForeignKey("LectureID")]
        public virtual Lecture Lecture { get; set; }
    }
}
