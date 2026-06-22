using PharmacyProMS.Data;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // ─── MARK ALL READ ────────────────────────────────────
        [HttpPost]
        public JsonResult MarkAllRead()
        {
            var notifs = db.Notifications
                .Where(n => !n.IsRead).ToList();

            foreach (var n in notifs)
                n.IsRead = true;

            db.SaveChanges();

            return Json(new { success = true });
        }

        // ─── MARK ONE READ ────────────────────────────────────
        [HttpPost]
        public JsonResult MarkRead(int id)
        {
            var notif = db.Notifications.Find(id);
            if (notif != null)
            {
                notif.IsRead = true;
                db.SaveChanges();
            }
            return Json(new { success = true });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}