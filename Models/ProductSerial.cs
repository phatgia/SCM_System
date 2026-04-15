using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models
{
    [Table("ProductSerial")]
    public class ProductSerial
    {
        [Key]
        public int SerialID { get; set; }

        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string SerialNumber { get; set; } = string.Empty;

        [ForeignKey("ProductLocation")]
        public int LocationID { get; set; }

        [ForeignKey("PurchaseOrder")]
        public int POID { get; set; }

        public DateTime? WarrantyEndDate { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = string.Empty;

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual ProductLocation ProductLocation { get; set; } = null!;
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    }
}
