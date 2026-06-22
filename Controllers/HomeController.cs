using PharmacyProMS.Data;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ApplicationDbContext db
            = new ApplicationDbContext();

        public ActionResult Index()
        {
            var today = DateTime.Today;
            var setting = db.PharmacySettings
                             .FirstOrDefault();

            // Today Stats
            decimal todaySale = db.SaleInvoices
                .Where(s => s.SaleDate == today)
                .Sum(s => (decimal?)s.NetAmount) ?? 0;

            int todaySaleCount = db.SaleInvoices
                .Count(s => s.SaleDate == today);

            decimal todayPurchase = db.PurchaseInvoices
                .Where(p => p.PurchaseDate == today)
                .Sum(p => (decimal?)p.NetAmount) ?? 0;

            int totalMedicines = db.Medicines
                .Count(m => m.IsActive);

            int lowStock = db.Medicines
                .Count(m => m.IsActive &&
                    m.CurrentStock <= m.ReOrderLevel &&
                    m.CurrentStock > 0);

            int outOfStock = db.Medicines
                .Count(m => m.IsActive &&
                    m.CurrentStock == 0);

            int totalCustomers = db.Customers
                .Count(c => c.IsActive);

            // Recent 5 Sales
            var recentSales = db.SaleInvoices
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToList();

            // Monthly Sale (last 6 months)
            decimal monthSale = db.SaleInvoices
                .Where(s => s.SaleDate.Month == today.Month
                    && s.SaleDate.Year == today.Year)
                .Sum(s => (decimal?)s.NetAmount) ?? 0;

            ViewBag.PharmacyName = setting != null
                ? setting.PharmacyName : "PharmacyPro MS";
            ViewBag.LogoPath = setting != null
                ? setting.LogoPath : null;
            ViewBag.Address = setting != null
                ? setting.Address : "";
            ViewBag.Phone = setting != null
                ? setting.Phone : "";
            ViewBag.TodaySale = todaySale;
            ViewBag.TodaySaleCount = todaySaleCount;
            ViewBag.TodayPurchase = todayPurchase;
            ViewBag.TotalMedicines = totalMedicines;
            ViewBag.LowStock = lowStock;
            ViewBag.OutOfStock = outOfStock;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.MonthSale = monthSale;
            ViewBag.RecentSales = recentSales;
            ViewBag.Today = today.ToString(
                "dddd, dd MMMM yyyy",
                System.Globalization.CultureInfo
                .InvariantCulture);

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}