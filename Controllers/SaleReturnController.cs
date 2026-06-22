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
    public class SaleReturnController : Controller
    {
        private ApplicationDbContext db
            = new ApplicationDbContext();
        private const int PageSize = 10;

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index(
            string searchTerm = "",
            int page = 1,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var query = db.SaleReturns
                          .Include("SaleInvoice")
                          .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(r =>
                    r.ReturnNumber.Contains(searchTerm) ||
                    r.SaleInvoice.InvoiceNumber
                        .Contains(searchTerm));

            if (dateFrom.HasValue)
                query = query.Where(r =>
                    r.ReturnDate >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(r =>
                    r.ReturnDate <= dateTo.Value);

            decimal totalRefund = query
                .Sum(r => (decimal?)r.TotalReturnAmount)
                ?? 0;

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / PageSize);

            var returns = query
                .OrderByDescending(r => r.ReturnDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Manual load
            var invIds = returns
                .Select(r => r.InvoiceId)
                .Distinct().ToList();
            var invoices = db.SaleInvoices
                .Where(s => invIds.Contains(s.InvoiceId))
                .ToList();
            foreach (var r in returns)
                r.SaleInvoice = invoices.FirstOrDefault(
                    s => s.InvoiceId == r.InvoiceId);

            var model = new SaleReturnListViewModel
            {
                Returns = returns,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize,
                DateFrom = dateFrom,
                DateTo = dateTo,
                TotalRefund = totalRefund
            };

            return View(model);
        }

        // ─── STEP 1: Search Invoice ───────────────────────────
        public ActionResult SearchInvoice()
        {
            return View();
        }

        // ─── STEP 2: Create Return Form ───────────────────────
        public ActionResult Create(int invoiceId)
        {
            var invoice = db.SaleInvoices
                .Include("SaleItems")
                .Include("Customer")
                .FirstOrDefault(s =>
                    s.InvoiceId == invoiceId);

            if (invoice == null) return HttpNotFound();

            // Manual load
            var medIds = invoice.SaleItems
                .Select(i => i.MedicineId)
                .Distinct().ToList();
            var meds = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            foreach (var item in invoice.SaleItems)
                item.Medicine = meds.FirstOrDefault(m =>
                    m.MedicineId == item.MedicineId);

            // Already returned quantities
            var prevReturns = db.SaleReturnItems
                .Where(ri => ri.SaleReturn.InvoiceId
                    == invoiceId)
                .GroupBy(ri => ri.MedicineId)
                .Select(g => new {
                    MedicineId = g.Key,
                    ReturnedQty = g.Sum(x => x.Quantity)
                })
                .ToList();

            var items = invoice.SaleItems
                .Select(si => {
                    int returned = prevReturns
                        .Where(r => r.MedicineId
                            == si.MedicineId)
                        .Sum(r => r.ReturnedQty);
                    int maxQty = si.Quantity - returned;

                    return new SaleReturnItemViewModel
                    {
                        MedicineId = si.MedicineId,
                        MedicineName = si.Medicine != null
                            ? si.Medicine.MedicineName : "—",
                        MaxQty = maxQty,
                        Quantity = 0,
                        SalePrice = si.SalePrice,
                        RefundAmount = 0
                    };
                })
                .Where(x => x.MaxQty > 0)
                .ToList();

            if (!items.Any())
            {
                TempData["Error"] =
                    "All items already returned!";
                return RedirectToAction("SearchInvoice");
            }

            var model = new SaleReturnCreateViewModel
            {
                ReturnNumber = GenerateReturnNumber(),
                ReturnDate = DateTime.Today,
                InvoiceId = invoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                SaleDate = invoice.SaleDate,
                CustomerName = invoice.Customer != null
                    ? invoice.Customer.CustomerName
                    : invoice.WalkInCustomerName
                        ?? "Walk-in",
                InvoiceTotal = invoice.NetAmount,
                InvoiceItems = items
            };

            return View(model);
        }

        // ─── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            SaleReturnCreateViewModel model)
        {
            List<SaleReturnItemViewModel> items = null;

            if (!string.IsNullOrEmpty(model.ItemsJson))
                items = JsonConvert.DeserializeObject<List<SaleReturnItemViewModel>>(model.ItemsJson);

            if (items == null || !items.Any())
            {
                ModelState.AddModelError("",
                    "Please select at least one item!");
                return RedirectToAction("Create",
                    new { invoiceId = model.InvoiceId });
            }

            decimal totalRefund = items
                .Sum(i => i.RefundAmount);

            // ── Master ────────────────────────────────────
            var saleReturn = new SaleReturn
            {
                ReturnNumber = model.ReturnNumber,
                ReturnDate = model.ReturnDate,
                Reason = model.Reason,
                TotalReturnAmount = totalRefund,
                InvoiceId = model.InvoiceId,
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now,
                ReturnItems =
                    new List<SaleReturnItem>()
            };

            // ── Details + Stock Increase ───────────────────
            foreach (var item in items)
            {
                if (item.Quantity <= 0) continue;

                // Stock ফেরত দাও
                var med = db.Medicines.Find(item.MedicineId);
                if (med != null)
                    med.CurrentStock += item.Quantity;

                saleReturn.ReturnItems.Add(
                    new SaleReturnItem
                    {
                        MedicineId = item.MedicineId,
                        Quantity = item.Quantity,
                        RefundAmount = item.RefundAmount
                    });
            }

            db.SaleReturns.Add(saleReturn);

            // Activity Log
            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Create",
                Description = "Sale Return: "
                                + saleReturn.ReturnNumber,
                TableAffected = "SaleReturns",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            TempData["Success"] =
                "Sale Return created successfully!";
            return RedirectToAction("Details",
                new { id = saleReturn.ReturnId });
        }

        // ─── DETAILS ──────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var ret = db.SaleReturns
                .Include("SaleInvoice")
                .Include("ReturnItems")
                .FirstOrDefault(r => r.ReturnId == id);

            if (ret == null) return HttpNotFound();

            // Manual load
            var medIds = ret.ReturnItems
                .Select(i => i.MedicineId)
                .Distinct().ToList();
            var meds = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            foreach (var item in ret.ReturnItems)
                item.Medicine = meds.FirstOrDefault(m =>
                    m.MedicineId == item.MedicineId);

            return View(ret);
        }

        // ─── GET INVOICE AJAX ─────────────────────────────────
        [HttpGet]
        public JsonResult GetInvoice(string invoiceNo)
        {
            var invoice = db.SaleInvoices
                .Where(s => s.InvoiceNumber == invoiceNo)
                .Select(s => new {
                    s.InvoiceId,
                    s.InvoiceNumber,
                    s.NetAmount,
                    SaleDate = s.SaleDate,
                    CustomerName = s.WalkInCustomerName
                                    ?? "Walk-in"
                })
                .FirstOrDefault();

            if (invoice == null)
                return Json(new
                {
                    success = false,
                    message = "Invoice not found!"
                },
                    JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                data = invoice
            },
                JsonRequestBehavior.AllowGet);
        }

        // ─── Return Number Generator ───────────────────────────
        private string GenerateReturnNumber()
        {
            string prefix = "SR-" +
                DateTime.Now.ToString("yyyyMM") + "-";
            var last = db.SaleReturns
                .Where(r => r.ReturnNumber.StartsWith(prefix))
                .OrderByDescending(r => r.ReturnNumber)
                .FirstOrDefault();

            int next = 1;
            if (last != null)
            {
                string num = last.ReturnNumber
                    .Replace(prefix, "");
                if (int.TryParse(num, out int n))
                    next = n + 1;
            }
            return prefix + next.ToString("D4");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}