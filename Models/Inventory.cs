using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models
{
    [Table("Inventory")]
    public class Inventory
    {
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [ForeignKey("ProductLocation")]
        public int LocationID { get; set; }

        public int QuantityAvailable { get; set; }

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual ProductLocation ProductLocation { get; set; } = null!;
    }
}
