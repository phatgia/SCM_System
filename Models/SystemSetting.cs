using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models
{
    [Table("SystemSettings")]
    public class SystemSetting
    {
        [Key]
        public int SettingID { get; set; }

        public int LowStockThreshold { get; set; } = 5;

        public bool AutoBackup { get; set; } = true;
        public bool EnableEmail { get; set; } = true;
        public bool EnableSMS { get; set; } = false;

        [StringLength(10)]
        public string Currency { get; set; } = "VND";

        [StringLength(50)]
        public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
    }
}
