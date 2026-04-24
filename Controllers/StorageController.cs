using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models;
using SCM_System.Models.ViewModels;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Quản trị viên,Quản lý kho")]
    public class StorageController : Controller
    {
        private readonly SCMDbContext _context;

        public StorageController(SCMDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Category(
            string? searchReceipt, string? searchPick, string? searchInv, 
            string? searchHandover, string? searchReturn, string? searchQC)
        {
            // Get currency settings
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSetting();
            decimal rate = 1;
            if (settings.Currency == "USD") rate = 25000;
            else if (settings.Currency == "EUR") rate = 27000;
            string symbol = settings.Currency == "VND" ? "₫" : (settings.Currency == "USD" ? "$" : "€");

            var viewModel = new StoragePageViewModel
            {
                CurrencySymbol = symbol
            };

            // 1. InboundOrders (Receipts)
            var receiptsQuery = _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Where(p => p.Status != "Hoàn thành")
                .OrderByDescending(p => p.OrderDate)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchReceipt))
            {
                receiptsQuery = receiptsQuery.Where(p => p.Supplier.SupplierName.Contains(searchReceipt) || p.POID.ToString().Contains(searchReceipt));
            }

            viewModel.InboundOrders = await receiptsQuery
                .Select(p => new InboundOrderViewModel
                {
                    POID = p.POID,
                    SupplierName = p.Supplier.SupplierName,
                    OrderDate = p.OrderDate,
                    Status = p.Status,
                    TotalAmount = p.TotalAmount / rate
                }).ToListAsync();

            // 2. Locations & Zones
            var locations = await _context.ProductLocations
                .Include(l => l.Inventories)
                .ToListAsync();

            viewModel.Locations = locations.Select(l => new LocationDetailViewModel
            {
                LocationID = l.LocationID,
                LocationCode = l.LocationCode,
                Description = l.Description ?? "",
                LocationType = l.LocationType,
                Capacity = l.Capacity,
                Used = l.Inventories.Sum(i => i.QuantityAvailable)
            }).ToList();

            viewModel.Zones = viewModel.Locations
                .GroupBy(l => l.LocationCode.Substring(0, 1))
                .Select(g => new LocationOccupancyViewModel
                {
                    ZoneName = $"Khu {g.Key}",
                    TotalCapacity = g.Sum(x => x.Capacity),
                    TotalUsed = g.Sum(x => x.Used)
                }).ToList();

            // 3. PickingOrders
            var pickQuery = _context.SaleOrders
                .Where(s => s.Status == "Đang xử lý")
                .Include(s => s.Customer)
                .Include(s => s.SaleOrderDetails).ThenInclude(d => d.Product).ThenInclude(p => p.Inventories).ThenInclude(i => i.ProductLocation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchPick))
            {
                pickQuery = pickQuery.Where(s => s.Customer.Name.Contains(searchPick) || s.SOID.ToString().Contains(searchPick));
            }

            viewModel.PickingOrders = await pickQuery
                .Select(s => new PickingOrderViewModel
                {
                    SOID = s.SOID,
                    CustomerName = s.Customer.Name,
                    Items = s.SaleOrderDetails.Select(d => new PickingItemViewModel
                    {
                        ProductID = d.ProductID,
                        ProductName = d.Product.ProductName,
                        Quantity = d.Quantity,
                        SuggestedLocationCode = d.Product.Inventories
                            .OrderByDescending(i => i.QuantityAvailable)
                            .Select(i => i.ProductLocation.LocationCode)
                            .FirstOrDefault() ?? "N/A",
                        StockAtLocation = d.Product.Inventories
                            .OrderByDescending(i => i.QuantityAvailable)
                            .Select(i => i.QuantityAvailable)
                            .FirstOrDefault()
                    }).ToList()
                }).ToListAsync();

            // 4. InventoryItems
            var invQuery = _context.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Include(i => i.ProductLocation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchInv))
            {
                invQuery = invQuery.Where(i => i.Product.ProductName.Contains(searchInv) || i.ProductLocation.LocationCode.Contains(searchInv));
            }

            viewModel.InventoryItems = await invQuery
                .Select(i => new InventoryItemViewModel
                {
                    ProductID = i.ProductID,
                    LocationID = i.LocationID,
                    ProductName = i.Product.ProductName,
                    CategoryName = i.Product.Category.CategoryName,
                    LocationCode = i.ProductLocation.LocationCode,
                    QuantityAvailable = i.QuantityAvailable
                }).ToListAsync();

            // 5. HandoverOrders
            var handoverQuery = _context.SaleOrders
                .Where(s => s.Status == "Đã soạn xong")
                .Include(s => s.Customer)
                .Include(s => s.SaleOrderDetails)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchHandover))
            {
                handoverQuery = handoverQuery.Where(s => s.Customer.Name.Contains(searchHandover) || s.SOID.ToString().Contains(searchHandover));
            }

            viewModel.HandoverOrders = await handoverQuery
                .Select(s => new HandoverOrderViewModel
                {
                    SOID = s.SOID,
                    CustomerName = s.Customer.Name,
                    TotalItems = s.SaleOrderDetails.Count,
                    ShipperName = "Võ Giao Hàng",
                    Status = s.Status
                }).ToListAsync();

            // 6. Returns
            var returnQuery = _context.ReturnOrders
                .Include(r => r.SaleOrder).ThenInclude(so => so.Customer)
                .OrderByDescending(r => r.ReturnID)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchReturn))
            {
                returnQuery = returnQuery.Where(r => (r.SaleOrder.Customer.Name != null && r.SaleOrder.Customer.Name.Contains(searchReturn)) || r.SOID.ToString().Contains(searchReturn));
            }

            viewModel.Returns = await returnQuery
                .Select(r => new StorageReturnViewModel
                {
                    ReturnID = r.ReturnID,
                    SOID = r.SOID,
                    CustomerName = r.SaleOrder.Customer.Name ?? "N/A",
                    ProductName = "Hàng đổi trả",
                    Reason = r.Reason ?? "",
                    Status = r.Status ?? "Mới"
                }).ToListAsync();

            // 7. QC Records
            var qcQuery = _context.QualityControls
                .Include(q => q.Product)
                .Include(q => q.User)
                .OrderByDescending(q => q.InspectionDate)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQC))
            {
                qcQuery = qcQuery.Where(q => (q.Product != null && q.Product.ProductName.Contains(searchQC)) || (q.ReferenceID != null && q.ReferenceID.Contains(searchQC)));
            }

            viewModel.QCRecords = await qcQuery
                .Select(q => new QCRecordViewModel
                {
                    QCID = q.QCID,
                    Type = (q.ReferenceID != null && q.ReferenceID.StartsWith("PO")) ? "Nhập kho" : "Hàng hoàn",
                    ReferenceID = q.ReferenceID ?? "N/A",
                    ProductName = q.Product != null ? q.Product.ProductName : "N/A",
                    Result = q.Result ?? "N/A",
                    InspectorName = q.User != null ? q.User.FullName : "Hệ thống",
                    CheckDate = q.InspectionDate,
                    Notes = q.Notes ?? ""
                }).ToListAsync();

            viewModel.AllProducts = await _context.Products.OrderBy(p => p.ProductName).ToListAsync();
            viewModel.PendingExports = await _context.Deliveries
            .Include(d => d.SaleOrder).ThenInclude(so => so.Customer)
            .Where(d => d.Status == "Chờ lấy hàng")
            .Select(d => new StorageExportItem
            {
                DeliveryID = d.DeliveryID,
                OrderCode = "SO-" + d.SaleOrder.OrderDate.Year + "-" + d.SOID.ToString("D3"),
                CustomerName = d.SaleOrder.Customer.Name ?? "Khách hàng"
            }).ToListAsync();


            ViewBag.SearchReceipt = searchReceipt;
            ViewBag.SearchPick = searchPick;
            ViewBag.SearchInv = searchInv;
            ViewBag.SearchHandover = searchHandover;
            ViewBag.SearchReturn = searchReturn;
            ViewBag.SearchQC = searchQC;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessReceipt(int poid)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderDetails)
                .FirstOrDefaultAsync(p => p.POID == poid);

            if (po == null) return NotFound();
            if (po.Status == "Hoàn thành") return BadRequest("Đơn hàng đã được xử lý nhập kho trước đó.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in po.PurchaseOrderDetails)
                {
                    // Find a location for this product, or default to A1 (LocationID 1)
                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.ProductID == item.ProductID);

                    if (inventory != null)
                    {
                        inventory.QuantityAvailable += item.Quantity;
                    }
                    else
                    {
                        // Create new inventory entry in A1
                        _context.Inventories.Add(new Inventory
                        {
                            ProductID = item.ProductID,
                            LocationID = 1, // Default A1
                            QuantityAvailable = item.Quantity
                        });
                    }
                }

                po.Status = "Hoàn thành";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Đã nhập kho thành công đơn hàng PO-{poid:D5}.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi nhập kho: " + ex.Message;
            }

            return RedirectToAction("Category", new { hash = "#menu1" });
        }

        [HttpPost]
        public async Task<IActionResult> AddLocation(string code, string desc, int capacity, string type)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest("Mã vị trí không được để trống.");
            var loc = new ProductLocation { LocationCode = code, Description = desc, Capacity = capacity, LocationType = type };
            _context.ProductLocations.Add(loc);
            await _context.SaveChangesAsync();
            return RedirectToAction("Category", new { hash = "#menu2" });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsPicked(int soid)
        {
            var so = await _context.SaleOrders.FindAsync(soid);
            if (so == null) return NotFound();
            if (so.Status != "Đang xử lý") return BadRequest("Đơn hàng không ở trạng thái chờ soạn.");

            so.Status = "Đã soạn xong";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã hoàn thành soạn hàng." });
        }

        [HttpPost]
        public async Task<IActionResult> HandoverSO(int soid)
        {
            var so = await _context.SaleOrders
                .Include(s => s.SaleOrderDetails)
                .FirstOrDefaultAsync(s => s.SOID == soid);

            if (so == null) return NotFound();
            
            if (so.Status != "Đã soạn xong") 
            {
                TempData["ErrorMessage"] = "Đơn hàng chưa được soạn xong, không thể bàn giao.";
                return RedirectToAction("Category", new { hash = "#menu5" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Deduct Inventory (The physical handover point)
                foreach (var item in so.SaleOrderDetails)
                {
                    // Find location that has the product (Simplified: subtract from first location found)
                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.ProductID == item.ProductID && i.QuantityAvailable >= item.Quantity);
                    
                    if (inventory == null)
                    {
                        // Fallback if no single location has enough, or just take from first available
                        inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == item.ProductID);
                    }

                    if (inventory != null)
                    {
                        inventory.QuantityAvailable -= item.Quantity;
                        if (inventory.QuantityAvailable < 0) inventory.QuantityAvailable = 0;
                        _context.Update(inventory);
                    }
                }

                // 2. Create Delivery Record
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var delivery = new Delivery
                {
                    SOID = so.SOID,
                    UserID = int.Parse(userIdStr ?? "0"),
                    Status = "Đang giao hàng",
                    DeliveryTime = DateTime.Now
                };
                _context.Deliveries.Add(delivery);

                // 3. Update SaleOrder Status
                so.Status = "Đang giao hàng";
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Đã khấu trừ tồn kho và bàn giao đơn hàng SO-{soid:D5} thành công.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi bàn giao: " + ex.Message;
            }

            return RedirectToAction("Category", new { hash = "#menu5" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInventory(int productId, int locationId, int quantity)
        {
            var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == productId && i.LocationID == locationId);
            if (inv == null) return NotFound();

            inv.QuantityAvailable = quantity;
            _context.Update(inv);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật tồn kho thành công.";
            return RedirectToAction("Category", new { hash = "#menu4" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQCRecord(int productId, string referenceId, string result, string? notes)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var qc = new QualityControl
            {
                ProductID = productId,
                UserID = int.Parse(userIdStr ?? "0"),
                ReferenceID = referenceId,
                Result = result,
                Notes = notes,
                InspectionDate = DateTime.Now
            };

            _context.QualityControls.Add(qc);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã tạo phiếu kiểm tra chất lượng mới.";
            return RedirectToAction("Category", new { hash = "#menu7" });
        }
    }
}