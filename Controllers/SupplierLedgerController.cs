using PharmacyProMS.Data;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class SupplierLedgerController : Controller
    {
        private ApplicationDbContext db
            = new ApplicationDbContext();

        // ─── INDEX ────────────────────────────────────────
        public ActionResult Index()
        {
            var suppliers = db.Suppliers
                .Where(s => s.IsActive)
                .ToList();

            // Company load
            var compIds = suppliers
                .Select(s => s.CompanyId)
                .Distinct().ToList();
            var companies = db.Companies
                .Where(c => compIds.Contains(c.CompanyId))
                .ToList();

            var result = new System.Collections
                .Generic.List<SupplierDueSummary>();

            foreach (var s in suppliers)
            {
                decimal totalPurchase = db.PurchaseInvoices
                    .Where(p => p.SupplierId == s.SupplierId)
                    .Sum(p => (decimal?)p.NetAmount) ?? 0;

                decimal totalPaid = db.PurchaseInvoices
                    .Where(p => p.SupplierId == s.SupplierId)
                    .Sum(p => (decimal?)p.PaidAmount) ?? 0;

                // Extra payments
                decimal extraPaid = db.SupplierPayments
                    .Where(p => p.SupplierId == s.SupplierId)
                    .Sum(p => (decimal?)p.Amount) ?? 0;

                decimal totalDue = totalPurchase
                    - totalPaid - extraPaid;
                if (totalDue < 0) totalDue = 0;

                var comp = companies.FirstOrDefault(c =>
                    c.CompanyId == s.CompanyId);

                result.Add(new SupplierDueSummary
                {
                    SupplierId = s.SupplierId,
                    SupplierName = s.SupplierName,
                    Phone = s.Phone,
                    CompanyName = comp != null
                        ? comp.CompanyName : "—",
                    TotalPurchase = totalPurchase,
                    TotalPaid = totalPaid + extraPaid,
                    TotalDue = totalDue
                });
            }

            var model = new SupplierLedgerIndexViewModel
            {
                Suppliers = result,
                GrandDue = result.Sum(r => r.TotalDue),
                DueCount = result.Count(r => r.TotalDue > 0),
                TotalCount = result.Count
            };

            return View(model);
        }

        // ─── DETAILS ──────────────────────────────────────
        public ActionResult Details(int supplierId)
        {
            var supplier = db.Suppliers.Find(supplierId);
            if (supplier == null) return HttpNotFound();

            // Company load
            supplier.Company = db.Companies
                .Find(supplier.CompanyId);

            // Purchase Invoices
            var invoices = db.PurchaseInvoices
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.PurchaseDate)
                .ToList();

            // Extra Payments
            var payments = db.SupplierPayments
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            decimal totalPurchase = invoices
                .Sum(i => i.NetAmount);
            decimal totalPaidInv = invoices
                .Sum(i => i.PaidAmount);
            decimal totalExtraPaid = payments
                .Sum(p => p.Amount);
            decimal totalPaid = totalPaidInv
                                  + totalExtraPaid;
            decimal totalDue = totalPurchase - totalPaid;
            if (totalDue < 0) totalDue = 0;

            ViewBag.Supplier = supplier;
            ViewBag.Invoices = invoices;
            ViewBag.Payments = payments;
            ViewBag.TotalPurchase = totalPurchase;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalDue = totalDue;

            return View();
        }

        // ─── PAY SUPPLIER GET ──────────────────────────────
        // ─── PAY SUPPLIER GET ─────────────────────────────
        public ActionResult PaySupplier(int supplierId)
        {
            var supplier = db.Suppliers.Find(supplierId);
            if (supplier == null) return HttpNotFound();

            supplier.Company = db.Companies
                .Find(supplier.CompanyId);

            // Due calculate
            decimal totalPurchase = db.PurchaseInvoices
                .Where(p => p.SupplierId == supplierId
                    && p.InvoiceStatus != "Cancelled")
                .Sum(p => (decimal?)p.NetAmount) ?? 0;

            decimal totalPaidInv = db.PurchaseInvoices
                .Where(p => p.SupplierId == supplierId
                    && p.InvoiceStatus != "Cancelled")
                .Sum(p => (decimal?)p.PaidAmount) ?? 0;

            decimal totalPaidExtra = db.SupplierPayments
                .Where(p => p.SupplierId == supplierId)
                .Sum(p => (decimal?)p.Amount) ?? 0;

            decimal totalDue = totalPurchase
                - totalPaidInv;
            if (totalDue < 0) totalDue = 0;

            ViewBag.Supplier = supplier;
            ViewBag.TotalDue = totalDue;

            return View();
        }

        // ─── PAY SUPPLIER POST ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PaySupplier(
            int supplierId,
            decimal paymentAmount,
            string paymentMethod,
            string note)
        {
            var supplier = db.Suppliers.Find(supplierId);
            if (supplier == null) return HttpNotFound();

            // Due calculate
            decimal totalPurchase = db.PurchaseInvoices
                .Where(p => p.SupplierId == supplierId
                    && p.InvoiceStatus != "Cancelled")
                .Sum(p => (decimal?)p.NetAmount) ?? 0;

            decimal totalPaid = db.PurchaseInvoices
                .Where(p => p.SupplierId == supplierId
                    && p.InvoiceStatus != "Cancelled")
                .Sum(p => (decimal?)p.PaidAmount) ?? 0;

            decimal totalDue = totalPurchase - totalPaid;
            if (totalDue < 0) totalDue = 0;

            // Validation
            if (paymentAmount <= 0)
            {
                TempData["Error"] =
                    "Payment amount must be greater than 0!";
                return RedirectToAction("PaySupplier",
                    new { supplierId });
            }

            if (paymentAmount > totalDue)
            {
                TempData["Error"] =
                    "Payment cannot exceed due amount: ৳"
                    + totalDue.ToString("N2");
                return RedirectToAction("PaySupplier",
                    new { supplierId });
            }

            // Payment save
            db.SupplierPayments.Add(new SupplierPayment
            {
                SupplierId = supplierId,
                Amount = paymentAmount,
                PaymentDate = DateTime.Today,
                PaymentMethod = paymentMethod ?? "Cash",
                Note = string.IsNullOrEmpty(note)
                    ? "Payment to "
                        + supplier.SupplierName
                    : note,
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now
            });

            // FIFO — Purchase Invoice গুলো clear করো
            decimal remaining = paymentAmount;

            var pendingInvoices = db.PurchaseInvoices
                .Where(p =>
                    p.SupplierId == supplierId
                    && p.PaidAmount < p.NetAmount
                    && p.InvoiceStatus != "Cancelled")
                .OrderBy(p => p.PurchaseDate)
                .ToList();

            foreach (var inv in pendingInvoices)
            {
                if (remaining <= 0) break;

                decimal due = inv.NetAmount - inv.PaidAmount;

                if (remaining >= due)
                {
                    inv.PaidAmount += due;
                    remaining -= due;
                }
                else
                {
                    inv.PaidAmount += remaining;
                    remaining = 0;
                }
            }

            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Payment",
                Description = "Paid to supplier: "
                    + supplier.SupplierName
                    + " ৳" + paymentAmount,
                TableAffected = "SupplierPayments",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            TempData["Success"] = "Payment of ৳"
                + paymentAmount.ToString("N2")
                + " paid to "
                + supplier.SupplierName + " successfully!";

            return RedirectToAction("Details",
                new { supplierId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}