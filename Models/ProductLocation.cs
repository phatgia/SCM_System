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

        public int Capacity { get; set; } = 100;

        [StringLength(50)]
        public string LocationType { get; set; } = "Thông thường";

        // Navigation
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
