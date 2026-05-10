using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models
{
    [Table("Delivery")]
    public class Delivery
    {
        [Key]
        public int DeliveryID { get; set; }

        [ForeignKey("SaleOrder")]
        public int SOID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = string.Empty;

        [StringLength(255)]
        public string? HandShakeProof { get; set; }

        public DateTime? DeliveryTime { get; set; }

        // Navigation
        public virtual SaleOrder SaleOrder { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public ICollection<DeliveryTracking>? DeliveryTrackings { get; set; }
    }
}
