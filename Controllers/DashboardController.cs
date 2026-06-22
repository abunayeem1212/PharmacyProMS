using PharmacyProMS.Data;
using PharmacyProMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(
                today.Year, today.Month, 1);

            // ── Today's Sales ──────────────────────────────
            var todaySales = db.SaleInvoices
                .Where(s => s.SaleDate == today)
                .ToList();

            decimal todaySaleAmt = todaySales
                .Sum(s => s.NetAmount);
            int todaySaleCount = todaySales.Count;

            // ── Today's Purchase ───────────────────────────
            decimal todayPurchase = db.PurchaseInvoices
                .Where(p => p.PurchaseDate == today)
                .Sum(p => (decimal?)p.NetAmount) ?? 0;

            // ── Today's Profit ─────────────────────────────
            var todaySaleItems = db.SaleInvoiceItems
                .Where(si => si.SaleInvoice.SaleDate == today)
                .ToList();

            decimal todayProfit = todaySaleItems
                .Sum(si => (si.SalePrice - si.PurchasePrice)
                    * si.Quantity);



            /////////////
            // Supplier Due
            decimal supplierDue = 0;
            var purInvoices = db.PurchaseInvoices.ToList();
            foreach (var inv in purInvoices)
            {
                decimal due = inv.NetAmount - inv.PaidAmount;
                if (due > 0) supplierDue += due;
            }
            ViewBag.SupplierDue = supplierDue;




            // Dashboard Index এ add করো
            decimal totalCustomerDue = db.SaleInvoices
                .Where(s => s.IsCreditSale && s.DueAmount > 0)
                .Sum(s => (decimal?)s.DueAmount) ?? 0;

            ViewBag.TotalCustomerDue = totalCustomerDue;

            // ── Overall ────────────────────────────────────
            int totalMedicines = db.Medicines
                .Count(m => m.IsActive);
            int totalCustomers = db.Customers
                .Count(c => c.IsActive);
            int totalSuppliers = db.Suppliers
                .Count(s => s.IsActive);
            int lowStockCount = db.Medicines
                .Count(m => m.IsActive &&
                    m.CurrentStock <= m.ReOrderLevel &&
                    m.CurrentStock > 0);
            int outOfStockCount = db.Medicines
                .Count(m => m.IsActive &&
                    m.CurrentStock == 0);

            // ── Expiry Alerts ──────────────────────────────
            // ✅ এভাবে করো — আগেই calculate করে রাখো
            
            DateTime expiry30 = today.AddDays(30);
            DateTime expiry90 = today.AddDays(90);

            int expiringCount = db.MedicineBatches
    .Count(b => b.ExpiryDate <= expiry90 &&
        b.ExpiryDate >= today &&
        b.Quantity > 0);

            int expiredCount = db.MedicineBatches
                .Count(b => b.ExpiryDate < today &&
                    b.Quantity > 0);

            // ── Monthly Sales (Last 6 Months) ──────────────
            var monthlySales = new List<MonthlyData>();
            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var start = new DateTime(
                    month.Year, month.Month, 1);
                var end = start.AddMonths(1)
                    .AddDays(-1);

                decimal amt = db.SaleInvoices
                    .Where(s => s.SaleDate >= start &&
                        s.SaleDate <= end)
                    .Sum(s => (decimal?)s.NetAmount) ?? 0;

                monthlySales.Add(new MonthlyData
                {
                    Month = month.ToString("MMM yy"),
                    Amount = amt
                });
            }

            // ── Monthly Purchases (Last 6 Months) ─────────
            var monthlyPurchases = new List<MonthlyData>();
            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var start = new DateTime(
                    month.Year, month.Month, 1);
                var end = start.AddMonths(1).AddDays(-1);

                decimal amt = db.PurchaseInvoices
                    .Where(p => p.PurchaseDate >= start &&
                        p.PurchaseDate <= end)
                    .Sum(p => (decimal?)p.NetAmount) ?? 0;

                monthlyPurchases.Add(new MonthlyData
                {
                    Month = month.ToString("MMM yy"),
                    Amount = amt
                });
            }

            // ── Top 5 Medicines ────────────────────────────
            var topMedicines = db.SaleInvoiceItems
                .Where(si => si.SaleInvoice.SaleDate >= thisMonth)
                .GroupBy(si => si.Medicine.MedicineName)
                .Select(g => new TopMedicineData
                {
                    MedicineName = g.Key,
                    TotalQty = g.Sum(x => x.Quantity),
                    TotalAmount = g.Sum(x => x.SubTotal)
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(5)
                .ToList();

            // ── Recent Sales ───────────────────────────────
            var recentSales = db.SaleInvoices
                .OrderByDescending(s => s.CreatedAt)
                .Take(8)
                .ToList()
                .Select(s => new RecentInvoiceData
                {
                    InvoiceNumber = s.InvoiceNumber,
                    CustomerName = s.WalkInCustomerName
                                    ?? "Walk-in",
                    NetAmount = s.NetAmount,
                    SaleDate = s.SaleDate
                })
                .ToList();

            // ── Expiring Batches ───────────────────────────
            // ✅ expiry90 variable use করো
                        var expiring = db.MedicineBatches
                .Include("Medicine")
                .Where(b => b.ExpiryDate <= expiry90 &&
                    b.ExpiryDate >= today &&
                    b.Quantity > 0)
                .OrderBy(b => b.ExpiryDate)
                .Take(5)
                .ToList()
                .Select(b => new ExpiryAlertData
                {
                    MedicineName = b.Medicine != null
                        ? b.Medicine.MedicineName : "—",
                    BatchNumber = b.BatchNumber,
                    ExpiryDate = b.ExpiryDate,
                    Quantity = b.Quantity,
                    DaysLeft = (b.ExpiryDate - today).Days
                })
                .ToList();

            // ── Low Stock ──────────────────────────────────
            var lowStock = db.Medicines
                .Include("Company")
                .Where(m => m.IsActive &&
                    m.CurrentStock <= m.ReOrderLevel)
                .OrderBy(m => m.CurrentStock)
                .Take(5)
                .ToList()
                .Select(m => new LowStockData
                {
                    MedicineName = m.MedicineName,
                    CurrentStock = m.CurrentStock,
                    ReOrderLevel = m.ReOrderLevel,
                    CompanyName = m.Company != null
                        ? m.Company.CompanyName : "—"
                })
                .ToList();

            var model = new DashboardViewModel
            {
                TodaySaleAmount = todaySaleAmt,
                TodaySaleCount = todaySaleCount,
                TodayPurchaseAmount = todayPurchase,
                TodayProfit = todayProfit,
                TotalMedicines = totalMedicines,
                TotalCustomers = totalCustomers,
                TotalSuppliers = totalSuppliers,
                LowStockCount = lowStockCount,
                OutOfStockCount = outOfStockCount,
                ExpiringCount = expiringCount,
                ExpiredCount = expiredCount,
                MonthlySales = monthlySales,
                MonthlyPurchases = monthlyPurchases,
                TopMedicines = topMedicines,
                RecentSales = recentSales,
                ExpiringBatches = expiring,
                LowStockMedicines = lowStock
            };

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}