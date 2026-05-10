namespace SCM_System.Models.ViewModels;

public class AdminCombinedViewModel
{
    public AdminUserViewModel UserVM { get; set; } = null!;
    public AdminConfigViewModel ConfigVM { get; set; } = null!;
    public AdminReportViewModel ReportVM { get; set; } = null!;
}