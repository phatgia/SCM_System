using System;
using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Top Cards
        public decimal MonthlyRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal InventoryValue { get; set; }
        public int ActiveDeliveriesCount { get; set; }
        public string CurrencySymbol { get; set; } = "₫";
        public string MonthLabel { get; set; } = string.Empty;

        // Charts
        public List<string> MonthlyLabels { get; set; } = new();
        public List<decimal> MonthlyRevenues { get; set; } = new();
        public List<decimal> MonthlyCosts { get; set; } = new();
        
        public List<string> OrderStatusLabels { get; set; } = new();
        public List<int> OrderStatusCounts { get; set; } = new();

        public List<string> InventoryCategoryLabels { get; set; } = new();
        public List<int> InventoryCategoryQuantities { get; set; } = new();

        // Tables & Lists
        public List<RecentOrderHomeViewModel> RecentOrders { get; set; } = new();
        public List<LowStockAlertViewModel> LowStockWarnings { get; set; } = new();
        public List<ShippingActivityHomeViewModel> ActiveDeliveries { get; set; } = new();
    }

    public class RecentOrderHomeViewModel
    {
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class LowStockAlertViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int Threshold { get; set; }
    }

    public class ShippingActivityHomeViewModel
    {
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ShipperName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
