using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using PharmacyProMS.Data;
using PharmacyProMS.Models;
using PharmacyProMS.ViewModels;
using System.Linq;
using System.Web.Mvc;

namespace PharmacyProMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(db));

            var users = db.Users.ToList();
            var model = users.Select(u => new UserViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                Role = userManager
                       .GetRoles(u.Id)
                       .FirstOrDefault() ?? "—"
            }).ToList();

            return View(model);
        }

        // ─── TOGGLE USER STATUS AJAX ──────────────────────────
        [HttpPost]
        public JsonResult ToggleStatus(string id)
        {
            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(db));

            var user = userManager.FindById(id);
            if (user == null)
                return Json(new
                {
                    success = false,
                    message = "User not found!"
                });

            // Admin নিজেকে deactivate করতে পারবে না
            if (user.Email == "admin@pharmacy.com")
                return Json(new
                {
                    success = false,
                    message = "Cannot deactivate main admin!"
                });

            user.IsActive = !user.IsActive;
            userManager.Update(user);

            return Json(new
            {
                success = true,
                isActive = user.IsActive,
                message = user.IsActive
                    ? "User activated!"
                    : "User deactivated!"
            });
        }

        // ─── CHANGE ROLE ──────────────────────────────────────
        [HttpPost]
        public JsonResult ChangeRole(string id, string role)
        {
            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(db));
            var roleManager = new RoleManager<IdentityRole>(
                new RoleStore<IdentityRole>(db));

            var user = userManager.FindById(id);
            if (user == null)
                return Json(new
                {
                    success = false,
                    message = "User not found!"
                });

            // Remove old roles
            var currentRoles = userManager.GetRoles(id);
            userManager.RemoveFromRoles(id,
                currentRoles.ToArray());

            // Add new role
            if (roleManager.RoleExists(role))
                userManager.AddToRole(id, role);

            return Json(new
            {
                success = true,
                message = "Role updated to " + role + "!"
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}