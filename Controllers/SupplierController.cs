using PharmacyProMS.Data;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SupplierController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 10;

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index(
            string searchTerm = "",
            string sortBy = "SupplierName",
            string sortOrder = "asc",
            int page = 1,
            int? filterCompany = null)
        {
            var query = db.Suppliers
                          .Include("Company")
                          .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(s =>
                    s.SupplierName.Contains(searchTerm) ||
                    s.Phone.Contains(searchTerm) ||
                    s.Email.Contains(searchTerm));

            // Filter by Company
            if (filterCompany.HasValue)
                query = query.Where(s =>
                    s.CompanyId == filterCompany.Value);

            // Sort
            switch (sortBy)
            {
                case "SupplierName":
                    query = sortOrder == "asc"
                        ? query.OrderBy(s => s.SupplierName)
                        : query.OrderByDescending(s => s.SupplierName);
                    break;
                case "Company":
                    query = sortOrder == "asc"
                        ? query.OrderBy(s => s.Company.CompanyName)
                        : query.OrderByDescending(s =>
                            s.Company.CompanyName);
                    break;
                case "IsActive":
                    query = sortOrder == "asc"
                        ? query.OrderBy(s => s.IsActive)
                        : query.OrderByDescending(s => s.IsActive);
                    break;
                default:
                    query = query.OrderBy(s => s.SupplierName);
                    break;
            }

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / PageSize);

            var suppliers = query
             .Skip((page - 1) * PageSize)
             .Take(PageSize)
             .ToList();

            // Manually load Company
            var companyIds = suppliers
                .Select(s => s.CompanyId)
                .Distinct().ToList();

            var companies = db.Companies
                .Where(c => companyIds.Contains(c.CompanyId))
                .ToList();

            foreach (var sup in suppliers)
            {
                sup.Company = companies.FirstOrDefault(c =>
                    c.CompanyId == sup.CompanyId);
            }

            var model = new SupplierListViewModel
            {
                Suppliers = suppliers,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize,
                FilterCompanyId = filterCompany,
                Companies = db.Companies
                                    .Where(c => c.IsActive)
                                    .OrderBy(c => c.CompanyName)
                                    .ToList()
            };

            return View(model);
        }

        // ─── CREATE GET ───────────────────────────────────────
        public ActionResult Create()
        {
            var model = new SupplierViewModel
            {
                IsActive = true,
                Companies = db.Companies
                              .Where(c => c.IsActive)
                              .OrderBy(c => c.CompanyName)
                              .ToList()
            };
            return View(model);
        }

        // ─── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Companies = db.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyName)
                    .ToList();
                return View(model);
            }

            // Duplicate check
            bool exists = db.Suppliers.Any(s =>
                s.SupplierName == model.SupplierName &&
                s.CompanyId == model.CompanyId);
            if (exists)
            {
                ModelState.AddModelError("SupplierName",
                    "Supplier already exists for this company!");
                model.Companies = db.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyName)
                    .ToList();
                return View(model);
            }

            var supplier = new Supplier
            {
                SupplierName = model.SupplierName,
                Phone = model.Phone,
                Address = model.Address,
                Email = model.Email,
                OpeningBalance = model.OpeningBalance,
                IsActive = model.IsActive,
                CompanyId = model.CompanyId,
                CreatedAt = DateTime.Now
            };

            db.Suppliers.Add(supplier);
            db.SaveChanges();

            LogActivity("Create",
                "Supplier created: " + supplier.SupplierName);

            TempData["Success"] = "Supplier created successfully!";
            return RedirectToAction("Index");
        }

        // ─── EDIT GET ─────────────────────────────────────────
        public ActionResult Edit(int id)
        {
            var supplier = db.Suppliers.Find(id);
            if (supplier == null) return HttpNotFound();

            var model = new SupplierViewModel
            {
                SupplierId = supplier.SupplierId,
                SupplierName = supplier.SupplierName,
                Phone = supplier.Phone,
                Address = supplier.Address,
                Email = supplier.Email,
                OpeningBalance = supplier.OpeningBalance,
                IsActive = supplier.IsActive,
                CompanyId = supplier.CompanyId,
                Companies = db.Companies
                                    .Where(c => c.IsActive)
                                    .OrderBy(c => c.CompanyName)
                                    .ToList()
            };

            return View(model);
        }

        // ─── EDIT POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Companies = db.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyName)
                    .ToList();
                return View(model);
            }

            var supplier = db.Suppliers.Find(model.SupplierId);
            if (supplier == null) return HttpNotFound();

            // Duplicate check (exclude self)
            bool exists = db.Suppliers.Any(s =>
                s.SupplierName == model.SupplierName &&
                s.CompanyId == model.CompanyId &&
                s.SupplierId != model.SupplierId);
            if (exists)
            {
                ModelState.AddModelError("SupplierName",
                    "Supplier already exists for this company!");
                model.Companies = db.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyName)
                    .ToList();
                return View(model);
            }

            supplier.SupplierName = model.SupplierName;
            supplier.Phone = model.Phone;
            supplier.Address = model.Address;
            supplier.Email = model.Email;
            supplier.OpeningBalance = model.OpeningBalance;
            supplier.IsActive = model.IsActive;
            supplier.CompanyId = model.CompanyId;

            db.SaveChanges();

            LogActivity("Edit",
                "Supplier updated: " + supplier.SupplierName);

            TempData["Success"] = "Supplier updated successfully!";
            return RedirectToAction("Index");
        }

        // ─── DETAILS ──────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var supplier = db.Suppliers
                .Include("Company")
                .Include("PurchaseInvoices")
                .FirstOrDefault(s => s.SupplierId == id);

            if (supplier == null) return HttpNotFound();
            return View(supplier);
        }

        // ─── DELETE (AJAX) ────────────────────────────────────
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var supplier = db.Suppliers.Find(id);
                if (supplier == null)
                    return Json(new
                    {
                        success = false,
                        message = "Supplier not found!"
                    });

                bool hasPurchase = db.PurchaseInvoices
                    .Any(p => p.SupplierId == id);
                if (hasPurchase)
                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete! Purchase history exists."
                    });

                db.Suppliers.Remove(supplier);
                db.SaveChanges();

                LogActivity("Delete",
                    "Supplier deleted: " + supplier.SupplierName);

                return Json(new
                {
                    success = true,
                    message = "Supplier deleted successfully!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }

        // ─── TOGGLE STATUS (AJAX) ─────────────────────────────
        [HttpPost]
        public JsonResult ToggleStatus(int id)
        {
            var supplier = db.Suppliers.Find(id);
            if (supplier == null)
                return Json(new { success = false });

            supplier.IsActive = !supplier.IsActive;
            db.SaveChanges();

            return Json(new
            {
                success = true,
                isActive = supplier.IsActive,
                message = supplier.IsActive
                    ? "Supplier activated!"
                    : "Supplier deactivated!"
            });
        }

        // ─── GET SUPPLIERS BY COMPANY (AJAX) ──────────────────
        [HttpGet]
        public JsonResult GetByCompany(int companyId)
        {
            var suppliers = db.Suppliers
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .Select(s => new { s.SupplierId, s.SupplierName })
                .ToList();

            return Json(suppliers, JsonRequestBehavior.AllowGet);
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
                TableAffected = "Suppliers",
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