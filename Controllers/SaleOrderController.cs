using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models;
using SCM_System.Models.ViewModels;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Quản trị viên,Nhân viên bán hàng")]
    public class SalesController : Controller
    {
        private readonly SCMDbContext _context;

        public SalesController(SCMDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // TRANG CHÍNH: GỘP 4 TAB (TẠO ĐƠN, ĐƠN BÁN, TỒN KHO, ĐỔI TRẢ)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Sales(string? searchOrder, string? searchStock, int? categoryId, string? searchReturn)        {
            // Lấy cài đặt tiền tệ dùng chung
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSetting();
            decimal rate = 1;
            if (settings.Currency == "USD") rate = 25000;
            else if (settings.Currency == "EUR") rate = 27000;
            string symbol = settings.Currency == "VND" ? "₫" : (settings.Currency == "USD" ? "$" : "€");

            // --- 1. LẤY DỮ LIỆU TAB TẠO ĐƠN (CREATE) ---
            var createVM = new SalesCreateViewModel
            {
                Products = await _context.Products.ToListAsync(),
                Customers = await _context.Customers.ToListAsync()
            };

            // --- 2. LẤY DỮ LIỆU TAB ĐƠN BÁN HÀNG (ORDERS) ---
            var orderQuery = _context.SaleOrders
                .Include(so => so.Customer)
                .Include(so => so.SaleOrderDetails)
                .ThenInclude(d => d.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchOrder))
            {
                orderQuery = orderQuery.Where(o => 
                    o.Customer.Name.Contains(searchOrder) || 
                    o.SOID.ToString().Contains(searchOrder));
            }

            var orderEntities = await orderQuery
                .OrderByDescending(so => so.OrderDate)
                .ToListAsync();

            // Map in memory to avoid SQL APPLY (not supported by SQLite)
            var orders = orderEntities.Select(so => new SaleOrderListItem
            {
                SOID = so.SOID,
                CustomerName = so.Customer.Name,
                CustomerPhone = so.Customer.Phone ?? "",
                TotalAmount = so.TotalAmount / rate,
                OrderDate = so.OrderDate,
                Status = so.Status,
                ProductSummary = string.Join(", ", so.SaleOrderDetails.Select(d => d.Product.ProductName).Take(2))
            }).ToList();

            var ordersVM = new SalesOrdersViewModel { Orders = orders, CurrencySymbol = symbol };

            // --- 3. LẤY DỮ LIỆU TAB TỒN KHO (STOCK) ---
            var stockQuery = _context.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Include(i => i.ProductLocation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchStock))
            {
                stockQuery = stockQuery.Where(i => i.Product.ProductName.Contains(searchStock));
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                stockQuery = stockQuery.Where(i => i.Product.CategoryID == categoryId);
            }

            var stocks = await stockQuery
                .Select(i => new StockItemViewModel
                {
                    ProductID = i.ProductID,
                    ProductName = i.Product.ProductName,
                    CategoryName = i.Product.Category.CategoryName,
                    PhysicalStock = i.QuantityAvailable,
                    ReservedStock = 0,
                    LocationName = i.ProductLocation.LocationCode
                })
                .ToListAsync();

            var stockVM = new SalesStockViewModel 
            { 
                Stocks = stocks, 
                Categories = await _context.Categories.ToListAsync() 
            };

            // --- 4. LẤY DỮ LIỆU TAB ĐỔI TRẢ (RETURNS) ---
            var returnQuery = _context.ReturnOrders
                .Include(r => r.SaleOrder)
                .ThenInclude(so => so.Customer)
                .Include(r => r.SaleOrder.SaleOrderDetails)
                .ThenInclude(d => d.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchReturn))
            {
                returnQuery = returnQuery.Where(r => 
                    r.SaleOrder.Customer.Name.Contains(searchReturn) || 
                    r.SOID.ToString().Contains(searchReturn));
            }

            var returnEntities = await returnQuery
                .OrderByDescending(r => r.ReturnID)
                .ToListAsync();

            var returns = returnEntities.Select(r => new ReturnOrderViewModel
            {
                ReturnID = r.ReturnID,
                SaleOrderCode = $"SO-{r.SOID:D5}",
                CustomerName = r.SaleOrder.Customer.Name,
                ProductSummary = string.Join(", ", r.SaleOrder.SaleOrderDetails.Select(d => d.Product.ProductName).Take(2)),
                Settlement = r.Settlement ?? "N/A",
                Status = r.Status
            }).ToList();

            var recentSaleOrders = await _context.SaleOrders
                .Include(so => so.Customer)
                .OrderByDescending(so => so.OrderDate)
                .Take(50)
                .ToListAsync();

            var returnVM = new SalesReturnViewModel
            {
                Returns = returns,
                SaleOrders = recentSaleOrders
            };

            // --- 5. ĐÓNG GÓI VÀ TRẢ VỀ VIEW ---
            ViewBag.SearchOrderTerm = searchOrder;
            ViewBag.SearchStockTerm = searchStock;
            ViewBag.CategoryId = categoryId;
            ViewBag.SearchReturnTerm = searchReturn;

            var combinedModel = new SalesCombinedViewModel
            {
                CreateVM = createVM,
                OrdersVM = ordersVM,
                StockVM = stockVM,
                ReturnVM = returnVM
            };

            return View(combinedModel);
        }

        // =====================================================================
        // CÁC HÀM XỬ LÝ DỮ LIỆU (POST / AJAX)
        // =====================================================================

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

            order.TotalAmount /= rate;
            foreach (var detail in order.SaleOrderDetails)
            {
                detail.UnitPrice /= rate;
            }

            // Trả về PartialView hiển thị chi tiết (bạn giữ nguyên file _OrderDetailsPartial.cshtml là được)
            return PartialView("_OrderDetailsPartial", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReturn(CreateReturnRequest request)
        {
            if (request.SOID == 0) return RedirectToAction("Index", "Sales", null, "menu4");
            
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
            
            // Redirect về trang chủ của Sales và trỏ thẳng vào tab Đổi trả (#menu4)
            return RedirectToAction("Sales", "Sales", null, "menu4");
        }
    }
}