using Newtonsoft.Json;
using PharmacyProMS.Data;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class SaleController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 10;

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index(
            string searchTerm = "",
            string sortBy = "SaleDate",
            string sortOrder = "desc",
            int page = 1,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var query = db.SaleInvoices
                          .Include("Customer")
                          .Include("SaleItems")
                          .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(s =>
                    s.InvoiceNumber.Contains(searchTerm) ||
                    s.WalkInCustomerName.Contains(searchTerm));

            if (dateFrom.HasValue)
                query = query.Where(s =>
                    s.SaleDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(s =>
                    s.SaleDate <= dateTo.Value);

            switch (sortBy)
            {
                case "SaleDate":
                    query = sortOrder == "asc"
                        ? query.OrderBy(s => s.SaleDate)
                        : query.OrderByDescending(s => s.SaleDate);
                    break;
                case "NetAmount":
                    query = sortOrder == "asc"
                        ? query.OrderBy(s => s.NetAmount)
                        : query.OrderByDescending(s => s.NetAmount);
                    break;
                default:
                    query = query.OrderByDescending(s => s.SaleDate);
                    break;
            }

            decimal totalSale = query.Sum(s =>
                (decimal?)s.NetAmount) ?? 0;
            decimal totalVat = query.Sum(s =>
                (decimal?)s.VatAmount) ?? 0;

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / PageSize);

            var invoices = query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Load Customer manually
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

            var model = new SaleListViewModel
            {
                Invoices = invoices,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize,
                DateFrom = dateFrom,
                DateTo = dateTo,
                TotalSaleAmount = totalSale,
                TotalVatAmount = totalVat
            };

            return View(model);
        }

        // ─── CREATE GET ───────────────────────────────────────
        public ActionResult Create()
        {
            var setting = db.PharmacySettings
                            .FirstOrDefault();

            var model = new SaleCreateViewModel
            {
                InvoiceNumber = GenerateInvoiceNumber(),
                SaleDate = DateTime.Today,
                VatPercentage = setting != null
                    ? setting.VatPercentage : 0,
                Customers = db.Customers
                                   .Where(c => c.IsActive)
                                   .OrderBy(c => c.CustomerName)
                                   .ToList(),
                Medicines = db.Medicines
                                   .Where(m => m.IsActive &&
                                       m.CurrentStock > 0)
                                   .OrderBy(m => m.MedicineName)
                                   .ToList()
            };

            return View(model);
        }

        // ─── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SaleCreateViewModel model)
        {
            List<SaleItemViewModel> items = null;
            if (!string.IsNullOrEmpty(model.ItemsJson))
                items = JsonConvert
                    .DeserializeObject<List<SaleItemViewModel>>(
                        model.ItemsJson);

            if (items == null || !items.Any())
            {
                ModelState.AddModelError("",
                    "At least one medicine is required!");
                model.Customers = db.Customers
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CustomerName).ToList();
                return View(model);
            }

            if (model.IsCreditSale && !model.CustomerId.HasValue)
            {
                ModelState.AddModelError("CustomerId",
                    "Customer required for credit sale!");
                model.Customers = db.Customers
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CustomerName).ToList();
                return View(model);
            }

            // Stock check
            foreach (var item in items)
            {
                var med = db.Medicines.Find(item.MedicineId);
                if (med == null || med.CurrentStock < item.Quantity)
                {
                    ModelState.AddModelError("",
                        "Insufficient stock: " + item.MedicineName);
                    model.Customers = db.Customers
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.CustomerName).ToList();
                    return View(model);
                }
            }

            decimal subtotal = items.Sum(i => i.SubTotal);
            decimal itemDisc = items.Sum(i =>
                i.ItemDiscount * i.Quantity);
            decimal invoiceDisc = model.Discount;
            decimal totalDisc = itemDisc + invoiceDisc;
            decimal afterDisc = subtotal - totalDisc;
            decimal vatAmount = Math.Round(
                afterDisc * model.VatPercentage / 100, 2);
            decimal netAmount = afterDisc + vatAmount;

            decimal paidAmount = model.IsCreditSale
                ? model.PaidAmount : netAmount;
            decimal dueAmount = netAmount - paidAmount;

            var invoice = new SaleInvoice
            {
                InvoiceNumber = model.InvoiceNumber,
                SaleDate = model.SaleDate,
                CustomerId = model.CustomerId,
                WalkInCustomerName = model.WalkInCustomerName,
                TotalAmount = subtotal,
                Discount = totalDisc,
                VatAmount = vatAmount,
                NetAmount = netAmount,
                IsCreditSale = model.IsCreditSale,
                PaidAmount = paidAmount,
                DueAmount = dueAmount,
                PaymentMethod = model.PaymentMethod,
                Note = model.Note,
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now,
                SaleItems = new List<SaleInvoiceItem>()
            };

            foreach (var item in items)
            {
                var med = db.Medicines.Find(item.MedicineId);
                med.CurrentStock -= item.Quantity;

                decimal purchasePrice = db.MedicineBatches
                    .Where(b => b.MedicineId == item.MedicineId
                        && b.Quantity > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => b.PurchasePrice)
                    .FirstOrDefault();

                invoice.SaleItems.Add(new SaleInvoiceItem
                {
                    MedicineId = item.MedicineId,
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice,
                    ItemDiscount = item.ItemDiscount,
                    PurchasePrice = purchasePrice,
                    SubTotal = item.SubTotal
                        - (item.ItemDiscount * item.Quantity)
                });
            }

            db.SaleInvoices.Add(invoice);

            if (model.IsCreditSale && paidAmount > 0)
            {
                db.CustomerPayments.Add(new CustomerPayment
                {
                    CustomerId = model.CustomerId.Value,
                    Amount = paidAmount,
                    PaymentDate = model.SaleDate,
                    Note = "Initial payment: "
                        + model.InvoiceNumber,
                    PaymentMethod = model.PaymentMethod,
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now
                });
            }

            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Create",
                Description = "Sale: " + invoice.InvoiceNumber
                    + " ৳" + netAmount
                    + (invoice.IsCreditSale ? " [CREDIT]" : ""),
                TableAffected = "SaleInvoices",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            TempData["Success"] = invoice.IsCreditSale
                ? "Credit sale saved! Due: ৳"
                  + dueAmount.ToString("N2")
                : "Sale completed! ৳" + netAmount.ToString("N2");

            return RedirectToAction("Details",
                new { id = invoice.InvoiceId });
        }

        // ─── DETAILS ──────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var invoice = db.SaleInvoices
                .Include("Customer")
                .Include("SaleItems")
                .Include("SaleItems.Medicine")
                .FirstOrDefault(s => s.InvoiceId == id);

            if (invoice == null) return HttpNotFound();

            // Pharmacy settings for print
            ViewBag.Setting = db.PharmacySettings.FirstOrDefault();

            return View(invoice);
        }

        // ─── DELETE AJAX ──────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public JsonResult Delete(int id)
        {
            try
            {
                var invoice = db.SaleInvoices
                    .Include("SaleItems")
                    .FirstOrDefault(s => s.InvoiceId == id);

                if (invoice == null)
                    return Json(new
                    {
                        success = false,
                        message = "Not found!"
                    });

                // Stock ফেরত দাও
                foreach (var item in invoice.SaleItems)
                {
                    var med = db.Medicines.Find(item.MedicineId);
                    if (med != null)
                        med.CurrentStock += item.Quantity;
                }

                db.SaleInvoices.Remove(invoice);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Invoice deleted!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ─── Invoice Number Generator ──────────────────────────
        private string GenerateInvoiceNumber()
        {
            string prefix = "SAL-" +
                DateTime.Now.ToString("yyyyMM") + "-";
            var last = db.SaleInvoices
                .Where(s => s.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(s => s.InvoiceNumber)
                .FirstOrDefault();

            int next = 1;
            if (last != null)
            {
                string num = last.InvoiceNumber
                    .Replace(prefix, "");
                if (int.TryParse(num, out int n))
                    next = n + 1;
            }
            return prefix + next.ToString("D4");
        }

        public ActionResult ThermalPrint(int id)
        {
            var invoice = db.SaleInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            // Load data manually
            if (invoice.CustomerId.HasValue)
                invoice.Customer = db.Customers
                    .Find(invoice.CustomerId.Value);

            var items = db.SaleInvoiceItems
                .Where(si => si.InvoiceId == id)
                .ToList();

            var medIds = items.Select(i => i.MedicineId)
                .Distinct().ToList();
            var medicines = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();

            foreach (var item in items)
                item.Medicine = medicines.FirstOrDefault(m =>
                    m.MedicineId == item.MedicineId);

            invoice.SaleItems = items;

            return View(invoice);
        }


        // ─── EDIT GET ─────────────────────────────────────
        [Authorize(Roles = "Admin,Pharmacist")]
        public ActionResult Edit(int id)
        {
            var invoice = db.SaleInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            if (invoice.InvoiceStatus == "Cancelled")
            {
                TempData["Error"] =
                    "Cancelled invoice cannot be edited!";
                return RedirectToAction("Details", new { id });
            }

            // Customer load
            if (invoice.CustomerId.HasValue)
                invoice.Customer = db.Customers
                    .Find(invoice.CustomerId.Value);

            ViewBag.Customers = db.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.CustomerName)
                .ToList();

            return View(invoice);
        }


        ///
        // ─── EDIT POST ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Pharmacist")]
        public ActionResult Edit(
            int id,
            DateTime saleDate,
            decimal discount,
            string note,
            string paymentMethod)
        {
            var invoice = db.SaleInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            if (invoice.InvoiceStatus == "Cancelled")
            {
                TempData["Error"] =
                    "Cancelled invoice cannot be edited!";
                return RedirectToAction("Details", new { id });
            }

            // Recalculate
            decimal newNet = invoice.TotalAmount
                - discount + invoice.VatAmount;

            invoice.SaleDate = saleDate;
            invoice.Discount = discount;
            invoice.NetAmount = newNet;
            invoice.DueAmount = newNet - invoice.PaidAmount;
            if (invoice.DueAmount < 0)
                invoice.DueAmount = 0;
            invoice.Note = note;
            invoice.PaymentMethod = paymentMethod;

            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Edit",
                Description = "Sale Invoice edited: "
                    + invoice.InvoiceNumber,
                TableAffected = "SaleInvoices",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            TempData["Success"] =
                "Invoice updated successfully!";
            return RedirectToAction("Details", new { id });
        }

        // ─── CANCEL GET ───────────────────────────────────
        [Authorize(Roles = "Admin")]
        public ActionResult Cancel(int id)
        {
            var invoice = db.SaleInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            if (invoice.InvoiceStatus == "Cancelled")
            {
                TempData["Error"] =
                    "Invoice already cancelled!";
                return RedirectToAction("Details", new { id });
            }

            if (invoice.CustomerId.HasValue)
                invoice.Customer = db.Customers
                    .Find(invoice.CustomerId.Value);

            return View(invoice);
        }

        // ─── CANCEL POST ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Cancel(int id, string cancelReason)
        {
            var invoice = db.SaleInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["Error"] = "Cancel reason is required!";
                return RedirectToAction("Cancel", new { id });
            }

            // Stock ফিরিয়ে দাও
            var items = db.SaleInvoiceItems
                .Where(si => si.InvoiceId == id).ToList();

            foreach (var item in items)
            {
                var med = db.Medicines.Find(item.MedicineId);
                if (med != null)
                    med.CurrentStock += item.Quantity;
            }

            // Due ছিলো এমন customer এর due update
            invoice.InvoiceStatus = "Cancelled";
            invoice.CancelledAt = DateTime.Now;
            invoice.CancelledBy = User.Identity.Name;
            invoice.CancelReason = cancelReason;
            invoice.DueAmount = 0;

            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Cancel",
                Description = "Sale Invoice cancelled: "
                    + invoice.InvoiceNumber
                    + " Reason: " + cancelReason,
                TableAffected = "SaleInvoices",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            TempData["Success"] =
                "Invoice cancelled! Stock restored.";
            return RedirectToAction("Index");
        }

        // ─── ADD PAYMENT POST ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment(
            int invoiceId,
            decimal paymentAmount,
            string paymentMethod,
            string note)
        {
            var invoice = db.SaleInvoices.Find(invoiceId);
            if (invoice == null)
                return Json(new
                {
                    success = false,
                    message = "Invoice not found!"
                });

            decimal due = invoice.NetAmount
                - invoice.PaidAmount;

            if (paymentAmount <= 0 || paymentAmount > due)
                return Json(new
                {
                    success = false,
                    message = "Invalid payment amount!"
                });

            // Payment record
            if (invoice.CustomerId.HasValue)
            {
                db.CustomerPayments.Add(new CustomerPayment
                {
                    CustomerId = invoice.CustomerId.Value,
                    Amount = paymentAmount,
                    PaymentDate = DateTime.Today,
                    PaymentMethod = paymentMethod ?? "Cash",
                    Note = note ?? "Payment for "
                        + invoice.InvoiceNumber,
                    InvoiceId = invoiceId,
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now
                });
            }

            // Invoice update
            invoice.PaidAmount += paymentAmount;
            invoice.DueAmount -= paymentAmount;
            if (invoice.DueAmount < 0)
                invoice.DueAmount = 0;

            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Payment",
                Description = "Payment received for "
                    + invoice.InvoiceNumber
                    + ": ৳" + paymentAmount,
                TableAffected = "SaleInvoices",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Payment added successfully!",
                newPaid = invoice.PaidAmount,
                newDue = invoice.DueAmount,
                isFullyPaid = invoice.DueAmount <= 0
            });
        }

        // ─── PAYMENT HISTORY ──────────────────────────────
        public ActionResult PaymentHistory(int id)
        {
            var invoice = db.SaleInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            if (invoice.CustomerId.HasValue)
                invoice.Customer = db.Customers
                    .Find(invoice.CustomerId.Value);

            var payments = db.CustomerPayments
                .Where(p => p.InvoiceId == id)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            var items = db.SaleInvoiceItems
                .Where(si => si.InvoiceId == id)
                .ToList();

            var medIds = items.Select(i => i.MedicineId)
                .Distinct().ToList();
            var meds = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            foreach (var item in items)
                item.Medicine = meds.FirstOrDefault(m =>
                    m.MedicineId == item.MedicineId);

            invoice.SaleItems = items;

            ViewBag.Payments = payments;
            ViewBag.TotalPaid = payments.Sum(p => p.Amount);

            return View(invoice);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}