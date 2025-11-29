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
        // --- SỬA TÊN: CommentText -> Content ---
        public string Content { get; set; }

        // --- SỬA TÊN: CommentDate -> CreatedAt ---
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- Khóa ngoại ---
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public int LectureID { get; set; }
        [ForeignKey("LectureID")]
        public virtual Lecture Lecture { get; set; }
    }
}