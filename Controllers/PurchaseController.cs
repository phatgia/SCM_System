using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models;
using SCM_System.Models.ViewModels;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Quản trị viên,Nhân viên mua hàng")]
    public class PurchaseController : Controller
    {
        private readonly SCMDbContext _context;

        public PurchaseController(SCMDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // GET: /Purchase/Supplier
        // =====================================================================
        public async Task<IActionResult> Supplier(string? searchSupplier, string? searchPO, string? searchReturn)
        {
            // 1. Fetch Suppliers
            var supplierQuery = _context.Suppliers
                .Include(s => s.PurchaseOrders)
                .AsQueryable();
 
            if (!string.IsNullOrWhiteSpace(searchSupplier))
                supplierQuery = supplierQuery.Where(s =>
                    s.SupplierName.Contains(searchSupplier) ||
                    (s.ContactPerson != null && s.ContactPerson.Contains(searchSupplier)) ||
                    (s.Phone != null && s.Phone.Contains(searchSupplier)));
 
            var suppliers = await supplierQuery
                .Select(s => new SupplierViewModel
                {
                    SupplierID = s.SupplierID,
                    SupplierName = s.SupplierName,
                    ContactPerson = s.ContactPerson,
                    Phone = s.Phone,
                    Email = s.Email,
                    Address = s.Address,
                    TotalOrders = s.PurchaseOrders.Count,
                    TotalPurchased = s.PurchaseOrders.Sum(po => po.TotalAmount)
                })
                .OrderBy(s => s.SupplierName)
                .ToListAsync();
 
            // 2. Fetch Purchase Orders
            var poQuery = _context.PurchaseOrders
                .Include(p => p.Supplier)
                .AsQueryable();
 
            if (!string.IsNullOrWhiteSpace(searchPO))
                poQuery = poQuery.Where(p => 
                    p.Supplier.SupplierName.Contains(searchPO) || 
                    p.POID.ToString().Contains(searchPO));
 
            var pos = await poQuery
                .OrderByDescending(p => p.POID)
                .Select(p => new PurchaseOrderViewModel
                {
                    POID = p.POID,
                    SupplierName = p.Supplier.SupplierName,
                    OrderDate = p.OrderDate,
                    ExpectedDeliveryDate = p.ExpectedDeliveryDate,
                    TotalAmount = p.TotalAmount,
                    Status = p.Status
                })
                .ToListAsync();
 
            // 3. Fetch Returns
            var returnQuery = _context.PurchaseReturns
                .Include(r => r.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
                .Include(r => r.PurchaseOrder.PurchaseOrderDetails)
                .ThenInclude(pod => pod.Product)
                .Include(r => r.User)
                .AsQueryable();
 
            if (!string.IsNullOrWhiteSpace(searchReturn))
                returnQuery = returnQuery.Where(r => 
                    r.PurchaseOrder.Supplier.SupplierName.Contains(searchReturn) || 
                    r.PurchaseOrder.POID.ToString().Contains(searchReturn) ||
                    (r.Reason != null && r.Reason.Contains(searchReturn)));
 
            var returnEntities = await returnQuery
                .OrderByDescending(r => r.ReturnDate)
                .ToListAsync();

            var returns = returnEntities.Select(r => new PurchaseReturnViewModel
                {
                    ReturnID = r.PurchaseReturnID,
                    SupplierName = r.PurchaseOrder.Supplier.SupplierName,
                    StaffName = r.User.FullName,
                    ProductSummary = string.Join(", ", r.PurchaseOrder.PurchaseOrderDetails.Select(d => d.Product.ProductName).Take(2)),
                    Reason = r.Reason,
                    Amount = r.Amount,
                    Status = r.Status,
                    Date = r.ReturnDate,
                    POID = r.POID
                }).ToList();
 
            // 4. Fetch Products for dropdown
            var products = await _context.Products.ToListAsync();
 
            // 5. Get Currency Symbol and Rate
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSetting();
            decimal rate = 1;
            if (settings.Currency == "USD") rate = 25000;
            else if (settings.Currency == "EUR") rate = 27000;
            string symbol = settings.Currency == "VND" ? "₫" : (settings.Currency == "USD" ? "$" : "€");
 
            // Apply rate to list data
            foreach (var s in suppliers) s.TotalPurchased /= rate;
            foreach (var po in pos) po.TotalAmount /= rate;
            foreach (var r in returns) r.Amount /= rate;
 
            var vm = new SupplierPageViewModel
            {
                Suppliers = suppliers,
                PurchaseOrders = pos,
                ReturnOrders = returns,
                AvailableProducts = products,
                SearchSupplier = searchSupplier,
                SearchPO = searchPO,
                SearchReturn = searchReturn,
                CurrencySymbol = symbol
            };
 
            return View(vm);
        }

        // =====================================================================
        // POST: /Purchase/AddSupplier
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSupplier(SupplierFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("Supplier");
            }

            var supplier = new Supplier
            {
                SupplierName = form.SupplierName,
                ContactPerson = form.ContactPerson,
                Phone = form.Phone,
                Email = form.Email,
                Address = form.Address
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm nhà cung cấp thành công!";
            return RedirectToAction("Supplier");
        }

        // =====================================================================
        // POST: /Purchase/EditSupplier
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(SupplierFormViewModel form)
        {
            var supplier = await _context.Suppliers.FindAsync(form.SupplierID);
            if (supplier == null) return RedirectToAction("Supplier");

            supplier.SupplierName = form.SupplierName;
            supplier.ContactPerson = form.ContactPerson;
            supplier.Phone = form.Phone;
            supplier.Email = form.Email;
            supplier.Address = form.Address;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật nhà cung cấp thành công!";
            return RedirectToAction("Supplier");
        }

        // =====================================================================
        // POST: /Purchase/CreatePurchaseOrder
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePOViewModel model)
        {
            if (model == null || model.SupplierID == 0 || !model.Items.Any())
                return BadRequest("Dữ liệu đơn hàng không hợp lệ.");

            // Get current UserID from claims
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var po = new PurchaseOrder
                {
                    SupplierID = model.SupplierID,
                    UserID = userId,
                    OrderDate = DateTime.Now,
                    ExpectedDeliveryDate = model.ExpectedDeliveryDate,
                    Status = "Đã duyệt", // Default status for simplicity
                    TotalAmount = model.Items.Sum(i => i.Quantity * i.Price)
                };

                _context.PurchaseOrders.Add(po);
                await _context.SaveChangesAsync();

                foreach (var item in model.Items)
                {
                    var detail = new PurchaseOrderDetail
                    {
                        POID = po.POID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    };
                    _context.PurchaseOrderDetails.Add(detail);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Tạo đơn nhập hàng thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Lỗi khi lưu đơn hàng: " + ex.Message);
            }
        }

        // =====================================================================
        // POST: /Purchase/CreateReturnOrder
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReturnOrder(int POID, string Reason)
        {
            var po = await _context.PurchaseOrders.FindAsync(POID);
            if (po == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn nhập liên quan.";
                return RedirectToAction("Supplier");
            }

            // Get current UserID from claims
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            var pr = new PurchaseReturn
            {
                POID = POID,
                UserID = userId,
                Reason = Reason,
                Status = "Chờ duyệt",
                Amount = po.TotalAmount, // Giả sử hoàn tiền toàn bộ
                ReturnDate = DateTime.Now
            };

            _context.PurchaseReturns.Add(pr);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gửi yêu cầu trả hàng thành công!";
            return RedirectToAction("Supplier");
        }

        // =====================================================================
        // GET: /Purchase/PODetails/{id}
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> PODetails(int id)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PurchaseOrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.POID == id);

            if (po == null) return NotFound();

            // Get Currency Symbol and Rate for display
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSetting();
            decimal rate = 1;
            if (settings.Currency == "USD") rate = 25000;
            else if (settings.Currency == "EUR") rate = 27000;
            ViewBag.CurrencySymbol = settings.Currency == "VND" ? "₫" : (settings.Currency == "USD" ? "$" : "€");

            // Apply rate to display values
            po.TotalAmount /= rate;
            foreach (var detail in po.PurchaseOrderDetails)
            {
                detail.UnitPrice /= rate;
            }

            return PartialView("_PODetailsPartial", po);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers.Include(s => s.PurchaseOrders).FirstOrDefaultAsync(s => s.SupplierID == id);
            if (supplier == null) return RedirectToAction("Supplier");
            if (supplier.PurchaseOrders.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp đã có đơn hàng.";
                return RedirectToAction("Supplier");
            }
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa nhà cung cấp.";
            return RedirectToAction("Supplier");
        }

        // =====================================================================
        // GET: /Purchase/ReturnDetails/{id}
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> ReturnDetails(int id)
        {
            var ret = await _context.PurchaseReturns
                .Include(r => r.PurchaseOrder).ThenInclude(p => p.Supplier)
                .Include(r => r.PurchaseOrder).ThenInclude(p => p.PurchaseOrderDetails).ThenInclude(d => d.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.PurchaseReturnID == id);

            if (ret == null) return NotFound();

            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSetting();
            decimal rate = 1;
            if (settings.Currency == "USD") rate = 25000;
            else if (settings.Currency == "EUR") rate = 27000;
            ViewBag.CurrencySymbol = settings.Currency == "VND" ? "₫" : (settings.Currency == "USD" ? "$" : "€");

            ret.Amount /= rate;
            foreach (var detail in ret.PurchaseOrder.PurchaseOrderDetails)
            {
                detail.UnitPrice /= rate;
            }

            return PartialView("_ReturnDetailsPartial", ret);
        }
    }
}