using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models
{
    [Table("QualityControl")]
    public class QualityControl
    {
        [Key]
        public int QCID { get; set; }

        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string ReferenceID { get; set; } = string.Empty; // POID or SOID

        public DateTime InspectionDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Result { get; set; } = "Đạt"; // "Đạt" or "Không đạt"

        [StringLength(255)]
        public string? DefectType { get; set; }

        public string? Notes { get; set; }

        // Navigation
        public virtual Product? Product { get; set; }
        public virtual User? User { get; set; }
    }
}
