using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models
{
    [Table("ProductLocation")]
    public class ProductLocation
    {
        [Key]
        public int LocationID { get; set; }

        [Required]
        [StringLength(50)]
        public string LocationCode { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        // Navigation
        public virtual ICollection<ProductSerial> ProductSerials { get; set; } = new List<ProductSerial>();
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
