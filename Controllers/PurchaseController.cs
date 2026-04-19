using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models;
using SCM_System.Models.ViewModels;

namespace SCM_System.Controllers
{
    [Authorize]
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
        public async Task<IActionResult> Supplier(string? search)
        {
            var query = _context.Suppliers
                .Include(s => s.PurchaseOrders)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s =>
                    s.SupplierName.Contains(search) ||
                    (s.ContactPerson != null && s.ContactPerson.Contains(search)) ||
                    (s.Phone != null && s.Phone.Contains(search)));

            var suppliers = await query
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

            var vm = new SupplierPageViewModel
            {
                Suppliers = suppliers,
                SearchTerm = search
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
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
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

            TempData["SuccessMessage"] = $"Đã thêm nhà cung cấp \"{form.SupplierName}\" thành công!";
            return RedirectToAction("Supplier");
        }

        // =====================================================================
        // POST: /Purchase/EditSupplier
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(SupplierFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Supplier");
            }

            var supplier = await _context.Suppliers.FindAsync(form.SupplierID);
            if (supplier == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp.";
                return RedirectToAction("Supplier");
            }

            supplier.SupplierName = form.SupplierName;
            supplier.ContactPerson = form.ContactPerson;
            supplier.Phone = form.Phone;
            supplier.Email = form.Email;
            supplier.Address = form.Address;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật nhà cung cấp \"{form.SupplierName}\" thành công!";
            return RedirectToAction("Supplier");
        }

        // =====================================================================
        // POST: /Purchase/DeleteSupplier
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.PurchaseOrders)
                .FirstOrDefaultAsync(s => s.SupplierID == id);

            if (supplier == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp.";
                return RedirectToAction("Supplier");
            }

            if (supplier.PurchaseOrders.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa \"{supplier.SupplierName}\" vì đã có đơn nhập hàng liên quan.";
                return RedirectToAction("Supplier");
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa nhà cung cấp \"{supplier.SupplierName}\".";
            return RedirectToAction("Supplier");
        }
    }
}