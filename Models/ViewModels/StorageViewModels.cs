using System;
using System.Collections.Generic;
using SCM_System.Models;

namespace SCM_System.Models.ViewModels
{
    public class StoragePageViewModel
    {
        public List<InboundOrderViewModel> InboundOrders { get; set; } = new();
        public List<LocationOccupancyViewModel> Zones { get; set; } = new();
        public List<LocationDetailViewModel> Locations { get; set; } = new();
        public List<PickingOrderViewModel> PickingOrders { get; set; } = new();
        public List<InventoryItemViewModel> InventoryItems { get; set; } = new();
        public List<HandoverOrderViewModel> HandoverOrders { get; set; } = new();
        public List<StorageReturnViewModel> Returns { get; set; } = new();
        public List<QCRecordViewModel> QCRecords { get; set; } = new();
        public List<Product> AllProducts { get; set; } = new();
        
        public string CurrencySymbol { get; set; } = "₫";
    }

    public class InboundOrderViewModel
    {
        public int POID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class LocationOccupancyViewModel
    {
        public string ZoneName { get; set; } = string.Empty; // Khu A, B, C...
        public int TotalCapacity { get; set; }
        public int TotalUsed { get; set; }
        public decimal OccupancyRate => TotalCapacity > 0 ? ((decimal)TotalUsed * 100 / TotalCapacity) : 0;
        public int FillPercentage => (int)OccupancyRate;
    }

    public class LocationDetailViewModel
    {
        public int LocationID { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int Used { get; set; }
        public int Empty => Capacity - Used;
        public decimal OccupancyRate => Capacity > 0 ? ((decimal)Used * 100 / Capacity) : 0;
        public int FillPercentage => (int)OccupancyRate;
    }

    public class PickingOrderViewModel
    {
        public int SOID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<PickingItemViewModel> Items { get; set; } = new();
    }

    public class PickingItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SuggestedLocationCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int StockAtLocation { get; set; }
        public bool IsPicked { get; set; }
    }

    public class InventoryItemViewModel
    {
        public int ProductID { get; set; }
        public int LocationID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int QuantityAvailable { get; set; }
        public int MinStock { get; set; } = 5; // Default for UI
        public string Status => QuantityAvailable < MinStock ? "Sắp hết" : "Đủ hàng";
    }

    public class HandoverOrderViewModel
    {
        public int SOID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public string ShipperName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class StorageReturnViewModel
    {
        public int ReturnID { get; set; }
        public int SOID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class QCRecordViewModel
    {
        public int QCID { get; set; }
        public string Type { get; set; } = string.Empty;
        public string ReferenceID { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public int PassQuantity { get; set; }
        public int FailQuantity { get; set; }
        public string Result { get; set; } = string.Empty;
        public string InspectorName { get; set; } = string.Empty;
        public DateTime CheckDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

}
