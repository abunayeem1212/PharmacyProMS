using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using PharmacyProMS.Data;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    public class AccountController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        private UserManager<ApplicationUser> _userManager;
        private UserManager<ApplicationUser> UserMgr
        {
            get
            {
                if (_userManager == null)
                    _userManager = new UserManager<ApplicationUser>(
                        new UserStore<ApplicationUser>(db));
                return _userManager;
            }
        }

        private IAuthenticationManager AuthManager
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        // ─── GET: Login ───────────────────────────────────────
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Dashboard");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // ─── POST: Login ──────────────────────────────────────
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = UserMgr.Find(model.Email, model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account is inactive");
                return View(model);
            }

            var identity = UserMgr.CreateIdentity(
                user, DefaultAuthenticationTypes.ApplicationCookie);

            AuthManager.SignOut();
            AuthManager.SignIn(new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            }, identity);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // ─── GET: Register ────────────────────────────────────
        [AllowAnonymous]
        public ActionResult Register()
        {
            ViewBag.Roles = new SelectList(
                new[] { "Admin", "Pharmacist", "Customer" });
            return View();
        }

        // ─── POST: Register ───────────────────────────────────
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(
                    new[] { "Admin", "Pharmacist", "Customer" });
                return View(model);
            }

            // Email already exists check
            if (UserMgr.FindByEmail(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Email already registered");
                ViewBag.Roles = new SelectList(
                    new[] { "Admin", "Pharmacist", "Customer" });
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Phone = model.Phone,
                IsActive = true
            };

            var result = UserMgr.Create(user, model.Password);

            if (result.Succeeded)
            {
                UserMgr.AddToRole(user.Id, model.Role);
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);

            ViewBag.Roles = new SelectList(
                new[] { "Admin", "Pharmacist", "Customer" });
            return View(model);
        }

        // ─── Logout ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            AuthManager.SignOut(
                DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login", "Account");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}