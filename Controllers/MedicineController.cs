using PharmacyProMS.Data;
using PharmacyProMS.Helpers;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class MedicineController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 10;

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index(
      string searchTerm = "",
      string sortBy = "MedicineName",
      string sortOrder = "asc",
      int page = 1,
      int? filterCompany = null,
      int? filterCategory = null,
      string filterStock = "")
        {
            var query = db.Medicines
                          .Include("Company")
                          .Include("Category")
                          .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(m =>
                    m.MedicineName.Contains(searchTerm) ||
                    m.GenericName.Contains(searchTerm));

            if (filterCompany.HasValue)
                query = query.Where(m =>
                    m.CompanyId == filterCompany.Value);

            if (filterCategory.HasValue)
                query = query.Where(m =>
                    m.CategoryId == filterCategory.Value);

            switch (filterStock)
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

            switch (sortBy)
            {
                case "SalePrice":
                    query = sortOrder == "asc"
                        ? query.OrderBy(m => m.SalePrice)
                        : query.OrderByDescending(m => m.SalePrice);
                    break;
                case "CurrentStock":
                    query = sortOrder == "asc"
                        ? query.OrderBy(m => m.CurrentStock)
                        : query.OrderByDescending(m => m.CurrentStock);
                    break;
                default:
                    query = sortOrder == "asc"
                        ? query.OrderBy(m => m.MedicineName)
                        : query.OrderByDescending(m => m.MedicineName);
                    break;
            }

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / PageSize);

            // ⚠️ এখানেই Fix — ToList() করার পরে
            // navigation property manually load
            var medicines = query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Company ও Category আলাদা load করো
            var companyIds = medicines.Select(m => m.CompanyId)
                                       .Distinct().ToList();
            var categoryIds = medicines.Select(m => m.CategoryId)
                                       .Distinct().ToList();

            var companies = db.Companies
                               .Where(c => companyIds.Contains(c.CompanyId))
                               .ToList();
            var categories = db.MedicineCategories
                               .Where(c => categoryIds.Contains(c.CategoryId))
                               .ToList();

            // Manually assign করো
            foreach (var med in medicines)
            {
                med.Company = companies.FirstOrDefault(c =>
                                   c.CompanyId == med.CompanyId);
                med.Category = categories.FirstOrDefault(c =>
                                   c.CategoryId == med.CategoryId);
            }

            var model = new MedicineListViewModel
            {
                Medicines = medicines,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize,
                FilterCompany = filterCompany,
                FilterCategory = filterCategory,
                FilterStock = filterStock,
                Companies = db.Companies
                                   .Where(c => c.IsActive)
                                   .OrderBy(c => c.CompanyName)
                                   .ToList(),
                Categories = db.MedicineCategories
                                   .Where(c => c.IsActive)
                                   .OrderBy(c => c.CategoryName)
                                   .ToList()
            };

            return View(model);
        }

        // ─── CREATE GET ───────────────────────────────────────
        public ActionResult Create()
        {
            return View(GetViewModel(new MedicineViewModel
            {
                IsActive = true,
                ReOrderLevel = 10
            }));
        }

        // ─── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MedicineViewModel model)
        {
            if (!ModelState.IsValid)
                return View(GetViewModel(model));

            bool exists = db.Medicines.Any(m =>
                m.MedicineName == model.MedicineName &&
                m.CompanyId == model.CompanyId);
            if (exists)
            {
                ModelState.AddModelError("MedicineName",
                    "Medicine already exists for this company!");
                return View(GetViewModel(model));
            }

            string imgPath = null;
            if (model.ImageFile != null)
                imgPath = ImageHelper.UploadImage(
                    model.ImageFile, "Medicines");

            var medicine = new Medicine
            {
                MedicineName = model.MedicineName,
                GenericName = model.GenericName,
                UnitType = model.UnitType,
                SalePrice = model.SalePrice,
                ReOrderLevel = model.ReOrderLevel,
                CurrentStock = 0,
                Barcode = model.Barcode,
                IsPrescriptionRequired = model.IsPrescriptionRequired,
                IsActive = model.IsActive,
                ImagePath = imgPath,
                CompanyId = model.CompanyId,
                CategoryId = model.CategoryId,
                CreatedAt = DateTime.Now
            };

            db.Medicines.Add(medicine);
            db.SaveChanges();

            LogActivity("Create",
                "Medicine created: " + medicine.MedicineName);

            TempData["Success"] = "Medicine created successfully!";
            return RedirectToAction("Index");
        }

        // ─── EDIT GET ─────────────────────────────────────────
        public ActionResult Edit(int id)
        {
            var med = db.Medicines.Find(id);
            if (med == null) return HttpNotFound();

            return View(GetViewModel(new MedicineViewModel
            {
                MedicineId = med.MedicineId,
                MedicineName = med.MedicineName,
                GenericName = med.GenericName,
                UnitType = med.UnitType,
                SalePrice = med.SalePrice,
                ReOrderLevel = med.ReOrderLevel,
                CurrentStock = med.CurrentStock,
                Barcode = med.Barcode,
                IsPrescriptionRequired = med.IsPrescriptionRequired,
                IsActive = med.IsActive,
                ImagePath = med.ImagePath,
                CompanyId = med.CompanyId,
                CategoryId = med.CategoryId
            }));
        }

        // ─── EDIT POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(MedicineViewModel model)
        {
            if (!ModelState.IsValid)
                return View(GetViewModel(model));

            var med = db.Medicines.Find(model.MedicineId);
            if (med == null) return HttpNotFound();

            bool exists = db.Medicines.Any(m =>
                m.MedicineName == model.MedicineName &&
                m.CompanyId == model.CompanyId &&
                m.MedicineId != model.MedicineId);
            if (exists)
            {
                ModelState.AddModelError("MedicineName",
                    "Medicine already exists!");
                return View(GetViewModel(model));
            }

            if (model.ImageFile != null)
            {
                ImageHelper.DeleteImage(med.ImagePath);
                med.ImagePath = ImageHelper.UploadImage(
                    model.ImageFile, "Medicines");
            }

            med.MedicineName = model.MedicineName;
            med.GenericName = model.GenericName;
            med.UnitType = model.UnitType;
            med.SalePrice = model.SalePrice;
            med.ReOrderLevel = model.ReOrderLevel;
            med.Barcode = model.Barcode;
            med.IsPrescriptionRequired = model.IsPrescriptionRequired;
            med.IsActive = model.IsActive;
            med.CompanyId = model.CompanyId;
            med.CategoryId = model.CategoryId;

            db.SaveChanges();

            LogActivity("Edit",
                "Medicine updated: " + med.MedicineName);

            TempData["Success"] = "Medicine updated successfully!";
            return RedirectToAction("Index");
        }

        // ─── DETAILS ──────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var med = db.Medicines
                .Include("Company")
                .Include("Category")
                .Include("Batches")
                .FirstOrDefault(m => m.MedicineId == id);

            if (med == null) return HttpNotFound();
            return View(med);
        }

        // ─── DELETE AJAX ──────────────────────────────────────
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var med = db.Medicines.Find(id);
                if (med == null)
                    return Json(new
                    {
                        success = false,
                        message = "Not found!"
                    });

                bool hasStock = med.CurrentStock > 0;
                if (hasStock)
                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete! Stock exists."
                    });

                ImageHelper.DeleteImage(med.ImagePath);
                db.Medicines.Remove(med);
                db.SaveChanges();

                LogActivity("Delete",
                    "Medicine deleted: " + med.MedicineName);

                return Json(new
                {
                    success = true,
                    message = "Medicine deleted!"
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

        // ─── TOGGLE STATUS AJAX ───────────────────────────────
        [HttpPost]
        public JsonResult ToggleStatus(int id)
        {
            var med = db.Medicines.Find(id);
            if (med == null)
                return Json(new { success = false });

            med.IsActive = !med.IsActive;
            db.SaveChanges();

            return Json(new
            {
                success = true,
                isActive = med.IsActive,
                message = med.IsActive
                    ? "Activated!" : "Deactivated!"
            });
        }

        // ─── SEARCH AJAX (Cart এ কাজে আসবে) ──────────────────
        [HttpGet]
        [AllowAnonymous]
        public JsonResult SearchMedicine(string term)
        {
            if (string.IsNullOrWhiteSpace(term) ||
                term.Length < 2)
                return Json(new List<object>(),
                    JsonRequestBehavior.AllowGet);

            var today = DateTime.Today;

            var meds = db.Medicines
                .Where(m => m.IsActive &&
                    m.CurrentStock > 0 &&
                    (m.MedicineName.Contains(term) ||
                     m.GenericName.Contains(term) ||
                     m.Barcode.Contains(term)))
                .Take(10)
                .ToList();

            var medIds = meds.Select(m => m.MedicineId)
                .ToList();
            var compIds = meds.Select(m => m.CompanyId)
                .Distinct().ToList();

            var companies = db.Companies
                .Where(c => compIds.Contains(c.CompanyId))
                .ToList();

            // Active batch info (FEFO — First Expiry First Out)
            var batches = db.MedicineBatches
                .Where(b => medIds.Contains(b.MedicineId)
                    && b.Quantity > 0
                    && b.ExpiryDate >= today)
                .OrderBy(b => b.ExpiryDate)
                .ToList();

            var result = meds.Select(m => {
                var batch = batches.FirstOrDefault(b =>
                    b.MedicineId == m.MedicineId);
                return new
                {
                    m.MedicineId,
                    m.MedicineName,
                    GenericName = m.GenericName ?? "",
                    m.SalePrice,
                    m.CurrentStock,
                    UnitType = m.UnitType ?? "Unit",
                    CompanyName = companies
                        .Where(c => c.CompanyId == m.CompanyId)
                        .Select(c => c.CompanyName)
                        .FirstOrDefault() ?? "",
                    m.Barcode,
                    BatchId = batch?.BatchId ?? 0,
                    BatchNumber = batch?.BatchNumber ?? "",
                    ExpiryDate = batch != null
                        ? batch.ExpiryDate.ToString("dd MMM yyyy",
                            System.Globalization.CultureInfo
                            .InvariantCulture) : "",
                    PurchasePrice = batch?.PurchasePrice ?? 0,
                    DaysToExpiry = batch != null
                        ? (batch.ExpiryDate - today).Days : 0
                };
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // ─── HELPER: Load Dropdowns ───────────────────────────
        private MedicineViewModel GetViewModel(MedicineViewModel vm)
        {
            vm.Companies = db.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToList();
            vm.Categories = db.MedicineCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToList();
            return vm;
        }

        // ─── Activity Log ─────────────────────────────────────
        private void LogActivity(string action, string description)
        {
            db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = User.Identity.Name,
                UserName = User.Identity.Name,
                Action = action,
                Description = description,
                TableAffected = "Medicines",
                LoggedAt = DateTime.Now
            });
            db.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}