using PharmacyProMS.Data;
using PharmacyProMS.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MedicineCategoryController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index(string searchTerm = "")
        {
            var query = db.MedicineCategories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c =>
                    c.CategoryName.Contains(searchTerm));

            ViewBag.SearchTerm = searchTerm;
            return View(query.OrderBy(c => c.CategoryName).ToList());
        }

        // ─── CREATE GET ───────────────────────────────────────
        public ActionResult Create()
        {
            return View(new MedicineCategory { IsActive = true });
        }

        // ─── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MedicineCategory model)
        {
            if (!ModelState.IsValid) return View(model);

            bool exists = db.MedicineCategories.Any(c =>
                c.CategoryName == model.CategoryName);
            if (exists)
            {
                ModelState.AddModelError("CategoryName",
                    "Category already exists!");
                return View(model);
            }

            db.MedicineCategories.Add(model);
            db.SaveChanges();

            TempData["Success"] = "Category created successfully!";
            return RedirectToAction("Index");
        }

        // ─── EDIT GET ─────────────────────────────────────────
        public ActionResult Edit(int id)
        {
            var cat = db.MedicineCategories.Find(id);
            if (cat == null) return HttpNotFound();
            return View(cat);
        }

        // ─── EDIT POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(MedicineCategory model)
        {
            if (!ModelState.IsValid) return View(model);

            bool exists = db.MedicineCategories.Any(c =>
                c.CategoryName == model.CategoryName &&
                c.CategoryId != model.CategoryId);
            if (exists)
            {
                ModelState.AddModelError("CategoryName",
                    "Category already exists!");
                return View(model);
            }

            var cat = db.MedicineCategories.Find(model.CategoryId);
            cat.CategoryName = model.CategoryName;
            cat.IsActive = model.IsActive;
            db.SaveChanges();

            TempData["Success"] = "Category updated successfully!";
            return RedirectToAction("Index");
        }

        // ─── DELETE AJAX ──────────────────────────────────────
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var cat = db.MedicineCategories.Find(id);
                if (cat == null)
                    return Json(new
                    {
                        success = false,
                        message = "Not found!"
                    });

                bool hasMeds = db.Medicines
                    .Any(m => m.CategoryId == id);
                if (hasMeds)
                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete! Medicines exist."
                    });

                db.MedicineCategories.Remove(cat);
                db.SaveChanges();
                return Json(new
                {
                    success = true,
                    message = "Category deleted!"
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
            var cat = db.MedicineCategories.Find(id);
            if (cat == null)
                return Json(new { success = false });

            cat.IsActive = !cat.IsActive;
            db.SaveChanges();
            return Json(new
            {
                success = true,
                isActive = cat.IsActive,
                message = cat.IsActive
                    ? "Activated!" : "Deactivated!"
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}