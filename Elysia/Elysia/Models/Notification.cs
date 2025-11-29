using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Elysia.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        public string UserId { get; set; } // Người nhận
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public string Message { get; set; } // Nội dung thông báo
        public string Url { get; set; }     // Đường dẫn khi click vào
        public bool IsRead { get; set; } = false; // Trạng thái đã xem
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}