using SCM_System.Models;

namespace SCM_System.Models.ViewModels
{
    public class SalesReturnViewModel
    {
        public List<ReturnOrderViewModel> Returns { get; set; } = new();
        public List<SaleOrder> SaleOrders { get; set; } = new(); // For creation modal dropdown
    }

    public class ReturnOrderViewModel
    {
        public int ReturnID { get; set; }
        public string RequestCode => $"DT-2026-{ReturnID:D3}";
        public string SaleOrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductSummary { get; set; } = string.Empty;
        public string Settlement { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateReturnRequest
    {
        public int SOID { get; set; }
        public string Settlement { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
