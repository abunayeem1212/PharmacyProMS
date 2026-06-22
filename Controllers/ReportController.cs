using PharmacyProMS.Data;
using PharmacyProMS.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;
using PharmacyProMS.Helpers;
using System.Collections.Generic;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class ReportController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // ─── SALES REPORT ─────────────────────────────────────
        public ActionResult Sales(
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            if (!dateFrom.HasValue)
                dateFrom = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month, 1);
            if (!dateTo.HasValue)
                dateTo = DateTime.Today;

            var invoices = db.SaleInvoices
                .Include("Customer")
                .Include("SaleItems")
                .Where(s => s.SaleDate >= dateFrom.Value &&
                    s.SaleDate <= dateTo.Value)
                .OrderByDescending(s => s.SaleDate)
                .ToList();

            // Load customer manually
            var customerIds = invoices
                .Where(s => s.CustomerId.HasValue)
                .Select(s => s.CustomerId.Value)
                .Distinct().ToList();
            var customers = db.Customers
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToList();
            foreach (var inv in invoices)
            {
                if (inv.CustomerId.HasValue)
                    inv.Customer = customers.FirstOrDefault(c =>
                        c.CustomerId == inv.CustomerId.Value);
            }

            ViewBag.DateFrom = dateFrom.Value
                .ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo.Value
                .ToString("yyyy-MM-dd");
            ViewBag.TotalSale = invoices
                .Sum(s => s.NetAmount);
            ViewBag.TotalVat = invoices
                .Sum(s => s.VatAmount);
            ViewBag.TotalDiscount = invoices
                .Sum(s => s.Discount);
            ViewBag.InvoiceCount = invoices.Count;

            return View(invoices);
        }

        // ─── STOCK REPORT ─────────────────────────────────────
        public ActionResult Stock(
            string filter = "all",
            int? companyId = null)
        {
            var query = db.Medicines
                .Include("Company")
                .Include("Category")
                .Where(m => m.IsActive)
                .AsQueryable();

            switch (filter)
            {
                case "low":
                    query = query.Where(m =>
                        m.CurrentStock <= m.ReOrderLevel &&
                        m.CurrentStock > 0);
                    break;
                case "out":
                    query = query.Where(m =>
                        m.CurrentStock == 0);
                    break;
                case "ok":
                    query = query.Where(m =>
                        m.CurrentStock > m.ReOrderLevel);
                    break;
            }

            if (companyId.HasValue)
                query = query.Where(m =>
                    m.CompanyId == companyId.Value);

            var medicines = query
                .OrderBy(m => m.CurrentStock)
                .ToList();

            // Manual navigation load
            var cIds = medicines.Select(m => m.CompanyId)
                .Distinct().ToList();
            var catIds = medicines.Select(m => m.CategoryId)
                .Distinct().ToList();
            var companies = db.Companies
                .Where(c => cIds.Contains(c.CompanyId))
                .ToList();
            var categories = db.MedicineCategories
                .Where(c => catIds.Contains(c.CategoryId))
                .ToList();
            foreach (var m in medicines)
            {
                m.Company = companies.FirstOrDefault(c =>
                    c.CompanyId == m.CompanyId);
                m.Category = categories.FirstOrDefault(c =>
                    c.CategoryId == m.CategoryId);
            }

            ViewBag.Filter = filter;
            ViewBag.Companies = db.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName).ToList();
            ViewBag.CompanyId = companyId;
            ViewBag.TotalStockValue = medicines
                .Sum(m => m.CurrentStock * m.SalePrice);

            return View(medicines);
        }

        // ─── EXPIRY REPORT ────────────────────────────────────
        public ActionResult Expiry(string filter = "expiring")
        {
            var today = DateTime.Today;
            var day30 = today.AddDays(30);
            var day90 = today.AddDays(90);

            var query = db.MedicineBatches
                .Include("Medicine")
                .Where(b => b.Quantity > 0)
                .AsQueryable();

            switch (filter)
            {
                case "expired":
                    query = query.Where(b =>
                        b.ExpiryDate < today);
                    break;
                case "30days":
                    query = query.Where(b =>
                        b.ExpiryDate >= today &&
                        b.ExpiryDate <= day30);
                    break;
                default: // expiring = 90 days
                    query = query.Where(b =>
                        b.ExpiryDate >= today &&
                        b.ExpiryDate <= day90);
                    break;
            }

            var batches = query
                .OrderBy(b => b.ExpiryDate)
                .ToList();

            // Manual load
            var medIds = batches
                .Select(b => b.MedicineId)
                .Distinct().ToList();
            var meds = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            foreach (var b in batches)
                b.Medicine = meds.FirstOrDefault(m =>
                    m.MedicineId == b.MedicineId);

            ViewBag.Filter = filter;
            ViewBag.Today = today;
            return View(batches);
        }

        // ─── PROFIT REPORT ────────────────────────────────────
        public ActionResult Profit(
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            if (!dateFrom.HasValue)
                dateFrom = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month, 1);
            if (!dateTo.HasValue)
                dateTo = DateTime.Today;

            var items = db.SaleInvoiceItems
                .Include("Medicine")
                .Include("SaleInvoice")
                .Where(si =>
                    si.SaleInvoice.SaleDate >= dateFrom &&
                    si.SaleInvoice.SaleDate <= dateTo)
                .ToList();

            // Manual navigation load
            var medIds = items.Select(i => i.MedicineId)
                .Distinct().ToList();
            var meds = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            foreach (var item in items)
                item.Medicine = meds.FirstOrDefault(m =>
                    m.MedicineId == item.MedicineId);

            var profitData = items
                .GroupBy(i => i.Medicine != null
                    ? i.Medicine.MedicineName : "Unknown")
                .Select(g => new ProfitReportViewModel
                {
                    MedicineName = g.Key,
                    TotalQty = g.Sum(x => x.Quantity),
                    TotalSale = g.Sum(x => x.SubTotal),
                    TotalCost = g.Sum(x =>
                        x.PurchasePrice * x.Quantity),
                    TotalProfit = g.Sum(x =>
                        (x.SalePrice - x.PurchasePrice)
                        * x.Quantity)
                })
                .OrderByDescending(x => x.TotalProfit)
                .ToList();

            ViewBag.DateFrom = dateFrom.Value
                .ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo.Value
                .ToString("yyyy-MM-dd");
            ViewBag.GrandSale = profitData.Sum(x => x.TotalSale);
            ViewBag.GrandCost = profitData.Sum(x => x.TotalCost);
            ViewBag.GrandProfit = profitData.Sum(x => x.TotalProfit);

            return View(profitData);
        }

        // ─── SALES EXCEL EXPORT ───────────────────────────────
        public ActionResult ExportSalesExcel(
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            if (!dateFrom.HasValue)
                dateFrom = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month, 1);
            if (!dateTo.HasValue)
                dateTo = DateTime.Today;

            var invoices = db.SaleInvoices
                .Include("Customer")
                .Where(s => s.SaleDate >= dateFrom.Value &&
                    s.SaleDate <= dateTo.Value)
                .OrderByDescending(s => s.SaleDate)
                .ToList();

            // Load customers manually
            var custIds = invoices
                .Where(s => s.CustomerId.HasValue)
                .Select(s => s.CustomerId.Value)
                .Distinct().ToList();
            var customers = db.Customers
                .Where(c => custIds.Contains(c.CustomerId))
                .ToList();
            foreach (var inv in invoices)
                if (inv.CustomerId.HasValue)
                    inv.Customer = customers.FirstOrDefault(
                        c => c.CustomerId == inv.CustomerId.Value);

            var headers = new List<string>
            {
                "SL", "Invoice No", "Date", "Customer",
                "Total", "Discount", "VAT", "Net Amount"
            };

            var rows = new List<List<string>>();
            int sl = 1;
            foreach (var inv in invoices)
            {
                rows.Add(new List<string>
                {
                    sl.ToString(),
                    inv.InvoiceNumber,
                    inv.SaleDate.ToString("dd MMM yyyy",
                        System.Globalization.CultureInfo.InvariantCulture),
                    inv.Customer != null
                        ? inv.Customer.CustomerName
                        : inv.WalkInCustomerName ?? "Walk-in",
                    string.Format("{0:N2}", inv.TotalAmount),
                    string.Format("{0:N2}", inv.Discount),
                    string.Format("{0:N2}", inv.VatAmount),
                    string.Format("{0:N2}", inv.NetAmount)
                });
                sl++;
            }

            string title = "Sales Report — "
                + dateFrom.Value.ToString("dd MMM yyyy",
                    System.Globalization.CultureInfo.InvariantCulture)
                + " to "
                + dateTo.Value.ToString("dd MMM yyyy",
                    System.Globalization.CultureInfo.InvariantCulture);

            byte[] fileBytes = ExcelHelper.GenerateExcel(
                "Sales Report", headers, rows, title);

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument" +
                ".spreadsheetml.sheet",
                "SalesReport_" +
                DateTime.Now.ToString("yyyyMMdd") + ".xlsx");
        }

        // ─── STOCK EXCEL EXPORT ───────────────────────────────
        public ActionResult ExportStockExcel(
            string filter = "all",
            int? companyId = null)
        {
            var query = db.Medicines
                .Include("Company")
                .Include("Category")
                .Where(m => m.IsActive)
                .AsQueryable();

            if (filter == "low")
                query = query.Where(m =>
                    m.CurrentStock <= m.ReOrderLevel &&
                    m.CurrentStock > 0);
            else if (filter == "out")
                query = query.Where(m => m.CurrentStock == 0);

            if (companyId.HasValue)
                query = query.Where(m =>
                    m.CompanyId == companyId.Value);

            var medicines = query.OrderBy(m => m.MedicineName)
                .ToList();

            // Manual load
            var cIds = medicines.Select(m => m.CompanyId)
                .Distinct().ToList();
            var catIds = medicines.Select(m => m.CategoryId)
                .Distinct().ToList();
            var comps = db.Companies
                .Where(c => cIds.Contains(c.CompanyId)).ToList();
            var cats = db.MedicineCategories
                .Where(c => catIds.Contains(c.CategoryId)).ToList();
            foreach (var m in medicines)
            {
                m.Company = comps.FirstOrDefault(c =>
                    c.CompanyId == m.CompanyId);
                m.Category = cats.FirstOrDefault(c =>
                    c.CategoryId == m.CategoryId);
            }

            var headers = new List<string>
            {
                "SL", "Medicine", "Generic", "Company",
                "Category", "Unit", "Sale Price",
                "Stock", "Re-Order", "Status", "Value"
            };

            var rows = new List<List<string>>();
            int sl = 1;
            foreach (var m in medicines)
            {
                string status = m.CurrentStock == 0
                    ? "Out of Stock"
                    : m.CurrentStock <= m.ReOrderLevel
                        ? "Low Stock" : "In Stock";

                rows.Add(new List<string>
                {
                    sl.ToString(),
                    m.MedicineName,
                    m.GenericName ?? "—",
                    m.Company != null ? m.Company.CompanyName : "—",
                    m.Category != null ? m.Category.CategoryName : "—",
                    m.UnitType ?? "—",
                    string.Format("{0:N2}", m.SalePrice),
                    m.CurrentStock.ToString(),
                    m.ReOrderLevel.ToString(),
                    status,
                    string.Format("{0:N2}",
                        m.CurrentStock * m.SalePrice)
                });
                sl++;
            }

            byte[] fileBytes = ExcelHelper.GenerateExcel(
                "Stock Report", headers, rows,
                "Stock Report — " + DateTime.Now.ToString(
                    "dd MMM yyyy",
                    System.Globalization.CultureInfo.InvariantCulture));

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument" +
                ".spreadsheetml.sheet",
                "StockReport_" +
                DateTime.Now.ToString("yyyyMMdd") + ".xlsx");
        }

        // ─── PROFIT EXCEL EXPORT ──────────────────────────────
        public ActionResult ExportProfitExcel(
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            if (!dateFrom.HasValue)
                dateFrom = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month, 1);
            if (!dateTo.HasValue)
                dateTo = DateTime.Today;

            var items = db.SaleInvoiceItems
                .Include("Medicine")
                .Include("SaleInvoice")
                .Where(si =>
                    si.SaleInvoice.SaleDate >= dateFrom &&
                    si.SaleInvoice.SaleDate <= dateTo)
                .ToList();

            var medIds = items.Select(i => i.MedicineId)
                .Distinct().ToList();
            var meds = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId)).ToList();
            foreach (var item in items)
                item.Medicine = meds.FirstOrDefault(m =>
                    m.MedicineId == item.MedicineId);

            var profitData = items
                .GroupBy(i => i.Medicine != null
                    ? i.Medicine.MedicineName : "Unknown")
                .Select(g => new ProfitReportViewModel
                {
                    MedicineName = g.Key,
                    TotalQty = g.Sum(x => x.Quantity),
                    TotalSale = g.Sum(x => x.SubTotal),
                    TotalCost = g.Sum(x =>
                        x.PurchasePrice * x.Quantity),
                    TotalProfit = g.Sum(x =>
                        (x.SalePrice - x.PurchasePrice)
                        * x.Quantity)
                })
                .OrderByDescending(x => x.TotalProfit)
                .ToList();

            var headers = new List<string>
            {
                "SL", "Medicine", "Total Qty",
                "Sale Amount", "Cost Amount",
                "Profit", "Margin %"
            };

            var rows = new List<List<string>>();
            int sl = 1;
            foreach (var row in profitData)
            {
                decimal margin = row.TotalSale > 0
                    ? Math.Round(
                        row.TotalProfit / row.TotalSale * 100, 1)
                    : 0;

                rows.Add(new List<string>
                {
                    sl.ToString(),
                    row.MedicineName,
                    row.TotalQty.ToString(),
                    string.Format("{0:N2}", row.TotalSale),
                    string.Format("{0:N2}", row.TotalCost),
                    string.Format("{0:N2}", row.TotalProfit),
                    margin + "%"
                });
                sl++;
            }

            byte[] fileBytes = ExcelHelper.GenerateExcel(
                "Profit Report", headers, rows,
                "Profit Report — "
                + dateFrom.Value.ToString("dd MMM yyyy",
                    System.Globalization.CultureInfo.InvariantCulture)
                + " to "
                + dateTo.Value.ToString("dd MMM yyyy",
                    System.Globalization.CultureInfo.InvariantCulture));

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument" +
                ".spreadsheetml.sheet",
                "ProfitReport_" +
                DateTime.Now.ToString("yyyyMMdd") + ".xlsx");
        }


        // ─── PURCHASE REPORT ──────────────────────────────────
        public ActionResult Purchase(
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int? filterSupplier = null)
        {
            if (!dateFrom.HasValue)
                dateFrom = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month, 1);
            if (!dateTo.HasValue)
                dateTo = DateTime.Today;

            var query = db.PurchaseInvoices
                .Include("Supplier")
                .Include("PurchaseItems")
                .Where(p => p.PurchaseDate >= dateFrom.Value &&
                    p.PurchaseDate <= dateTo.Value)
                .AsQueryable();

            if (filterSupplier.HasValue)
                query = query.Where(p =>
                    p.SupplierId == filterSupplier.Value);

            var invoices = query
                .OrderByDescending(p => p.PurchaseDate)
                .ToList();

            // Manual load Supplier
            var supIds = invoices.Select(p => p.SupplierId)
                .Distinct().ToList();
            var suppliers = db.Suppliers
                .Where(s => supIds.Contains(s.SupplierId)).ToList();
            foreach (var inv in invoices)
                inv.Supplier = suppliers.FirstOrDefault(s =>
                    s.SupplierId == inv.SupplierId);

            ViewBag.DateFrom = dateFrom.Value
                .ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo.Value
                .ToString("yyyy-MM-dd");
            ViewBag.TotalPurchase = invoices.Sum(p => p.NetAmount);
            ViewBag.TotalPaid = invoices.Sum(p => p.PaidAmount);
            ViewBag.TotalDue = invoices
                .Sum(p => p.NetAmount - p.PaidAmount);
            ViewBag.InvoiceCount = invoices.Count;
            ViewBag.FilterSupplier = filterSupplier;
            ViewBag.Suppliers = db.Suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.SupplierName).ToList();

            return View(invoices);
        }

        // ─── PURCHASE EXCEL EXPORT ────────────────────────────
        public ActionResult ExportPurchaseExcel(
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int? filterSupplier = null)
        {
            if (!dateFrom.HasValue)
                dateFrom = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month, 1);
            if (!dateTo.HasValue)
                dateTo = DateTime.Today;

            var query = db.PurchaseInvoices
                .Include("Supplier")
                .Where(p => p.PurchaseDate >= dateFrom.Value &&
                    p.PurchaseDate <= dateTo.Value)
                .AsQueryable();

            if (filterSupplier.HasValue)
                query = query.Where(p =>
                    p.SupplierId == filterSupplier.Value);

            var invoices = query
                .OrderByDescending(p => p.PurchaseDate)
                .ToList();

            var supIds = invoices.Select(p => p.SupplierId)
                .Distinct().ToList();
            var suppliers = db.Suppliers
                .Where(s => supIds.Contains(s.SupplierId)).ToList();
            foreach (var inv in invoices)
                inv.Supplier = suppliers.FirstOrDefault(s =>
                    s.SupplierId == inv.SupplierId);

            var headers = new List<string>
    {
        "SL", "Invoice No", "Date", "Supplier",
        "Total", "Discount", "Net Amount",
        "Paid", "Due"
    };

            var rows = new List<List<string>>();
            int i = 1;
            foreach (var inv in invoices)
            {
                decimal due = inv.NetAmount - inv.PaidAmount;
                rows.Add(new List<string>
        {
            i.ToString(),
            inv.InvoiceNumber,
            inv.PurchaseDate.ToString("dd MMM yyyy",
                System.Globalization.CultureInfo
                .InvariantCulture),
            inv.Supplier != null
                ? inv.Supplier.SupplierName : "—",
            string.Format("{0:N2}", inv.TotalAmount),
            string.Format("{0:N2}", inv.Discount),
            string.Format("{0:N2}", inv.NetAmount),
            string.Format("{0:N2}", inv.PaidAmount),
            string.Format("{0:N2}", due)
        });
                i++;
            }

            byte[] fileBytes = ExcelHelper.GenerateExcel(
                "Purchase Report", headers, rows,
                "Purchase Report — "
                + dateFrom.Value.ToString("dd MMM yyyy",
                    System.Globalization.CultureInfo.InvariantCulture)
                + " to "
                + dateTo.Value.ToString("dd MMM yyyy",
                    System.Globalization.CultureInfo.InvariantCulture));

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument" +
                ".spreadsheetml.sheet",
                "PurchaseReport_" +
                DateTime.Now.ToString("yyyyMMdd") + ".xlsx");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}