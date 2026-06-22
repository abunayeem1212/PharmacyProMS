using PharmacyProMS.Data;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class CustomerLedgerController : Controller
    {
        private ApplicationDbContext db
            = new ApplicationDbContext();

        // ─── INDEX ────────────────────────────────────
        public ActionResult Index()
        {
            var customers = db.Customers
                .Where(c => c.IsActive).ToList();

            var result = new System.Collections.Generic
                .List<CustomerDueSummary>();

            foreach (var c in customers)
            {
                decimal totalSale = db.SaleInvoices
                    .Where(s =>
                        s.CustomerId == c.CustomerId
                        && s.IsCreditSale
                        && s.InvoiceStatus != "Cancelled")
                    .Sum(s => (decimal?)s.NetAmount) ?? 0;

                decimal totalDue = db.SaleInvoices
                    .Where(s =>
                        s.CustomerId == c.CustomerId
                        && s.IsCreditSale
                        && s.DueAmount > 0
                        && s.InvoiceStatus != "Cancelled")
                    .Sum(s => (decimal?)s.DueAmount) ?? 0;

                decimal totalPaid = db.CustomerPayments
                    .Where(p => p.CustomerId == c.CustomerId)
                    .Sum(p => (decimal?)p.Amount) ?? 0;

                result.Add(new CustomerDueSummary
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.CustomerName,
                    Phone = c.Phone,
                    Address = c.Address,
                    TotalSale = totalSale,
                    TotalPaid = totalPaid,
                    TotalDue = totalDue
                });
            }

            var model = new CustomerLedgerIndexViewModel
            {
                Customers = result,
                GrandTotal = result.Sum(r => r.TotalDue),
                DueCount = result
                    .Count(r => r.TotalDue > 0),
                TotalCount = result.Count
            };

            return View(model);
        }

        // ─── DETAILS ──────────────────────────────────
        public ActionResult Details(int customerId)
        {
            var customer = db.Customers.Find(customerId);
            if (customer == null) return HttpNotFound();

            var invoices = db.SaleInvoices
                .Where(s =>
                    s.CustomerId == customerId
                    && s.IsCreditSale
                    && s.InvoiceStatus != "Cancelled")
                .OrderByDescending(s => s.SaleDate)
                .ToList();

            var payments = db.CustomerPayments
                .Where(p => p.CustomerId == customerId)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            decimal totalSale = invoices
                .Sum(i => i.NetAmount);
            decimal totalPaid = payments
                .Sum(p => p.Amount);
            decimal totalDue = invoices
                .Sum(i => i.DueAmount);

            ViewBag.Customer = customer;
            ViewBag.Invoices = invoices;
            ViewBag.Payments = payments;
            ViewBag.TotalSale = totalSale;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalDue = totalDue;

            return View();
        }

        // ─── COLLECT PAYMENT GET ──────────────────────
        public ActionResult CollectPayment(int customerId)
        {
            var customer = db.Customers.Find(customerId);
            if (customer == null) return HttpNotFound();

            decimal totalDue = db.SaleInvoices
                .Where(s =>
                    s.CustomerId == customerId
                    && s.DueAmount > 0
                    && s.InvoiceStatus != "Cancelled")
                .Sum(s => (decimal?)s.DueAmount) ?? 0;

            if (totalDue <= 0)
            {
                TempData["Success"] =
                    "No due amount for this customer!";
                return RedirectToAction("Details",
                    new { customerId });
            }

            ViewBag.Customer = customer;
            ViewBag.TotalDue = totalDue;

            return View();
        }

        // ─── COLLECT PAYMENT POST ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CollectPayment(
            int customerId,
            decimal paymentAmount,
            string paymentMethod,
            string note)
        {
            var customer = db.Customers.Find(customerId);
            if (customer == null) return HttpNotFound();

            decimal totalDue = db.SaleInvoices
                .Where(s =>
                    s.CustomerId == customerId
                    && s.DueAmount > 0
                    && s.InvoiceStatus != "Cancelled")
                .Sum(s => (decimal?)s.DueAmount) ?? 0;

            if (paymentAmount <= 0)
            {
                TempData["Error"] =
                    "Payment amount must be greater than 0!";
                return RedirectToAction("CollectPayment",
                    new { customerId });
            }

            if (paymentAmount > totalDue)
            {
                TempData["Error"] =
                    "Payment cannot exceed due amount!";
                return RedirectToAction("CollectPayment",
                    new { customerId });
            }

            // ─ Payment record save করো ─
            var payment = new CustomerPayment
            {
                CustomerId = customerId,
                Amount = paymentAmount,
                PaymentDate = DateTime.Today,
                PaymentMethod = paymentMethod ?? "Cash",
                Note = string.IsNullOrEmpty(note)
                    ? "Payment received" : note,
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now
            };
            db.CustomerPayments.Add(payment);

            // ─ FIFO — পুরনো invoice আগে clear ─
            decimal remaining = paymentAmount;
            var pendingInvoices = db.SaleInvoices
                .Where(s =>
                    s.CustomerId == customerId
                    && s.DueAmount > 0
                    && s.InvoiceStatus != "Cancelled")
                .OrderBy(s => s.SaleDate)
                .ToList();

            foreach (var inv in pendingInvoices)
            {
                if (remaining <= 0) break;

                if (remaining >= inv.DueAmount)
                {
                    remaining -= inv.DueAmount;
                    inv.PaidAmount += inv.DueAmount;
                    inv.DueAmount = 0;
                }
                else
                {
                    inv.PaidAmount += remaining;
                    inv.DueAmount -= remaining;
                    remaining = 0;
                }
            }

            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Payment",
                Description = "Payment from "
                    + customer.CustomerName
                    + ": ৳" + paymentAmount,
                TableAffected = "CustomerPayments",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            TempData["Success"] = "Payment of ৳"
                + paymentAmount.ToString("N2")
                + " collected from "
                + customer.CustomerName + "!";

            return RedirectToAction("Details",
                new { customerId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}