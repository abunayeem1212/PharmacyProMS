using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using PharmacyProMS.Data;
using PharmacyProMS.Models;

[assembly: OwinStartup(typeof(PharmacyProMS.Startup))]

namespace PharmacyProMS
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login")
            });

            // Seed Roles and Admin User
            SeedRolesAndAdmin();
        }

        private void SeedRolesAndAdmin()
        {
            var context = new ApplicationDbContext();
            var roleManager = new RoleManager<IdentityRole>(
                new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(context));

            // Create Roles
            string[] roles = { "Admin", "Pharmacist", "Customer" };
            foreach (var role in roles)
            {
                if (!roleManager.RoleExists(role))
                    roleManager.Create(new IdentityRole(role));
            }

            // Create Default Admin
            string adminEmail = "admin@pharmacy.com";
            string adminPassword = "Admin@123";

            if (userManager.FindByEmail(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Admin",
                    Phone = "01700000000",
                    IsActive = true
                };

                var result = userManager.Create(admin, adminPassword);
                if (result.Succeeded)
                    userManager.AddToRole(admin.Id, "Admin");
            }
        }
    }
}