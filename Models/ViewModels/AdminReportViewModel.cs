using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class AdminConfigViewModel
    {
        public int LowStockThreshold { get; set; }
        public bool AutoBackup { get; set; }
        public bool EnableEmail { get; set; }
        public bool EnableSMS { get; set; }
        public string Currency { get; set; } = "VND";
        public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
    }

    public class AdminReportViewModel
    {
        public double RamUsageMB { get; set; }
        public int ThreadCount { get; set; }
        public string Uptime { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string CpuUsage { get; set; } = "N/A"; // Placeholder for future if needed
    }
}
