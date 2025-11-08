using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elysia.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; } // Số tiền thanh toán

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string PaymentMethod { get; set; } // "MoMo", "BankTransfer", "Demo"
        public string Status { get; set; } // "Pending", "Completed", "Failed"

        // Dùng để gửi hóa đơn
        public string? TransactionId { get; set; }

        // --- Khóa ngoại (Thanh toán này thuộc về lượt đăng ký nào) ---
        public int EnrollmentID { get; set; }
        [ForeignKey("EnrollmentID")]
        public virtual Enrollment Enrollment { get; set; }
    }
}