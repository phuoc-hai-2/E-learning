using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elysia.Models
{
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        // Số sao đánh giá (ví dụ: 1 đến 5)
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.Now;

        // --- KHÓA NGOẠI (Tới Sinh viên) ---
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // --- KHÓA NGOẠI (Tới Khóa học) ---
        public int CourseID { get; set; }
        [ForeignKey("CourseID")]
        public virtual Course Course { get; set; }
    }
}