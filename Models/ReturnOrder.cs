using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models
{
    [Table("ReturnOrder")]
    public class ReturnOrder
    {
        [Key]
        public int ReturnID { get; set; }

        [ForeignKey("SaleOrder")]
        public int SOID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; } // Nhân viên tiếp nhận hàng trả từ khách

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(100)]
        public string? Settlement { get; set; } // Thay thế cho Condition (Đổi mới, Hoàn tiền...)

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Đang xử lý"; // Trạng thái: Đang xử lý, Hoàn thành, Đã hủy

        // Navigation
        public virtual SaleOrder SaleOrder { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
