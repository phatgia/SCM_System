using SCM_System.Models;

namespace SCM_System.Models.ViewModels
{
    public class SalesOrdersViewModel
    {
        public List<SaleOrderListItem> Orders { get; set; } = new();
        public string CurrencySymbol { get; set; } = "₫";
    }

    public class SaleOrderListItem
    {
        public int SOID { get; set; }
        public string OrderCode => $"SO-{SOID:D5}";
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ProductSummary { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SalesStockViewModel
    {
        public List<StockItemViewModel> Stocks { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }

    public class StockItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU => $"SKU-{ProductID:D5}";
        public string CategoryName { get; set; } = string.Empty;
        public int PhysicalStock { get; set; }
        public int ReservedStock { get; set; } // e.g., in transit or pending sale
        public int AvailableStock => PhysicalStock - ReservedStock;
        public string LocationName { get; set; } = string.Empty;
    }

    public class SalesCreateViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CreateSaleOrderRequest
    {
        // Existing Customer
        public int? CustomerID { get; set; }
        
        // New Customer (if ID is null)
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? ShippingAddress { get; set; }

        public string? Note { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }
}
