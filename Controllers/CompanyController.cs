using PharmacyProMS.Data;
using PharmacyProMS.Helpers;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CompanyController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 10;

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index(
            string searchTerm = "",
            string sortBy = "CompanyName",
            string sortOrder = "asc",
            int page = 1)
        {
            var query = db.Companies.AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c =>
                    c.CompanyName.Contains(searchTerm) ||
                    c.Phone.Contains(searchTerm) ||
                    c.Email.Contains(searchTerm));

            // Sort
            switch (sortBy)
            {
                case "CompanyName":
                    query = sortOrder == "asc"
                        ? query.OrderBy(c => c.CompanyName)
                        : query.OrderByDescending(c => c.CompanyName);
                    break;
                case "IsActive":
                    query = sortOrder == "asc"
                        ? query.OrderBy(c => c.IsActive)
                        : query.OrderByDescending(c => c.IsActive);
                    break;
                case "CreatedAt":
                    query = sortOrder == "asc"
                        ? query.OrderBy(c => c.CreatedAt)
                        : query.OrderByDescending(c => c.CreatedAt);
                    break;
                default:
                    query = query.OrderBy(c => c.CompanyName);
                    break;
            }

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);

            var companies = query
                .Include("Medicines")
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var model = new CompanyListViewModel
            {
                Companies = companies,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize
            };

            return View(model);
        }

        // ─── CREATE GET ───────────────────────────────────────
        public ActionResult Create()
        {
            return View(new CompanyViewModel { IsActive = true });
        }

        // ─── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CompanyViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Duplicate check
            bool exists = db.Companies.Any(c =>
                c.CompanyName == model.CompanyName);
            if (exists)
            {
                ModelState.AddModelError("CompanyName",
                    "Company name already exists!");
                return View(model);
            }

            string logoPath = null;
            if (model.LogoFile != null)
                logoPath = ImageHelper.UploadImage(
                    model.LogoFile, "Companies");

            var company = new Company
            {
                CompanyName = model.CompanyName,
                Address = model.Address,
                Phone = model.Phone,
                Email = model.Email,
                IsActive = model.IsActive,
                LogoPath = logoPath,
                CreatedAt = DateTime.Now
            };

            db.Companies.Add(company);
            db.SaveChanges();

            // Activity Log
            LogActivity("Create", "Company created: " + company.CompanyName);

            TempData["Success"] = "Company created successfully!";
            return RedirectToAction("Index");
        }

        // ─── EDIT GET ─────────────────────────────────────────
        public ActionResult Edit(int id)
        {
            var company = db.Companies.Find(id);
            if (company == null) return HttpNotFound();

            var model = new CompanyViewModel
            {
                CompanyId = company.CompanyId,
                CompanyName = company.CompanyName,
                Address = company.Address,
                Phone = company.Phone,
                Email = company.Email,
                IsActive = company.IsActive,
                LogoPath = company.LogoPath,
                CreatedAt = company.CreatedAt
            };

            return View(model);
        }

        // ─── EDIT POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CompanyViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var company = db.Companies.Find(model.CompanyId);
            if (company == null) return HttpNotFound();

            // Duplicate check (exclude self)
            bool exists = db.Companies.Any(c =>
                c.CompanyName == model.CompanyName &&
                c.CompanyId != model.CompanyId);
            if (exists)
            {
                ModelState.AddModelError("CompanyName",
                    "Company name already exists!");
                return View(model);
            }

            // New image uploaded?
            if (model.LogoFile != null)
            {
                ImageHelper.DeleteImage(company.LogoPath);
                company.LogoPath = ImageHelper.UploadImage(
                    model.LogoFile, "Companies");
            }

            company.CompanyName = model.CompanyName;
            company.Address = model.Address;
            company.Phone = model.Phone;
            company.Email = model.Email;
            company.IsActive = model.IsActive;

            db.SaveChanges();

            LogActivity("Edit", "Company updated: " + company.CompanyName);

            TempData["Success"] = "Company updated successfully!";
            return RedirectToAction("Index");
        }

        // ─── DETAILS ──────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var company = db.Companies
                .Include("Medicines")
                .Include("Suppliers")
                .FirstOrDefault(c => c.CompanyId == id);

            if (company == null) return HttpNotFound();
            return View(company);
        }

        // ─── DELETE (AJAX) ────────────────────────────────────
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var company = db.Companies.Find(id);
                if (company == null)
                    return Json(new
                    {
                        success = false,
                        message = "Company not found!"
                    });

                // Check if medicines exist
                bool hasMedicines = db.Medicines
                    .Any(m => m.CompanyId == id);
                if (hasMedicines)
                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete! Medicines exist under this company."
                    });

                ImageHelper.DeleteImage(company.LogoPath);
                db.Companies.Remove(company);
                db.SaveChanges();

                LogActivity("Delete",
                    "Company deleted: " + company.CompanyName);

                return Json(new
                {
                    success = true,
                    message = "Company deleted successfully!"
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
            var company = db.Companies.Find(id);
            if (company == null)
                return Json(new { success = false });

            company.IsActive = !company.IsActive;
            db.SaveChanges();

            return Json(new
            {
                success = true,
                isActive = company.IsActive,
                message = company.IsActive
                    ? "Company activated!"
                    : "Company deactivated!"
            });
        }

        // ─── Activity Log Helper ──────────────────────────────
        private void LogActivity(string action, string description)
        {
            var log = new UserActivityLog
            {
                UserId = System.Web.HttpContext.Current
                                    .User.Identity.Name,
                UserName = System.Web.HttpContext.Current
                                    .User.Identity.Name,
                Action = action,
                Description = description,
                TableAffected = "Companies",
                LoggedAt = DateTime.Now
            };
            db.UserActivityLogs.Add(log);
            db.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}