using SCM_System.Models;

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

        // Thống kê bổ sung
        public decimal TotalPurchased { get; set; }
        public int TotalOrders { get; set; }
    }

    public class SupplierPageViewModel
    {
        public List<SupplierViewModel> Suppliers { get; set; } = new();
        public string? SearchTerm { get; set; }

        // Dùng cho modal thêm/sửa
        public SupplierFormViewModel Form { get; set; } = new();
    }

    public class SupplierFormViewModel
    {
        public int SupplierID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp.")]
        [System.ComponentModel.DataAnnotations.StringLength(150)]
        public string SupplierName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string? ContactPerson { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(20)]
        public string? Phone { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string? Email { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(255)]
        public string? Address { get; set; }
    }
}
