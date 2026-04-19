using SCM_System.Models;
using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class SupplierViewModel
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        public decimal TotalPurchased { get; set; }
        public int TotalOrders { get; set; }
    }

    public class PurchaseOrderViewModel
    {
        public int POID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PurchaseReturnViewModel
    {
        public int ReturnID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty; // Name of person who created return
        public string ProductSummary { get; set; } = string.Empty; // For "CPU, RAM..." display
        public string? Reason { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class SupplierPageViewModel
    {
        public List<SupplierViewModel> Suppliers { get; set; } = new();
        public List<PurchaseOrderViewModel> PurchaseOrders { get; set; } = new();
        public List<PurchaseReturnViewModel> ReturnOrders { get; set; } = new();
        public List<Product> AvailableProducts { get; set; } = new();
        
        public string? SearchTerm { get; set; }
        public SupplierFormViewModel Form { get; set; } = new();
        public string CurrencySymbol { get; set; } = "₫";
    }

    public class SupplierFormViewModel
    {
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp.")]
        [StringLength(150)]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }
    }

    // For creating PO with multiple items
    public class CreatePOViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp.")]
        public int SupplierID { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? Notes { get; set; }
        public List<CreatePOItemViewModel> Items { get; set; } = new();
    }

    public class CreatePOItemViewModel
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
