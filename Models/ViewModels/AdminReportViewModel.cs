using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class AdminConfigViewModel
    {
        public int LowStockThreshold { get; set; }
        public int WarrantyAlertDays { get; set; }
        public bool AutoBackup { get; set; }
        public bool EnableEmail { get; set; }
        public bool EnableSMS { get; set; }
        public string Currency { get; set; } = "VND";
        public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
    }

    public class AdminReportViewModel
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
    }
}
