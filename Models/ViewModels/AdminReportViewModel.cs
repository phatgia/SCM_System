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
        public string CpuUsage { get; set; } = "N/A";
        public string EnvironmentInfo { get; set; } = "";
        public int ProcessID { get; set; }

        // History for Charts
        public List<string> HistoryLabels { get; set; } = new();
        public List<double> RamHistory { get; set; } = new();
        public List<int> ThreadHistory { get; set; } = new();

        // Role Distribution for Pie Chart
        public List<string> RoleLabels { get; set; } = new();
        public List<int> RoleCounts { get; set; } = new();

        // Database Stats
        public Dictionary<string, int> EntityCounts { get; set; } = new();

        // Recent System Events
        public List<SystemEventViewModel> RecentEvents { get; set; } = new();
    }
}
