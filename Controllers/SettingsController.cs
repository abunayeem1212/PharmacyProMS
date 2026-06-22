using PharmacyProMS.Data;
using PharmacyProMS.Helpers;
using PharmacyProMS.Models;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index()
        {
            var setting = db.PharmacySettings.FirstOrDefault();
            if (setting == null)
            {
                setting = new PharmacySetting
                {
                    PharmacyName = "PharmacyPro MS",
                    Currency = "BDT",
                    VatPercentage = 0,
                    LowStockThreshold = 10
                };
                db.PharmacySettings.Add(setting);
                db.SaveChanges();
            }
            return View(setting);
        }

        // ─── SAVE POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(
            PharmacySetting model,
            HttpPostedFileBase LogoFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            var setting = db.PharmacySettings
                .FirstOrDefault();

            if (setting == null)
            {
                setting = new PharmacySetting();
                db.PharmacySettings.Add(setting);
            }

            // Logo upload
            if (LogoFile != null)
            {
                ImageHelper.DeleteImage(setting.LogoPath);
                setting.LogoPath = ImageHelper.UploadImage(
                    LogoFile, "Logo");
            }

            setting.PharmacyName = model.PharmacyName;
            setting.Address = model.Address;
            setting.Phone = model.Phone;
            setting.VatPercentage = model.VatPercentage;
            setting.Currency = model.Currency;
            setting.LowStockThreshold = model.LowStockThreshold;

            db.SaveChanges();

            TempData["Success"] = "Settings saved successfully!";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}