using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models
{
    [Table("SaleOrderDetail")]
    public class SaleOrderDetail
    {
        [Key]
        public int SODetailID { get; set; }

        [ForeignKey("SaleOrder")]
        public int SOID { get; set; }

        [ForeignKey("Product")]
        public int ProductID { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Navigation
        public virtual SaleOrder SaleOrder { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
