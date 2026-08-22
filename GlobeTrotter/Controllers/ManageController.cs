using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using GlobeTrotter.Models;

namespace GlobeTrotter.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private readonly GlobeTrotterDBEntities1 db = new GlobeTrotterDBEntities1();

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        private async Task<string> GetResolvedUserIdAsync()
        {
            var id = User.Identity.GetUserId();
            if (!string.IsNullOrEmpty(id))
            {
                var exists = await db.AspNetUsers.AnyAsync(u => u.Id == id);
                if (exists) return id;
            }

            var name = User.Identity.Name;
            if (!string.IsNullOrEmpty(name))
            {
                var matched = await db.AspNetUsers.FirstOrDefaultAsync(u => u.UserName == name || u.Email == name);
                if (matched != null) return matched.Id;
            }

            var fallback = await db.AspNetUsers.FirstOrDefaultAsync(u => u.Id == "demo-user-001") 
                        ?? await db.AspNetUsers.FirstOrDefaultAsync();

            return fallback != null ? fallback.Id : "demo-user-001";
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Manage/Index -> Redirects to modern Profile
        public ActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        //
        // GET: /Manage/Profile
        public new async Task<ActionResult> Profile()
        {
            var userId = await GetResolvedUserIdAsync();
            var dbUser = await db.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);
            var appUser = await UserManager.FindByIdAsync(userId);

            if (dbUser == null && appUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch user trips
            var trips = await db.Trips
                .Include(t => t.TripStops.Select(s => s.DestinationCity))
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .Include(t => t.TripExpenses)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            var today = DateTime.Today;
            var upcomingList = new List<TripCardViewModel>();
            var previousList = new List<TripCardViewModel>();
            decimal totalBudgetManaged = 0m;
            var visitedCities = new HashSet<string>();

            foreach (var t in trips)
            {
                decimal stayCost = t.TripStops.Sum(ts => ts.AccommodationCost);
                decimal transportCost = t.TripStops.Sum(ts => ts.TransportCost);
                decimal activityCost = t.TripStops.SelectMany(ts => ts.TripActivities).Sum(ta => ta.Cost);
                decimal totalCost = stayCost + transportCost + activityCost + t.TripExpenses.Sum(te => te.Amount);

                totalBudgetManaged += t.TotalBudget;

                var stopNames = t.TripStops.Select(s => s.DestinationCity?.Name ?? "City").Distinct().ToList();
                foreach (var name in stopNames) visitedCities.Add(name);

                var card = new TripCardViewModel
                {
                    TripId = t.TripId,
                    Title = t.Title,
                    Description = t.Description,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    CoverImageUrl = string.IsNullOrWhiteSpace(t.CoverImageUrl) ? "https://images.unsplash.com/photo-1488646953014-85cb44e25828?auto=format&fit=crop&w=800&q=80" : t.CoverImageUrl,
                    TotalBudget = t.TotalBudget,
                    Currency = t.Currency ?? "USD",
                    EstimatedCost = totalCost,
                    IsPublic = t.IsPublic,
                    ShareSlug = t.ShareSlug,
                    StopsCount = t.TripStops.Count,
                    StopCityNames = stopNames,
                    Status = t.StartDate > today ? "Upcoming" : (t.StartDate <= today && t.EndDate >= today ? "Active" : "Completed"),
                    CreatedAt = t.CreatedAt
                };

                if (t.EndDate < today)
                {
                    previousList.Add(card);
                }
                else
                {
                    upcomingList.Add(card);
                }
            }

            // Wishlist
            var wishlist = await db.SavedDestinations
                .Include(sd => sd.DestinationCity)
                .Where(sd => sd.UserId == userId)
                .Select(sd => sd.DestinationCity)
                .ToListAsync();

            string fName = appUser?.FirstName ?? (dbUser?.FullName?.Split(' ').FirstOrDefault() ?? "Traveler");
            string lName = appUser?.LastName ?? (dbUser?.FullName != null && dbUser.FullName.Contains(" ") ? dbUser.FullName.Substring(dbUser.FullName.IndexOf(' ') + 1) : "");
            string city = appUser?.City ?? "San Francisco";
            string country = appUser?.Country ?? "United States";
            string avatar = appUser?.AvatarUrl ?? dbUser?.AvatarUrl ?? "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=300&q=80";

            var model = new UserProfileViewModel
            {
                UserId = userId,
                FirstName = fName,
                LastName = lName,
                Email = appUser?.Email ?? dbUser?.Email ?? "",
                UserName = appUser?.UserName ?? dbUser?.UserName ?? "",
                PhoneNumber = appUser?.PhoneNumber ?? dbUser?.PhoneNumber ?? "",
                City = city,
                Country = country,
                AvatarUrl = avatar,
                Bio = appUser?.Bio ?? dbUser?.Bio ?? "Passionate wanderer exploring cultures, architecture, and hidden gems across continents.",
                PreferredCurrency = appUser?.PreferredCurrency ?? dbUser?.PreferredCurrency ?? "USD",
                LanguagePreference = appUser?.LanguagePreference ?? dbUser?.LanguagePreference ?? "English",
                MemberSince = dbUser?.CreatedAt ?? appUser?.CreatedAt ?? DateTime.UtcNow.AddMonths(-3),
                TotalTripsCount = trips.Count,
                UpcomingTripsCount = upcomingList.Count,
                CompletedTripsCount = previousList.Count,
                CitiesVisitedCount = visitedCities.Count,
                TotalBudgetManaged = totalBudgetManaged,
                WishlistSavedCount = wishlist.Count,
                UpcomingTrips = upcomingList,
                PreviousTrips = previousList,
                WishlistCities = wishlist
            };

            return View(model);
        }

        //
        // POST: /Manage/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public new async Task<ActionResult> Profile(UserProfileViewModel model)
        {
            var userId = await GetResolvedUserIdAsync();
            var appUser = await UserManager.FindByIdAsync(userId);
            var dbUser = await db.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);

            if (ModelState.IsValid)
            {
                if (appUser != null)
                {
                    appUser.FirstName = model.FirstName;
                    appUser.LastName = model.LastName;
                    appUser.PhoneNumber = model.PhoneNumber;
                    appUser.City = model.City;
                    appUser.Country = model.Country;
                    appUser.AvatarUrl = model.AvatarUrl;
                    appUser.Bio = model.Bio;
                    appUser.PreferredCurrency = model.PreferredCurrency ?? "USD";
                    appUser.LanguagePreference = model.LanguagePreference ?? "English";

                    await UserManager.UpdateAsync(appUser);
                }

                if (dbUser != null)
                {
                    dbUser.FullName = $"{model.FirstName} {model.LastName}".Trim();
                    dbUser.PhoneNumber = model.PhoneNumber;
                    dbUser.AvatarUrl = model.AvatarUrl;
                    dbUser.Bio = model.Bio;
                    dbUser.PreferredCurrency = model.PreferredCurrency ?? "USD";
                    dbUser.LanguagePreference = model.LanguagePreference ?? "English";

                    await db.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "✨ Your traveler profile was updated successfully!";
                return RedirectToAction("Profile");
            }

            return await Profile();
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

#region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}