using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using PharmacyProMS.Data;
using PharmacyProMS.Helpers;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private ApplicationDbContext db
            = new ApplicationDbContext();

        private UserManager<ApplicationUser> GetUserManager()
        {
            return new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(db));
        }

        // ─── INDEX ────────────────────────────────────────────
        public ActionResult Index()
        {
            var userManager = GetUserManager();
            var user = userManager.FindById(
                User.Identity.GetUserId());

            if (user == null)
                return RedirectToAction("Login", "Account");

            var role = userManager
                .GetRoles(user.Id)
                .FirstOrDefault() ?? "—";

            var model = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Phone = user.Phone,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                Role = role,
                CreatedAt = user.CreatedAt.ToString(
                    "dd MMM yyyy",
                    System.Globalization.CultureInfo
                    .InvariantCulture)
            };

            return View(model);
        }

        // ─── UPDATE POST ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userManager = GetUserManager();
            var user = userManager.FindById(
                User.Identity.GetUserId());

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Profile picture upload
            if (model.PictureFile != null)
            {
                ImageHelper.DeleteImage(user.ProfilePicture);
                user.ProfilePicture = ImageHelper.UploadImage(
                    model.PictureFile, "Profiles");
            }

            user.FullName = model.FullName;
            user.Phone = model.Phone;

            var result = userManager.Update(user);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Profile updated successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);

            return View(model);
        }

        // ─── CHANGE PASSWORD GET ──────────────────────────────
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // ─── CHANGE PASSWORD POST ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userManager = GetUserManager();
            var userId = User.Identity.GetUserId();

            var result = userManager.ChangePassword(
                userId,
                model.CurrentPassword,
                model.NewPassword);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Password changed successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}