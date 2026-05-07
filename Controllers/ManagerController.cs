using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Quản lý, Quản trị viên")]
    public class ManagerController : Controller
    {
        private readonly SCMDbContext _context;

        public ManagerController(SCMDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string reportType = "summary", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null) settings = new SCM_System.Models.SystemSetting();

            string symbol = settings.Currency == "VND" ? "₫" : (settings.Currency == "USD" ? "$" : "€");

            var saleQuery = _context.SaleOrders.AsQueryable();
            var purchaseQuery = _context.PurchaseOrders.AsQueryable();
            var deliveryQuery = _context.Deliveries.AsQueryable();
            var returnQuery = _context.ReturnOrders.AsQueryable();

            if (fromDate.HasValue)
            {
                saleQuery = saleQuery.Where(o => o.OrderDate >= fromDate.Value);
                purchaseQuery = purchaseQuery.Where(o => o.OrderDate >= fromDate.Value);
                deliveryQuery = deliveryQuery.Where(d => d.DeliveryTime >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                saleQuery = saleQuery.Where(o => o.OrderDate <= toDate.Value);
                purchaseQuery = purchaseQuery.Where(o => o.OrderDate <= toDate.Value);
                deliveryQuery = deliveryQuery.Where(d => d.DeliveryTime <= toDate.Value);
            }

            var saleOrders = await saleQuery.ToListAsync();
            var purchaseOrders = await purchaseQuery.ToListAsync();
            var deliveries = await deliveryQuery.ToListAsync();
            var returns = await returnQuery.ToListAsync();

            decimal totalRevenue = saleOrders
                .Where(so => so.Status == "Hoàn thành" || so.Status == "Đã giao")
                .Sum(so => so.TotalAmount);

            decimal totalExpense = purchaseOrders
                .Where(po => po.Status == "Hoàn thành")
                .Sum(po => po.TotalAmount);

            int completedOrders = saleOrders.Count(so => so.Status == "Hoàn thành");

            double deliverySuccessRate = deliveries.Any() 
                ? (double)deliveries.Count(d => d.Status == "Thành công") / deliveries.Count * 100 
                : 100;

            double returnRate = saleOrders.Any() 
                ? (double)returns.Count / saleOrders.Count * 100 
                : 0;

            var chartLabels = new List<string>();
            var chartRevenue = new List<decimal>();
            var chartExpense = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var label = $"T{date.Month}/{date.Year % 100}";
                chartLabels.Add(label);

                var monthlyRev = saleOrders
                    .Where(so => so.OrderDate.Month == date.Month && so.OrderDate.Year == date.Year)
                    .Sum(so => so.TotalAmount);
                
                var monthlyExp = purchaseOrders
                    .Where(po => po.OrderDate.Month == date.Month && po.OrderDate.Year == date.Year)
                    .Sum(po => po.TotalAmount);

                chartRevenue.Add(monthlyRev);
                chartExpense.Add(monthlyExp);
            }

            decimal rate = 1;
            if (settings.Currency == "USD") rate = 25000;
            else if (settings.Currency == "EUR") rate = 27000;

            var chartRevConverted = chartRevenue.Select(v => v / rate).ToList();
            var chartExpConverted = chartExpense.Select(v => v / rate).ToList();

            string revStr, expStr;
            if (settings.Currency == "VND")
            {
                revStr = (totalRevenue / 1000000000).ToString("N2") + " tỷ";
                expStr = (totalExpense / 1000000000).ToString("N2") + " tỷ";
            }
            else
            {
                revStr = (totalRevenue / rate).ToString("N0");
                expStr = (totalExpense / rate).ToString("N0");
            }

            var reportVM = new ManagerReportViewModel
            {
                TotalRevenue = revStr,
                TotalExpense = expStr,
                CompletedOrdersCount = completedOrders,
                DeliverySuccessRate = deliverySuccessRate.ToString("N1") + "%",
                ReturnRate = returnRate.ToString("N1") + "%",
                CurrencySymbol = symbol,
                ChartLabels = chartLabels,
                ChartDataRevenue = chartRevConverted,
                ChartDataExpense = chartExpConverted,
                ReportType = reportType
            };

            return View(reportVM);
        }
    }
}
