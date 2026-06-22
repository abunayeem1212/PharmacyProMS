using PharmacyProMS.Data;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class StockAdjustmentController : Controller
    {
        private ApplicationDbContext db
            = new ApplicationDbContext();
        private const int PageSize = 15;

        // ─── INDEX ────────────────────────────────────────
        public ActionResult Index(
            string searchTerm = "",
            string filterType = "",
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int page = 1)
        {
            var query = db.StockAdjustments
                          .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(a =>
                    a.Medicine.MedicineName
                        .Contains(searchTerm) ||
                    a.Reason.Contains(searchTerm));

            if (!string.IsNullOrWhiteSpace(filterType))
                query = query.Where(a =>
                    a.AdjustmentType == filterType);

            if (dateFrom.HasValue)
                query = query.Where(a =>
                    a.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(a =>
                    a.CreatedAt <= dateTo.Value
                    .AddDays(1));

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / PageSize);

            var adjustments = query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Manual Medicine load
            var medIds = adjustments
                .Select(a => a.MedicineId)
                .Distinct().ToList();
            var medicines = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            foreach (var a in adjustments)
                a.Medicine = medicines.FirstOrDefault(m =>
                    m.MedicineId == a.MedicineId);

            var model = new StockAdjustmentListViewModel
            {
                Adjustments = adjustments,
                SearchTerm = searchTerm,
                FilterType = filterType,
                DateFrom = dateFrom,
                DateTo = dateTo,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize
            };

            return View(model);
        }

        // ─── CREATE GET ───────────────────────────────────
        public ActionResult Create(int? medicineId = null)
        {
            var model = new StockAdjustmentCreateViewModel
            {
                AdjustmentType = "Decrease",
                Medicines = db.Medicines
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.MedicineName)
                    .ToList()
            };

            if (medicineId.HasValue)
            {
                model.MedicineId = medicineId.Value;
                var med = db.Medicines
                    .Find(medicineId.Value);
                if (med != null)
                    model.CurrentStock = med.CurrentStock;
            }

            return View(model);
        }

        // ─── CREATE POST ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            StockAdjustmentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Medicines = db.Medicines
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.MedicineName)
                    .ToList();
                return View(model);
            }

            var medicine = db.Medicines
                .Find(model.MedicineId);
            if (medicine == null)
            {
                ModelState.AddModelError("MedicineId",
                    "Medicine not found!");
                model.Medicines = db.Medicines
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.MedicineName)
                    .ToList();
                return View(model);
            }

            int stockBefore = medicine.CurrentStock;

            // Decrease validation
            if (model.AdjustmentType == "Decrease"
                && model.Quantity > medicine.CurrentStock)
            {
                ModelState.AddModelError("Quantity",
                    "Cannot decrease more than current stock ("
                    + medicine.CurrentStock + ")!");
                model.Medicines = db.Medicines
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.MedicineName)
                    .ToList();
                model.CurrentStock = medicine.CurrentStock;
                return View(model);
            }

            // Stock update
            if (model.AdjustmentType == "Increase")
                medicine.CurrentStock += model.Quantity;
            else
                medicine.CurrentStock -= model.Quantity;

            int stockAfter = medicine.CurrentStock;

            // Adjustment record
            var adjustment = new StockAdjustment
            {
                MedicineId = model.MedicineId,
                AdjustmentType = model.AdjustmentType,
                Quantity = model.Quantity,
                Reason = model.Reason,
                Note = model.Note,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now
            };

            db.StockAdjustments.Add(adjustment);

            // Activity Log
            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = "StockAdjust",
                Description = medicine.MedicineName
                    + " stock "
                    + model.AdjustmentType + " by "
                    + model.Quantity
                    + " (" + model.Reason + ")"
                    + " | Before: " + stockBefore
                    + " → After: " + stockAfter,
                TableAffected = "StockAdjustments",
                LoggedAt = DateTime.Now
            });

            db.SaveChanges();

            TempData["Success"] = medicine.MedicineName
                + " stock updated! "
                + stockBefore + " → " + stockAfter;

            return RedirectToAction("Index");
        }

        // ─── GET MEDICINE STOCK (AJAX) ────────────────────
        [HttpGet]
        public JsonResult GetStock(int medicineId)
        {
            var med = db.Medicines.Find(medicineId);
            if (med == null)
                return Json(new { success = false },
                    JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                currentStock = med.CurrentStock,
                medicineName = med.MedicineName,
                unitType = med.UnitType
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}