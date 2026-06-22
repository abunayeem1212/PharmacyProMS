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
    public class PurchaseController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 10;

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index(
            string searchTerm = "",
            string sortBy = "PurchaseDate",
            string sortOrder = "desc",
            int page = 1,
            int? filterSupplier = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var query = db.PurchaseInvoices
                          .Include("Supplier")
                          .Include("Supplier.Company")
                          .Include("PurchaseItems")
                          .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(p =>
                    p.InvoiceNumber.Contains(searchTerm) ||
                    p.Supplier.SupplierName.Contains(searchTerm));

            // Filter by Supplier
            if (filterSupplier.HasValue)
                query = query.Where(p =>
                    p.SupplierId == filterSupplier.Value);

            // Date Range Filter
            if (dateFrom.HasValue)
                query = query.Where(p =>
                    p.PurchaseDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(p =>
                    p.PurchaseDate <= dateTo.Value);

            // Sort
            switch (sortBy)
            {
                case "PurchaseDate":
                    query = sortOrder == "asc"
                        ? query.OrderBy(p => p.PurchaseDate)
                        : query.OrderByDescending(p => p.PurchaseDate);
                    break;
                case "InvoiceNumber":
                    query = sortOrder == "asc"
                        ? query.OrderBy(p => p.InvoiceNumber)
                        : query.OrderByDescending(p => p.InvoiceNumber);
                    break;
                case "NetAmount":
                    query = sortOrder == "asc"
                        ? query.OrderBy(p => p.NetAmount)
                        : query.OrderByDescending(p => p.NetAmount);
                    break;
                default:
                    query = query.OrderByDescending(p => p.PurchaseDate);
                    break;
            }

            // Summary
            decimal totalPurchase = query.Sum(p =>
                (decimal?)p.NetAmount) ?? 0;
            decimal totalPaid = query.Sum(p =>
                (decimal?)p.PaidAmount) ?? 0;

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / PageSize);

            var invoices = query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

            // Manually load Supplier
            var supplierIds = invoices
                .Select(p => p.SupplierId)
                .Distinct().ToList();

            var suppliers = db.Suppliers
                .Where(s => supplierIds.Contains(s.SupplierId))
                .ToList();

            foreach (var inv in invoices)
            {
                inv.Supplier = suppliers.FirstOrDefault(s =>
                    s.SupplierId == inv.SupplierId);
            }

            var model = new PurchaseListViewModel
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
                FilterSupplier = filterSupplier,
                TotalPurchaseAmount = totalPurchase,
                TotalPaidAmount = totalPaid,
                TotalDueAmount = totalPurchase - totalPaid,
                Suppliers = db.Suppliers
                                       .Where(s => s.IsActive)
                                       .OrderBy(s => s.SupplierName)
                                       .ToList()
            };

            return View(model);
        }

        // ─── CREATE GET ───────────────────────────────────────
        public ActionResult Create()
        {
            var model = new PurchaseCreateViewModel
            {
                InvoiceNumber = GenerateInvoiceNumber(),
                PurchaseDate = DateTime.Today,
                Suppliers = db.Suppliers
                                  .Include("Company")
                                  .Where(s => s.IsActive)
                                  .OrderBy(s => s.SupplierName)
                                  .ToList(),
                Medicines = db.Medicines
                                  .Where(m => m.IsActive)
                                  .OrderBy(m => m.MedicineName)
                                  .ToList()
            };
            return View(model);
        }

        // ─── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PurchaseCreateViewModel model)
        {
            // Items JSON parse করো
            List<PurchaseItemViewModel> items = null;
            if (!string.IsNullOrEmpty(model.ItemsJson))
            {
                items = JsonConvert.DeserializeObject
                    <List<PurchaseItemViewModel>>(model.ItemsJson);
            }

            if (items == null || !items.Any())
            {
                ModelState.AddModelError("",
                    "At least one medicine item is required!");
            }

            if (!ModelState.IsValid)
            {
                model.Suppliers = db.Suppliers
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SupplierName).ToList();
                model.Medicines = db.Medicines
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.MedicineName).ToList();
                return View(model);
            }

            // Duplicate Invoice Number check
            bool invoiceExists = db.PurchaseInvoices
                .Any(p => p.InvoiceNumber == model.InvoiceNumber);
            if (invoiceExists)
            {
                ModelState.AddModelError("InvoiceNumber",
                    "Invoice number already exists!");
                model.Suppliers = db.Suppliers
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SupplierName).ToList();
                model.Medicines = db.Medicines
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.MedicineName).ToList();
                return View(model);
            }

            // Calculate Totals
            decimal totalAmount = items.Sum(i => i.SubTotal);
            decimal netAmount = totalAmount - model.Discount;

            // ── Create Master ──────────────────────────────
            var invoice = new PurchaseInvoice
            {
                InvoiceNumber = model.InvoiceNumber,
                PurchaseDate = model.PurchaseDate,
                SupplierId = model.SupplierId,
                TotalAmount = totalAmount,
                Discount = model.Discount,
                NetAmount = netAmount,
                PaidAmount = model.PaidAmount,
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now,
                PurchaseItems = new List<PurchaseInvoiceItem>()
            };

            // ── Create Details + Stock Update ──────────────
            foreach (var item in items)
            {
                // Batch তৈরি করো
                var batch = new MedicineBatch
                {
                    MedicineId = item.MedicineId,
                    BatchNumber = item.BatchNumber,
                    PurchasePrice = item.PurchasePrice,
                    Quantity = item.Quantity,
                    SupplierId = model.SupplierId,
                    CreatedAt = DateTime.Now
                };

                // Expiry Date parse
                if (DateTime.TryParse(item.ExpiryDate,
                    out DateTime expiry))
                    batch.ExpiryDate = expiry;
                else
                    batch.ExpiryDate = DateTime.Today.AddYears(1);

                // Manufacture Date parse
                if (!string.IsNullOrEmpty(item.ManufactureDate) &&
                    DateTime.TryParse(item.ManufactureDate,
                    out DateTime mfgDate))
                    batch.ManufactureDate = mfgDate;

                db.MedicineBatches.Add(batch);

                // Stock বাড়াও
                var medicine = db.Medicines.Find(item.MedicineId);
                if (medicine != null)
                    medicine.CurrentStock += item.Quantity;

                // Details row
                invoice.PurchaseItems.Add(new PurchaseInvoiceItem
                {
                    MedicineId = item.MedicineId,
                    Quantity = item.Quantity,
                    PurchasePrice = item.PurchasePrice,
                    SubTotal = item.SubTotal
                });
            }

            db.PurchaseInvoices.Add(invoice);
            db.SaveChanges();

            // Activity Log
            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Create",
                Description = "Purchase Invoice: "
                                + invoice.InvoiceNumber,
                TableAffected = "PurchaseInvoices",
                LoggedAt = DateTime.Now
            });
            db.SaveChanges();

            TempData["Success"] =
                "Purchase Invoice saved successfully!";
            return RedirectToAction("Details",
                new { id = invoice.PurchaseId });
        }

        // ─── DETAILS ──────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var invoice = db.PurchaseInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            // Supplier load
            invoice.Supplier = db.Suppliers
                .Find(invoice.SupplierId);
            if (invoice.Supplier != null)
                invoice.Supplier.Company = db.Companies
                    .Find(invoice.Supplier.CompanyId);

            // Items load
            var items = db.PurchaseInvoiceItems
                .Where(pi => pi.PurchaseId == id)
                .ToList();

            var medIds = items
                .Select(i => i.MedicineId)
                .Distinct().ToList();
            var batchIds = items
                .Where(i => i.BatchId.HasValue)
                .Select(i => i.BatchId.Value)
                .Distinct().ToList();

            var medicines = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            var batches = db.MedicineBatches
                .Where(b => batchIds.Contains(b.BatchId))
                .ToList();

            foreach (var item in items)
            {
                item.Medicine = medicines.FirstOrDefault(m =>
                    m.MedicineId == item.MedicineId);
                if (item.BatchId.HasValue)
                    item.Batch = batches.FirstOrDefault(b =>
                        b.BatchId == item.BatchId.Value);
            }

            invoice.PurchaseItems = items;

            // Payment summary
            decimal paidFromPayments = db.SupplierPayments
                .Where(p => p.PurchaseId == id)
                .Sum(p => (decimal?)p.Amount) ?? 0;

            ViewBag.Due = invoice.NetAmount
                - invoice.PaidAmount;
            ViewBag.PaidFromPayments = paidFromPayments;

            return View(invoice);
        }

        // ─── DELETE AJAX ──────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public JsonResult Delete(int id)
        {
            try
            {
                var invoice = db.PurchaseInvoices
                    .Include("PurchaseItems")
                    .FirstOrDefault(p => p.PurchaseId == id);

                if (invoice == null)
                    return Json(new
                    {
                        success = false,
                        message = "Invoice not found!"
                    });

                // Stock কমাও
                foreach (var item in invoice.PurchaseItems)
                {
                    var med = db.Medicines.Find(item.MedicineId);
                    if (med != null)
                        med.CurrentStock -= item.Quantity;
                }

                db.PurchaseInvoices.Remove(invoice);
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

        // ─── GET MEDICINE INFO AJAX ───────────────────────────
        [HttpGet]
        public JsonResult GetMedicineInfo(int id)
        {
            var med = db.Medicines
                .Where(m => m.MedicineId == id)
                .Select(m => new
                {
                    m.MedicineId,
                    m.MedicineName,
                    m.GenericName,
                    m.UnitType,
                    m.CurrentStock
                })
                .FirstOrDefault();

            return Json(med, JsonRequestBehavior.AllowGet);
        }

        // ─── INVOICE NUMBER GENERATOR ─────────────────────────
        private string GenerateInvoiceNumber()
        {
            string prefix = "PUR-" +
                DateTime.Now.ToString("yyyyMM") + "-";
            var last = db.PurchaseInvoices
                .Where(p => p.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(p => p.InvoiceNumber)
                .FirstOrDefault();

            int next = 1;
            if (last != null)
            {
                string lastNum = last.InvoiceNumber
                    .Replace(prefix, "");
                if (int.TryParse(lastNum, out int n))
                    next = n + 1;
            }
            return prefix + next.ToString("D4");
        }

        // ─── PAYMENT UPDATE GET ───────────────────────────────
        //public ActionResult UpdatePayment(int id)
        //{
        //    var invoice = db.PurchaseInvoices
        //        .FirstOrDefault(p => p.PurchaseId == id);

        //    if (invoice == null) return HttpNotFound();

        //    // Supplier load
        //    invoice.Supplier = db.Suppliers
        //        .FirstOrDefault(s =>
        //            s.SupplierId == invoice.SupplierId);

        //    decimal due = invoice.NetAmount - invoice.PaidAmount;
        //    ViewBag.Due = due;

        //    return View(invoice);
        //}


        // ─── UPDATE PAYMENT GET ───────────────────────────
        public ActionResult UpdatePayment(int id)
        {
            // Payment History page এ redirect করো
            return RedirectToAction("PaymentHistory",
                new { id = id });
        }

        // ─── PAYMENT UPDATE POST ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePayment(
            int id, decimal additionalPayment)
        {
            var invoice = db.PurchaseInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            decimal due = invoice.NetAmount - invoice.PaidAmount;

            // Validation
            if (additionalPayment <= 0)
            {
                TempData["Error"] =
                    "Payment amount must be greater than 0!";
                return RedirectToAction("UpdatePayment",
                    new { id = id });
            }

            if (additionalPayment > due)
            {
                TempData["Error"] =
                    "Payment cannot exceed due amount!";
                return RedirectToAction("UpdatePayment",
                    new { id = id });
            }

            // Payment update
            invoice.PaidAmount += additionalPayment;

            db.SaveChanges();

            // Activity Log
            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Payment",
                Description = "Payment updated for invoice: "
                                + invoice.InvoiceNumber
                                + " Amount: " + additionalPayment,
                TableAffected = "PurchaseInvoices",
                LoggedAt = DateTime.Now
            });
            db.SaveChanges();

            TempData["Success"] = "Payment updated successfully!";
            return RedirectToAction("Details", new { id = id });
        }



        //
        // ─── PURCHASE EDIT GET ────────────────────────────
        [Authorize(Roles = "Admin,Pharmacist")]
        public ActionResult Edit(int id)
        {
            var invoice = db.PurchaseInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            if (invoice.InvoiceStatus == "Cancelled")
            {
                TempData["Error"] =
                    "Cancelled invoice cannot be edited!";
                return RedirectToAction("Details", new { id });
            }

            invoice.Supplier = db.Suppliers
                .Find(invoice.SupplierId);

            ViewBag.Suppliers = db.Suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.SupplierName)
                .ToList();

            return View(invoice);
        }

        // ─── PURCHASE EDIT POST ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Pharmacist")]
        public ActionResult Edit(
            int id,
            DateTime purchaseDate,
            decimal discount,
            string note)
        {
            var invoice = db.PurchaseInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            decimal newNet = invoice.TotalAmount - discount;

            invoice.PurchaseDate = purchaseDate;
            invoice.Discount = discount;
            invoice.NetAmount = newNet;
            invoice.Note = note;

            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Edit",
                Description = "Purchase Invoice edited: "
                    + invoice.InvoiceNumber,
                TableAffected = "PurchaseInvoices",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            TempData["Success"] = "Invoice updated!";
            return RedirectToAction("Details", new { id });
        }

        // ─── PURCHASE CANCEL ──────────────────────────────
        [Authorize(Roles = "Admin")]
        public ActionResult Cancel(int id)
        {
            var invoice = db.PurchaseInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            if (invoice.InvoiceStatus == "Cancelled")
            {
                TempData["Error"] = "Already cancelled!";
                return RedirectToAction("Details", new { id });
            }

            invoice.Supplier = db.Suppliers
                .Find(invoice.SupplierId);

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Cancel(int id, string cancelReason)
        {
            var invoice = db.PurchaseInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["Error"] = "Cancel reason required!";
                return RedirectToAction("Cancel", new { id });
            }

            // Stock কমাও (purchase cancel হলে)
            var items = db.PurchaseInvoiceItems
                .Where(pi => pi.PurchaseId == id).ToList();

            foreach (var item in items)
            {
                var med = db.Medicines.Find(item.MedicineId);
                if (med != null &&
                    med.CurrentStock >= item.Quantity)
                    med.CurrentStock -= item.Quantity;

                var batch = db.MedicineBatches
    .Where(b => b.MedicineId == item.MedicineId
        && b.Quantity >= item.Quantity)
    .OrderBy(b => b.ExpiryDate)
    .FirstOrDefault();

                if (batch != null)
                    batch.Quantity -= item.Quantity;
            }

            invoice.InvoiceStatus = "Cancelled";
            invoice.CancelledAt = DateTime.Now;
            invoice.CancelledBy = User.Identity.Name;
            invoice.CancelReason = cancelReason;

            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Cancel",
                Description = "Purchase cancelled: "
                    + invoice.InvoiceNumber
                    + " Reason: " + cancelReason,
                TableAffected = "PurchaseInvoices",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            TempData["Success"] =
                "Invoice cancelled! Stock reversed.";
            return RedirectToAction("Index");
        }

        // ─── PURCHASE ADD PAYMENT ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment(
            int purchaseId,
            decimal paymentAmount,
            string paymentMethod,
            string note)
        {
            var invoice = db.PurchaseInvoices.Find(purchaseId);
            if (invoice == null)
                return Json(new { success = false });

            decimal due = invoice.NetAmount - invoice.PaidAmount;

            if (paymentAmount <= 0 || paymentAmount > due)
                return Json(new
                {
                    success = false,
                    message = "Invalid amount!"
                });

            db.SupplierPayments.Add(new SupplierPayment
            {
                SupplierId = invoice.SupplierId,
                Amount = paymentAmount,
                PaymentDate = DateTime.Today,
                PaymentMethod = paymentMethod ?? "Cash",
                Note = note ?? "Payment for "
                    + invoice.InvoiceNumber,
                PurchaseId = purchaseId,
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now
            });

            invoice.PaidAmount += paymentAmount;
            if (invoice.PaidAmount > invoice.NetAmount)
                invoice.PaidAmount = invoice.NetAmount;

            db.SaveChanges();

            decimal newDue = invoice.NetAmount
                - invoice.PaidAmount;

            return Json(new
            {
                success = true,
                message = "Payment added!",
                newPaid = invoice.PaidAmount,
                newDue = newDue,
                isFullyPaid = newDue <= 0
            });
        }


        // ─── PAYMENT HISTORY ──────────────────────────────
        public ActionResult PaymentHistory(int id)
        {
            var invoice = db.PurchaseInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            invoice.Supplier = db.Suppliers
                .Find(invoice.SupplierId);
            if (invoice.Supplier != null)
                invoice.Supplier.Company = db.Companies
                    .Find(invoice.Supplier.CompanyId);

            // Items load
            var items = db.PurchaseInvoiceItems
                .Where(pi => pi.PurchaseId == id)
                .ToList();

            var medIds = items
                .Select(i => i.MedicineId)
                .Distinct().ToList();
            var batchIds = items
                .Where(i => i.BatchId.HasValue)
                .Select(i => i.BatchId.Value)
                .Distinct().ToList();

            var medicines = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            var batches = db.MedicineBatches
                .Where(b => batchIds.Contains(b.BatchId))
                .ToList();

            foreach (var item in items)
            {
                item.Medicine = medicines.FirstOrDefault(m =>
                    m.MedicineId == item.MedicineId);
                if (item.BatchId.HasValue)
                    item.Batch = batches.FirstOrDefault(b =>
                        b.BatchId == item.BatchId.Value);
            }

            invoice.PurchaseItems = items;

            // Payments
            var payments = db.SupplierPayments
                .Where(p => p.PurchaseId == id)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            decimal due = invoice.NetAmount - invoice.PaidAmount;

            ViewBag.Payments = payments;
            ViewBag.TotalPaid = payments.Sum(p => p.Amount);
            ViewBag.Due = due;

            return View(invoice);
        }
        

        //
        public ActionResult ThermalPrint(int id)
        {
            var invoice = db.PurchaseInvoices.Find(id);
            if (invoice == null) return HttpNotFound();

            invoice.Supplier = db.Suppliers
                .Find(invoice.SupplierId);

            var items = db.PurchaseInvoiceItems
                .Where(pi => pi.PurchaseId == id)
                .ToList();

            var medIds = items
                .Select(i => i.MedicineId)
                .Distinct().ToList();
            var batchIds = items
                .Where(i => i.BatchId.HasValue)
                .Select(i => i.BatchId.Value)
                .Distinct().ToList();

            var medicines = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            var batches = db.MedicineBatches
                .Where(b => batchIds.Contains(b.BatchId))
                .ToList();

            foreach (var item in items)
            {
                item.Medicine = medicines.FirstOrDefault(m =>
                    m.MedicineId == item.MedicineId);
                if (item.BatchId.HasValue)
                    item.Batch = batches.FirstOrDefault(b =>
                        b.BatchId == item.BatchId.Value);
            }

            invoice.PurchaseItems = items;

            return View(invoice);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
