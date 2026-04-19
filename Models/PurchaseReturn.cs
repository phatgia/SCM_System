using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models
{
    [Table("PurchaseReturn")]
    public class PurchaseReturn
    {
        [Key]
        public int PurchaseReturnID { get; set; }

        [ForeignKey("PurchaseOrder")]
        public int POID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; } // Nhân viên lập phiếu trả

        [StringLength(500)]
        public string? Reason { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Chờ duyệt";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime ReturnDate { get; set; } = DateTime.Now;

        // Navigation
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
