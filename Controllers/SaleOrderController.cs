using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models;
using SCM_System.Models.ViewModels;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly SCMDbContext _context;

        public SalesController(SCMDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // GET: /Sales/Create
        // =====================================================================
        public async Task<IActionResult> Create()
        {
            var vm = new SalesCreateViewModel
            {
                Products = await _context.Products.ToListAsync(),
                Customers = await _context.Customers.ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder([FromBody] CreateSaleOrderRequest request)
        {
            if (request == null || !request.Items.Any())
                return BadRequest("Dữ liệu đơn hàng không hợp lệ.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int customerId;
                if (request.CustomerID.HasValue && request.CustomerID > 0)
                {
                    customerId = request.CustomerID.Value;
                }
                else
                {
                    // Create new customer
                    var newCustomer = new Customer
                    {
                        Name = request.CustomerName ?? "Khách lẻ",
                        Phone = request.CustomerPhone ?? "",
                        Email = request.CustomerEmail ?? "",
                        ShippingAddress = request.ShippingAddress ?? ""
                    };
                    _context.Customers.Add(newCustomer);
                    await _context.SaveChangesAsync();
                    customerId = newCustomer.CustomerID;
                }

                // 1. Create SaleOrder
                var order = new SaleOrder
                {
                    CustomerID = customerId,
                    UserID = int.Parse(userIdStr),
                    OrderDate = DateTime.Now,
                    Status = "Đang xử lý",
                    TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice)
                };
                _context.SaleOrders.Add(order);
                await _context.SaveChangesAsync();

                // 2. Create Details & Update Inventory
                foreach (var item in request.Items)
                {
                    var detail = new SaleOrderDetail
                    {
                        SOID = order.SOID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _context.SaleOrderDetails.Add(detail);

                    // Update Inventory (Subtract from total stock)
                    // Simplified: just update the first location that has the product
                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == item.ProductID);
                    if (inventory != null)
                    {
                        inventory.QuantityAvailable -= item.Quantity;
                        if (inventory.QuantityAvailable < 0) inventory.QuantityAvailable = 0; 
                        _context.Update(inventory);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Tạo đơn bán hàng thành công!", orderId = order.SOID });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        // =====================================================================
        // GET: /Sales/Orders
        // =====================================================================
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.SaleOrders
                .Include(so => so.Customer)
                .Include(so => so.SaleOrderDetails)
                .ThenInclude(d => d.Product)
                .OrderByDescending(so => so.OrderDate)
                .Select(so => new SaleOrderListItem
                {
                    SOID = so.SOID,
                    CustomerName = so.Customer.Name,
                    CustomerPhone = so.Customer.Phone,
                    TotalAmount = so.TotalAmount,
                    OrderDate = so.OrderDate,
                    Status = so.Status,
                    ProductSummary = string.Join(", ", so.SaleOrderDetails.Select(d => d.Product.ProductName).Take(2))
                })
                .ToListAsync();

            // 5. Get Currency Symbol and Rate
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSetting();
            decimal rate = 1;
            if (settings.Currency == "USD") rate = 25000;
            else if (settings.Currency == "EUR") rate = 27000;
            string symbol = settings.Currency == "VND" ? "₫" : (settings.Currency == "USD" ? "$" : "€");

            foreach (var o in orders) o.TotalAmount /= rate;

            return View(new SalesOrdersViewModel { Orders = orders, CurrencySymbol = symbol });
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.SaleOrders
                .Include(so => so.Customer)
                .Include(so => so.SaleOrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(so => so.SOID == id);

            if (order == null) return NotFound();

            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSetting();
            decimal rate = 1;
            if (settings.Currency == "USD") rate = 25000;
            else if (settings.Currency == "EUR") rate = 27000;
            string symbol = settings.Currency == "VND" ? "₫" : (settings.Currency == "USD" ? "$" : "€");

            ViewBag.CurrencySymbol = symbol;

            // Divide for display
            order.TotalAmount /= rate;
            foreach (var detail in order.SaleOrderDetails)
            {
                detail.UnitPrice /= rate;
            }

            return PartialView("_OrderDetailsPartial", order);
        }

        // =====================================================================
        // GET: /Sales/Stock
        // =====================================================================
        public async Task<IActionResult> Stock()
        {
            var stocks = await _context.Inventories
                .Include(i => i.Product)
                .ThenInclude(p => p.Category)
                .Include(i => i.ProductLocation)
                .Select(i => new StockItemViewModel
                {
                    ProductID = i.ProductID,
                    ProductName = i.Product.ProductName,
                    CategoryName = i.Product.Category.CategoryName,
                    PhysicalStock = i.QuantityAvailable,
                    ReservedStock = 0, // In real app, calculate from pending SOs
                    LocationName = i.ProductLocation.LocationCode
                })
                .ToListAsync();

            var categories = await _context.Categories.ToListAsync();

            return View(new SalesStockViewModel { Stocks = stocks, Categories = categories });
        }

        // =====================================================================
        // GET: /Sales/Returns
        // =====================================================================
        public async Task<IActionResult> Returns()
        {
            var returns = await _context.ReturnOrders
                .Include(r => r.SaleOrder)
                .ThenInclude(so => so.Customer)
                .Include(r => r.SaleOrder.SaleOrderDetails)
                .ThenInclude(d => d.Product)
                .OrderByDescending(r => r.ReturnID)
                .Select(r => new ReturnOrderViewModel
                {
                    ReturnID = r.ReturnID,
                    SaleOrderCode = $"SO-{r.SOID:D4}",
                    CustomerName = r.SaleOrder.Customer.Name,
                    ProductSummary = string.Join(", ", r.SaleOrder.SaleOrderDetails.Select(d => d.Product.ProductName).Take(2)),
                    Settlement = r.Settlement ?? "N/A",
                    Status = r.Status
                })
                .ToListAsync();

            var saleOrders = await _context.SaleOrders
                .Include(so => so.Customer)
                .OrderByDescending(so => so.OrderDate)
                .Take(50)
                .ToListAsync();

            var vm = new SalesReturnViewModel
            {
                Returns = returns,
                SaleOrders = saleOrders
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReturn(CreateReturnRequest request)
        {
            if (request.SOID == 0) return RedirectToAction("Returns");
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var returnOrder = new ReturnOrder
            {
                SOID = request.SOID,
                UserID = int.Parse(userIdStr),
                Reason = request.Reason,
                Settlement = request.Settlement,
                Status = "Đang xử lý"
            };

            _context.ReturnOrders.Add(returnOrder);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã tạo yêu cầu đổi trả thành công!";
            return RedirectToAction("Returns");
        }
    }
}
