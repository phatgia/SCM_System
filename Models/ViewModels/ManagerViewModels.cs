using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class ManagerReportViewModel
    {
        public string TotalRevenue { get; set; } = "0";
        public string TotalExpense { get; set; } = "0";
        public int CompletedOrdersCount { get; set; }
        public string DeliverySuccessRate { get; set; } = "0";
        public string ReturnRate { get; set; } = "0";
        public string CurrencySymbol { get; set; } = "₫";

        // For Chart
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ChartDataRevenue { get; set; } = new();
        public List<decimal> ChartDataExpense { get; set; } = new();

        public string ReportType { get; set; } = "Tổng quan";

        public List<RecentOrderHomeViewModel> RecentOrders { get; set; } = new();
        public List<TopProductViewModel> TopProducts { get; set; } = new();
    }

    public class TopProductViewModel
    {
        public string ProductName { get; set; } = "";
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
    }
}
