using PharmacyProMS.Data;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ActivityLogController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 20;

        public ActionResult Index(
            string searchTerm = "",
            string action = "",
            string dateFrom = "",
            string dateTo = "",
            int page = 1)
        {
            var query = db.UserActivityLogs
                          .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(l =>
                    l.UserName.Contains(searchTerm) ||
                    l.Description.Contains(searchTerm) ||
                    l.TableAffected.Contains(searchTerm));

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(l =>
                    l.Action == action);

            if (DateTime.TryParse(dateFrom, out DateTime df))
                query = query.Where(l =>
                    l.LoggedAt >= df);

            if (DateTime.TryParse(dateTo, out DateTime dt))
                query = query.Where(l =>
                    l.LoggedAt <= dt.AddDays(1));

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(
                (double)totalCount / PageSize);

            var logs = query
                .OrderByDescending(l => l.LoggedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Action = action;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(logs);
        }

        // ─── CLEAR LOGS AJAX ──────────────────────────────────
        [HttpPost]
        public JsonResult ClearOld()
        {
            var cutoff = DateTime.Now.AddDays(-30);
            var old = db.UserActivityLogs
                .Where(l => l.LoggedAt < cutoff)
                .ToList();

            db.UserActivityLogs.RemoveRange(old);
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = old.Count + " old logs cleared!"
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}