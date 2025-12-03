using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elysia.Models
{
    public class Enrollment
    {
        [Key]
        public int EnrollmentID { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        // Cột theo dõi tiến độ
        [Column(TypeName = "decimal(5, 2)")]
        public decimal ProgressPercent { get; set; } = 0;

        // ----- KHÓA NGOẠI (Tới Sinh viên) -----
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // ----- KHÓA NGOẠI (Tới Khóa học) -----
        public int CourseID { get; set; }
        [ForeignKey("CourseID")]
        public virtual Course Course { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }
}