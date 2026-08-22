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
        /// Ensures demo users have a valid ASP.NET Identity v3 password hash
        /// so they can actually log in. Safe to re-run — checks the hash version first.
        /// </summary>
        private static async Task SeedDemoUserPasswordsAsync()
        {
            const string demoPassword = "Pass@123";

            // Known demo email addresses seeded via SQL
            var demoEmails = new[]
            {
                "alex.traveler@globetrotter.com",
                "elena.wander@globetrotter.com"
            };

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

                foreach (var email in demoEmails)
                {
                    var user = await manager.FindByEmailAsync(email);
                    if (user == null) continue;

                    // v2 hashes are 68 chars, v3 are longer — rehash only if needed
                    if (user.PasswordHash == null || user.PasswordHash.Length < 80)
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
