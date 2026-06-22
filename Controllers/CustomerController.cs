using PharmacyProMS.Data;
using PharmacyProMS.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class CustomerController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 10;

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index(
            string searchTerm = "",
            string sortBy = "CustomerName",
            string sortOrder = "asc",
            int page = 1)
        {
            var query = db.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c =>
                    c.CustomerName.Contains(searchTerm) ||
                    c.Phone.Contains(searchTerm) ||
                    c.Email.Contains(searchTerm));

            switch (sortBy)
            {
                case "CustomerName":
                    query = sortOrder == "asc"
                        ? query.OrderBy(c => c.CustomerName)
                        : query.OrderByDescending(c => c.CustomerName);
                    break;
                case "CreatedAt":
                    query = sortOrder == "asc"
                        ? query.OrderBy(c => c.CreatedAt)
                        : query.OrderByDescending(c => c.CreatedAt);
                    break;
                default:
                    query = query.OrderBy(c => c.CustomerName);
                    break;
            }

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / PageSize);

            var customers = query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(customers);
        }

        // ─── CREATE GET ───────────────────────────────────────
        public ActionResult Create()
        {
            return View(new Customer { IsActive = true });
        }

        // ─── CREATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Customer model)
        {
            if (!ModelState.IsValid) return View(model);

            bool exists = db.Customers.Any(c =>
                c.Phone == model.Phone);
            if (exists)
            {
                ModelState.AddModelError("Phone",
                    "Phone number already registered!");
                return View(model);
            }

            model.CreatedAt = DateTime.Now;
            db.Customers.Add(model);
            db.SaveChanges();

            TempData["Success"] = "Customer created successfully!";
            return RedirectToAction("Index");
        }

        // ─── EDIT GET ─────────────────────────────────────────
        public ActionResult Edit(int id)
        {
            var customer = db.Customers.Find(id);
            if (customer == null) return HttpNotFound();
            return View(customer);
        }

        // ─── EDIT POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Customer model)
        {
            if (!ModelState.IsValid) return View(model);

            bool exists = db.Customers.Any(c =>
                c.Phone == model.Phone &&
                c.CustomerId != model.CustomerId);
            if (exists)
            {
                ModelState.AddModelError("Phone",
                    "Phone number already registered!");
                return View(model);
            }

            var customer = db.Customers.Find(model.CustomerId);
            customer.CustomerName = model.CustomerName;
            customer.Phone = model.Phone;
            customer.Address = model.Address;
            customer.Email = model.Email;
            customer.IsActive = model.IsActive;

            db.SaveChanges();

            TempData["Success"] = "Customer updated successfully!";
            return RedirectToAction("Index");
        }

        // ─── DETAILS ──────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var customer = db.Customers
                .Include("SaleInvoices")
                .FirstOrDefault(c => c.CustomerId == id);

            if (customer == null) return HttpNotFound();
            return View(customer);
        }

        // ─── DELETE AJAX ──────────────────────────────────────
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var customer = db.Customers.Find(id);
                if (customer == null)
                    return Json(new
                    {
                        success = false,
                        message = "Not found!"
                    });

                bool hasSales = db.SaleInvoices
                    .Any(s => s.CustomerId == id);
                if (hasSales)
                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete! " +
                            "Sale history exists."
                    });

                db.Customers.Remove(customer);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Customer deleted!"
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
            var customer = db.Customers.Find(id);
            if (customer == null)
                return Json(new { success = false });

            customer.IsActive = !customer.IsActive;
            db.SaveChanges();

            return Json(new
            {
                success = true,
                isActive = customer.IsActive,
                message = customer.IsActive
                    ? "Activated!" : "Deactivated!"
            });
        }

        // ─── SEARCH AJAX (Sale এ কাজে আসবে) ──────────────────
        [HttpGet]
        public JsonResult Search(string term)
        {
            var customers = db.Customers
                .Where(c => c.IsActive &&
                    (c.CustomerName.Contains(term) ||
                     c.Phone.Contains(term)))
                .Select(c => new {
                    c.CustomerId,
                    c.CustomerName,
                    c.Phone,
                    c.Address
                })
                .Take(10)
                .ToList();

            return Json(customers,
                JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}