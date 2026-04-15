using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SCM_System.Models
{
    [Table("User")]
    [Index(nameof(Username), IsUnique = true)]
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [ForeignKey("Role")]
        public int RoleID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        // Navigation
        public virtual Role Role { get; set; } = null!;
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public virtual ICollection<SaleOrder> SaleOrders { get; set; } = new List<SaleOrder>();
        public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    }
}
