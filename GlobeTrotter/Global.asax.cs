using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using GlobeTrotter.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace GlobeTrotter
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Re-hash any demo/seeded users whose passwords were inserted
            // directly via SQL (wrong hash format). Runs once, asynchronously.
            Task.Run(async () =>
            {
                try { await SeedDemoUserPasswordsAsync(); }
                catch { /* non-fatal */ }
            });
        }

        /// <summary>
        /// Ensures demo users and system administrator have a valid ASP.NET Identity v3 password hash
        /// so they can log in immediately.
        /// </summary>
        private static async Task SeedDemoUserPasswordsAsync()
        {
            const string demoPassword = "Pass@123";

            using (var db = new ApplicationDbContext())
            {
                var store   = new UserStore<ApplicationUser>(db);
                var manager = new UserManager<ApplicationUser>(store);
                manager.UserValidator = new UserValidator<ApplicationUser>(manager)
                {
                    AllowOnlyAlphanumericUserNames = false,
                    RequireUniqueEmail = true
                };
                manager.PasswordValidator = new PasswordValidator { RequiredLength = 6 };
                var hasher = new PasswordHasher();

                // 1. Ensure Admin Account exists
                var adminUser = await manager.FindByEmailAsync("admin@globetrotter.io");
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin@globetrotter.io",
                        Email = "admin@globetrotter.io",
                        FullName = "System Administrator",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await manager.CreateAsync(adminUser, demoPassword);
                }
                else if (adminUser.PasswordHash == null || adminUser.PasswordHash.Length < 80)
                {
                    adminUser.PasswordHash = hasher.HashPassword(demoPassword);
                    await store.UpdateAsync(adminUser);
                }

                // 2. Known demo traveler accounts seeded via SQL
                var demoEmails = new[]
                {
                    "alex.traveler@globetrotter.com",
                    "elena.wander@globetrotter.com",
                    "marcus.globe@globetrotter.com"
                };

                foreach (var email in demoEmails)
                {
                    var user = await manager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        var newUser = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = email.Split('@')[0].Replace('.', ' '),
                            EmailConfirmed = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await manager.CreateAsync(newUser, demoPassword);
                    }
                    else if (user.PasswordHash == null || user.PasswordHash.Length < 80)
                    {
                        user.PasswordHash = hasher.HashPassword(demoPassword);
                        await store.UpdateAsync(user);
                    }
                }

                await db.SaveChangesAsync();
            }
        }
    }
}
