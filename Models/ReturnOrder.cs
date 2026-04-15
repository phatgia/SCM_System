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

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(100)]
        public string? Condition { get; set; }

        // Navigation
        public virtual SaleOrder SaleOrder { get; set; } = null!;
    }
}
