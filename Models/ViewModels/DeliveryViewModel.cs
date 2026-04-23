using SCM_System.Models;

namespace SCM_System.Models.ViewModels
{
    // ── Item hiển thị trong tab Danh sách giao hàng ───────────────────────
    public class DeliveryListItem
    {
        public int DeliveryID { get; set; }
        public string OrderCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string ShipperName { get; set; } = "";
        public string Address { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
        public DateTime? DeliveryTime { get; set; }
    }

    // ── Shipper để chọn trong modal phân công ─────────────────────────────
    public class ShipperItem
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = "";
        public int ActiveDeliveries { get; set; }
        public string StatusLabel => ActiveDeliveries == 0
            ? "Đang rảnh"
            : $"Đang bận — {ActiveDeliveries} đơn";
    }

    // ── ViewModel tổng hợp cho toàn bộ trang Vận chuyển ──────────────────
    public class DeliveryViewModel
    {
        // Tab 2: Danh sách giao hàng
        public List<DeliveryListItem> AllDeliveries { get; set; } = new();
        public int PendingPickupCount { get; set; }
        public int InDeliveryCount { get; set; }
        public int CompletedThisMonthCount { get; set; }

        // Dùng chung: danh sách shipper cho modal phân công
        public List<ShipperItem> Shippers { get; set; } = new();
    }
}