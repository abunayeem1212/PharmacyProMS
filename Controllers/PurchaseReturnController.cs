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
    public class PurchaseReturnController : Controller
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
            var query = db.PurchaseReturns
                          .Include("PurchaseInvoice")
                          .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(r =>
                    r.ReturnNumber.Contains(searchTerm) ||
                    r.PurchaseInvoice.InvoiceNumber
                        .Contains(searchTerm));

            if (dateFrom.HasValue)
                query = query.Where(r =>
                    r.ReturnDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(r =>
                    r.ReturnDate <= dateTo.Value);

            decimal totalReturn = query
                .Sum(r => (decimal?)r.TotalReturnAmount) ?? 0;

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / PageSize);

            var returns = query
                .OrderByDescending(r => r.ReturnDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Manual load
            var purIds = returns
                .Select(r => r.PurchaseId)
                .Distinct().ToList();
            var purchases = db.PurchaseInvoices
                .Where(p => purIds.Contains(p.PurchaseId))
                .ToList();
            foreach (var r in returns)
                r.PurchaseInvoice = purchases
                    .FirstOrDefault(p =>
                        p.PurchaseId == r.PurchaseId);

            var model = new PurchaseReturnListViewModel
            {
                Returns = returns,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize,
                DateFrom = dateFrom,
                DateTo = dateTo,
                TotalReturn = totalReturn
            };

            return View(model);
        }

        // ─── SEARCH INVOICE ───────────────────────────────────
        public ActionResult SearchInvoice()
        {
            return View();
        }

        // ─── CREATE GET ───────────────────────────────────────
        public ActionResult Create(int purchaseId)
        {
            var invoice = db.PurchaseInvoices
                .Include("PurchaseItems")
                .Include("Supplier")
                .FirstOrDefault(p =>
                    p.PurchaseId == purchaseId);

            if (invoice == null) return HttpNotFound();

            // Manual load
            var medIds = invoice.PurchaseItems
                .Select(i => i.MedicineId)
                .Distinct().ToList();
            var meds = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            foreach (var item in invoice.PurchaseItems)
                item.Medicine = meds.FirstOrDefault(m =>
                    m.MedicineId == item.MedicineId);

            // Already returned quantities
            var prevReturns = db.PurchaseReturnItems
                .Where(ri => ri.PurchaseReturn.PurchaseId
                    == purchaseId)
                .GroupBy(ri => ri.MedicineId)
                .Select(g => new {
                    MedicineId = g.Key,
                    ReturnedQty = g.Sum(x => x.Quantity)
                })
                .ToList();

            var items = invoice.PurchaseItems
                .Select(pi => {
                    int returned = prevReturns
                        .Where(r => r.MedicineId
                            == pi.MedicineId)
                        .Sum(r => r.ReturnedQty);
                    int maxQty = pi.Quantity - returned;

                    return new PurchaseReturnItemViewModel
                    {
                        MedicineId = pi.MedicineId,
                        MedicineName = pi.Medicine != null
                            ? pi.Medicine.MedicineName : "—",
                        MaxQty = maxQty,
                        Quantity = 0,
                        PurchasePrice = pi.PurchasePrice,
                        ReturnAmount = 0
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

            var model = new PurchaseReturnCreateViewModel
            {
                ReturnNumber = GenerateReturnNumber(),
                ReturnDate = DateTime.Today,
                PurchaseId = purchaseId,
                InvoiceNumber = invoice.InvoiceNumber,
                PurchaseDate = invoice.PurchaseDate,
                SupplierName = invoice.Supplier != null
                    ? invoice.Supplier.SupplierName : "—",
                InvoiceTotal = invoice.NetAmount,
                InvoiceItems = items
            };

            return View(model);
        }

        // ─── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            PurchaseReturnCreateViewModel model)
        {
            List<PurchaseReturnItemViewModel> items = null;

            if (!string.IsNullOrEmpty(model.ItemsJson))
                items = JsonConvert.DeserializeObject<List<PurchaseReturnItemViewModel>>(model.ItemsJson);

            if (items == null || !items.Any())
            {
                TempData["Error"] =
                    "Please select at least one item!";
                return RedirectToAction("Create",
                    new { purchaseId = model.PurchaseId });
            }

            decimal totalReturn = items
                .Sum(i => i.ReturnAmount);

            var purReturn = new PurchaseReturn
            {
                ReturnNumber = model.ReturnNumber,
                ReturnDate = model.ReturnDate,
                Reason = model.Reason,
                TotalReturnAmount = totalReturn,
                PurchaseId = model.PurchaseId,
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now,
                ReturnItems =
                    new List<PurchaseReturnItem>()
            };

            foreach (var item in items)
            {
                if (item.Quantity <= 0) continue;

                // Stock কমাও
                var med = db.Medicines.Find(item.MedicineId);
                if (med != null)
                    med.CurrentStock -= item.Quantity;

                purReturn.ReturnItems.Add(
                    new PurchaseReturnItem
                    {
                        MedicineId = item.MedicineId,
                        Quantity = item.Quantity,
                        ReturnAmount = item.ReturnAmount
                    });
            }

            db.PurchaseReturns.Add(purReturn);

            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "Create",
                Description = "Purchase Return: "
                                + purReturn.ReturnNumber,
                TableAffected = "PurchaseReturns",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            TempData["Success"] =
                "Purchase Return created successfully!";
            return RedirectToAction("Details",
                new { id = purReturn.PReturnId });
        }

        // ─── DETAILS ──────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var ret = db.PurchaseReturns
                .Include("PurchaseInvoice")
                .Include("PurchaseInvoice.Supplier")
                .Include("ReturnItems")
                .FirstOrDefault(r => r.PReturnId == id);

            if (ret == null) return HttpNotFound();

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
            var invoice = db.PurchaseInvoices
                .Include("Supplier")
                .Where(p => p.InvoiceNumber == invoiceNo)
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
                data = new
                {
                    invoice.PurchaseId,
                    invoice.InvoiceNumber,
                    invoice.NetAmount,
                    PurchaseDate = invoice.PurchaseDate,
                    SupplierName = invoice.Supplier != null
                        ? invoice.Supplier.SupplierName : "—"
                }
            },
                JsonRequestBehavior.AllowGet);
        }

        // ─── Return Number Generator ───────────────────────────
        private string GenerateReturnNumber()
        {
            string prefix = "PR-" +
                DateTime.Now.ToString("yyyyMM") + "-";
            var last = db.PurchaseReturns
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