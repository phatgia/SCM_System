using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models
{
    [Table("Product")]
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        [StringLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [ForeignKey("Category")]
        public int CategoryID { get; set; }

        [StringLength(30)]
        public string? Unit { get; set; }

        public int? WarrantyMonths { get; set; }

        // Navigation
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
        public virtual ICollection<SaleOrderDetail> SaleOrderDetails { get; set; } = new List<SaleOrderDetail>();
        public virtual ICollection<ProductSerial> ProductSerials { get; set; } = new List<ProductSerial>();
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
