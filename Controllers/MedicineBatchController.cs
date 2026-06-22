using PharmacyProMS.Data;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class MedicineBatchController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 10;

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index(
            string searchTerm = "",
            int page = 1,
            int? filterMedicine = null,
            string filterStatus = "")
        {
            var today = DateTime.Today;
            var day90 = today.AddDays(90);

            var query = db.MedicineBatches
                          .Include("Medicine")
                          .Include("Supplier")
                          .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(b =>
                    b.BatchNumber.Contains(searchTerm) ||
                    b.Medicine.MedicineName
                        .Contains(searchTerm));

            // Filter by Medicine
            if (filterMedicine.HasValue)
                query = query.Where(b =>
                    b.MedicineId == filterMedicine.Value);

            // Filter by Status
            switch (filterStatus)
            {
                case "expired":
                    query = query.Where(b =>
                        b.ExpiryDate < today);
                    break;
                case "expiring":
                    query = query.Where(b =>
                        b.ExpiryDate >= today &&
                        b.ExpiryDate <= day90);
                    break;
                case "ok":
                    query = query.Where(b =>
                        b.ExpiryDate > day90);
                    break;
            }

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / PageSize);

            var batches = query
                .OrderBy(b => b.ExpiryDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Manual navigation load
            var medIds = batches
                .Select(b => b.MedicineId)
                .Distinct().ToList();
            var supIds = batches
                .Where(b => b.SupplierId.HasValue)
                .Select(b => b.SupplierId.Value)
                .Distinct().ToList();

            var meds = db.Medicines
                .Where(m => medIds.Contains(m.MedicineId))
                .ToList();
            var sups = db.Suppliers
                .Where(s => supIds.Contains(s.SupplierId))
                .ToList();

            foreach (var b in batches)
            {
                b.Medicine = meds.FirstOrDefault(m =>
                    m.MedicineId == b.MedicineId);
                if (b.SupplierId.HasValue)
                    b.Supplier = sups.FirstOrDefault(s =>
                        s.SupplierId == b.SupplierId.Value);
            }

            var model = new MedicineBatchListViewModel
            {
                Batches = batches,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize,
                FilterMedicine = filterMedicine,
                FilterStatus = filterStatus,
                Medicines = db.Medicines
                                   .Where(m => m.IsActive)
                                   .OrderBy(m => m.MedicineName)
                                   .ToList()
            };

            return View(model);
        }

        // ─── CREATE GET ───────────────────────────────────────
        public ActionResult Create(int? medicineId = null)
        {
            var model = new MedicineBatchViewModel
            {
                ExpiryDate = DateTime.Today.AddYears(1),
                MedicineId = medicineId ?? 0,
                Medicines = db.Medicines
                               .Where(m => m.IsActive)
                               .OrderBy(m => m.MedicineName)
                               .ToList(),
                Suppliers = db.Suppliers
                               .Where(s => s.IsActive)
                               .OrderBy(s => s.SupplierName)
                               .ToList()
            };
            return View(model);
        }

        // ─── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MedicineBatchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Medicines = db.Medicines
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.MedicineName).ToList();
                model.Suppliers = db.Suppliers
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SupplierName).ToList();
                return View(model);
            }

            // Duplicate Batch Number check
            bool exists = db.MedicineBatches.Any(b =>
                b.BatchNumber == model.BatchNumber &&
                b.MedicineId == model.MedicineId);
            if (exists)
            {
                ModelState.AddModelError("BatchNumber",
                    "Batch number already exists " +
                    "for this medicine!");
                model.Medicines = db.Medicines
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.MedicineName).ToList();
                model.Suppliers = db.Suppliers
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SupplierName).ToList();
                return View(model);
            }

            var batch = new MedicineBatch
            {
                BatchNumber = model.BatchNumber,
                PurchasePrice = model.PurchasePrice,
                ManufactureDate = model.ManufactureDate,
                ExpiryDate = model.ExpiryDate,
                Quantity = model.Quantity,
                MedicineId = model.MedicineId,
                SupplierId = model.SupplierId,
                CreatedAt = DateTime.Now
            };

            db.MedicineBatches.Add(batch);

            // Stock বাড়াও
            var medicine = db.Medicines
                .Find(model.MedicineId);
            if (medicine != null)
                medicine.CurrentStock += model.Quantity;

            db.SaveChanges();

            TempData["Success"] =
                "Batch created successfully!";
            return RedirectToAction("Index");
        }

        // ─── EDIT GET ─────────────────────────────────────────
        public ActionResult Edit(int id)
        {
            var batch = db.MedicineBatches.Find(id);
            if (batch == null) return HttpNotFound();

            var model = new MedicineBatchViewModel
            {
                BatchId = batch.BatchId,
                BatchNumber = batch.BatchNumber,
                PurchasePrice = batch.PurchasePrice,
                ManufactureDate = batch.ManufactureDate,
                ExpiryDate = batch.ExpiryDate,
                Quantity = batch.Quantity,
                MedicineId = batch.MedicineId,
                SupplierId = batch.SupplierId,
                Medicines = db.Medicines
                                    .Where(m => m.IsActive)
                                    .OrderBy(m => m.MedicineName)
                                    .ToList(),
                Suppliers = db.Suppliers
                                    .Where(s => s.IsActive)
                                    .OrderBy(s => s.SupplierName)
                                    .ToList()
            };

            return View(model);
        }

        // ─── EDIT POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(MedicineBatchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Medicines = db.Medicines
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.MedicineName).ToList();
                model.Suppliers = db.Suppliers
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SupplierName).ToList();
                return View(model);
            }

            var batch = db.MedicineBatches.Find(model.BatchId);
            if (batch == null) return HttpNotFound();

            // Stock difference calculate করো
            int oldQty = batch.Quantity;
            int newQty = model.Quantity;
            int diff = newQty - oldQty;

            batch.BatchNumber = model.BatchNumber;
            batch.PurchasePrice = model.PurchasePrice;
            batch.ManufactureDate = model.ManufactureDate;
            batch.ExpiryDate = model.ExpiryDate;
            batch.Quantity = model.Quantity;
            batch.SupplierId = model.SupplierId;

            // Stock update
            var medicine = db.Medicines.Find(batch.MedicineId);
            if (medicine != null)
                medicine.CurrentStock += diff;

            db.SaveChanges();

            TempData["Success"] =
                "Batch updated successfully!";
            return RedirectToAction("Index");
        }

        // ─── DELETE AJAX ──────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public JsonResult Delete(int id)
        {
            try
            {
                var batch = db.MedicineBatches.Find(id);
                if (batch == null)
                    return Json(new
                    {
                        success = false,
                        message = "Batch not found!"
                    });

                // Stock কমাও
                var medicine = db.Medicines
                    .Find(batch.MedicineId);
                if (medicine != null)
                    medicine.CurrentStock -= batch.Quantity;

                db.MedicineBatches.Remove(batch);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Batch deleted!"
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

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}