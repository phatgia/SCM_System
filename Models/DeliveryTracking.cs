using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models
{
    public class DeliveryTracking
    {
        [Key]
        public int TrackingID { get; set; }
        
        public int DeliveryID { get; set; }

        public string? StatusEvent { get; set; }
        
        public string? Note { get; set; }
        
        public DateTime EventTime { get; set; }
        
        public string? ShipperName { get; set; }
        
    
        public Delivery? Delivery { get; set; } 
    }
}