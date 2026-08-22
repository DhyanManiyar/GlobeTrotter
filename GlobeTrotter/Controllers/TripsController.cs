using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using GlobeTrotter.Models;
using Microsoft.AspNet.Identity;

namespace GlobeTrotter.Controllers
{
    [Authorize]
    public class TripsController : Controller
    {
        private readonly GlobeTrotterDBEntities1 db = new GlobeTrotterDBEntities1();

        // Helper: Get Current User Id or fallback for testing
        private string CurrentUserId => User.Identity.GetUserId();

        // =====================================================================
        // 1. DASHBOARD & MY TRIPS HUB
        // =====================================================================
        public async Task<ActionResult> Index(string status = "All", string search = "")
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var userEntity = await db.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);

            // Fetch user-specific trips with stops and activities
            var query = db.Trips
                .Include(t => t.TripStops.Select(s => s.DestinationCity))
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .Include(t => t.TripExpenses)
                .Where(t => t.UserId == userId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.ToLower().Trim();
                query = query.Where(t => t.Title.ToLower().Contains(s) || 
                                         t.Description.ToLower().Contains(s) ||
                                         t.TripStops.Any(ts => ts.DestinationCity.Name.ToLower().Contains(s)));
            }

            var rawTrips = await query.OrderByDescending(t => t.StartDate).ToListAsync();

            var today = DateTime.Today;
            var tripCards = new List<TripCardViewModel>();

            int totalCitiesCount = 0;
            decimal totalEstimatedCost = 0m;

            foreach (var t in rawTrips)
            {
                // Calculate total cost for this trip
                decimal stayCost = t.TripStops.Sum(ts => ts.AccommodationCost);
                decimal transportCost = t.TripStops.Sum(ts => ts.TransportCost);
                decimal activityCost = t.TripStops.SelectMany(ts => ts.TripActivities).Sum(ta => ta.Cost);
                decimal miscCost = t.TripExpenses.Sum(te => te.Amount);
                decimal totalTripCost = stayCost + transportCost + activityCost + miscCost;

                totalEstimatedCost += totalTripCost;

                // Determine Trip Status
                string tripStatus;
                if (t.TripStops.Count == 0)
                {
                    tripStatus = "Draft";
                }
                else if (t.StartDate > today)
                {
                    tripStatus = "Upcoming";
                }
                else if (t.StartDate <= today && t.EndDate >= today)
                {
                    tripStatus = "Active";
                }
                else
                {
                    tripStatus = "Completed";
                }

                // Calculate planning progress percentage
                int progress = 20; // base created
                if (t.TripStops.Count > 0) progress += 30;
                if (t.TripStops.Any(s => s.TripActivities.Count > 0)) progress += 30;
                if (totalTripCost > 0) progress += 20;
                if (progress > 100) progress = 100;

                var stopNames = t.TripStops
                    .OrderBy(s => s.StopOrder)
                    .Select(s => s.DestinationCity?.Name ?? "City")
                    .Distinct()
                    .ToList();

                totalCitiesCount += stopNames.Count;

                tripCards.Add(new TripCardViewModel
                {
                    TripId = t.TripId,
                    Title = t.Title,
                    Description = t.Description,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    CoverImageUrl = string.IsNullOrWhiteSpace(t.CoverImageUrl) 
                        ? "https://images.unsplash.com/photo-1488646953014-85cb44e25828?auto=format&fit=crop&w=800&q=80" 
                        : t.CoverImageUrl,
                    TotalBudget = t.TotalBudget,
                    Currency = t.Currency ?? "USD",
                    EstimatedCost = totalTripCost,
                    IsPublic = t.IsPublic,
                    ShareSlug = t.ShareSlug,
                    StopsCount = t.TripStops.Count,
                    StopCityNames = stopNames,
                    Status = tripStatus,
                    ProgressPercent = progress,
                    CreatedAt = t.CreatedAt
                });
            }

            // Filter by selected tab if specified
            var filteredTripCards = tripCards;
            if (!string.IsNullOrEmpty(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filteredTripCards = tripCards.Where(c => c.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Wishlist & recommendations
            int wishlistCount = await db.SavedDestinations.CountAsync(sd => sd.UserId == userId);
            var recommendedCities = await db.DestinationCities
                .OrderByDescending(c => c.PopularityScore)
                .Take(4)
                .ToListAsync();

            var viewModel = new TripDashboardViewModel
            {
                User = userEntity,
                TotalTripsCount = tripCards.Count,
                UpcomingTripsCount = tripCards.Count(t => t.Status == "Upcoming"),
                CompletedTripsCount = tripCards.Count(t => t.Status == "Completed"),
                TotalCitiesPlanned = totalCitiesCount,
                TotalBudgetPlanned = tripCards.Sum(t => t.TotalBudget),
                TotalEstimatedCost = totalEstimatedCost,
                SavedDestinationsCount = wishlistCount,
                Trips = filteredTripCards,
                RecommendedCities = recommendedCities
            };

            ViewBag.CurrentFilter = status;
            ViewBag.SearchQuery = search;

            return View(viewModel);
        }

        // =====================================================================
        // 2. CREATE TRIP
        // =====================================================================
        public ActionResult Create()
        {
            var model = new CreateTripViewModel
            {
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(14),
                TotalBudget = 2500.00m,
                Currency = "USD",
                CoverImageUrl = "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?auto=format&fit=crop&w=1200&q=80"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateTripViewModel model)
        {
            if (model.StartDate > model.EndDate)
            {
                ModelState.AddModelError("EndDate", "End date must be on or after the start date.");
            }

            if (ModelState.IsValid)
            {
                var userId = CurrentUserId;
                string slug = Guid.NewGuid().ToString("N").Substring(0, 12);

                var trip = new Trip
                {
                    UserId = userId,
                    Title = model.Title,
                    Description = model.Description,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    TotalBudget = model.TotalBudget,
                    Currency = model.Currency ?? "USD",
                    CoverImageUrl = string.IsNullOrWhiteSpace(model.CoverImageUrl)
                        ? "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?auto=format&fit=crop&w=1200&q=80"
                        : model.CoverImageUrl,
                    IsPublic = model.IsPublic,
                    ShareSlug = slug,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.Trips.Add(trip);
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Trip '{trip.Title}' created successfully! Start adding your city stops.";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // =====================================================================
        // 3. EDIT TRIP
        // =====================================================================
        public async Task<ActionResult> Edit(int id)
        {
            var userId = CurrentUserId;
            var trip = await db.Trips.FirstOrDefaultAsync(t => t.TripId == id && t.UserId == userId);
            if (trip == null)
            {
                return HttpNotFound("Trip not found or unauthorized.");
            }

            var model = new EditTripViewModel
            {
                TripId = trip.TripId,
                Title = trip.Title,
                Description = trip.Description,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                TotalBudget = trip.TotalBudget,
                Currency = trip.Currency,
                CoverImageUrl = trip.CoverImageUrl,
                IsPublic = trip.IsPublic
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditTripViewModel model)
        {
            if (model.StartDate > model.EndDate)
            {
                ModelState.AddModelError("EndDate", "End date must be on or after start date.");
            }

            if (ModelState.IsValid)
            {
                var userId = CurrentUserId;
                var trip = await db.Trips.FirstOrDefaultAsync(t => t.TripId == model.TripId && t.UserId == userId);
                if (trip == null)
                {
                    return HttpNotFound("Trip not found or unauthorized.");
                }

                trip.Title = model.Title;
                trip.Description = model.Description;
                trip.StartDate = model.StartDate;
                trip.EndDate = model.EndDate;
                trip.TotalBudget = model.TotalBudget;
                trip.Currency = model.Currency ?? "USD";
                trip.CoverImageUrl = model.CoverImageUrl;
                trip.IsPublic = model.IsPublic;
                trip.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Trip '{trip.Title}' updated successfully!";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // =====================================================================
        // 4. DELETE TRIP (AJAX / POST)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = CurrentUserId;
            var trip = await db.Trips.FirstOrDefaultAsync(t => t.TripId == id && t.UserId == userId);
            if (trip == null)
            {
                return Json(new { success = false, message = "Trip not found or unauthorized." });
            }

            db.Trips.Remove(trip);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trip removed from your dashboard.";
            return RedirectToAction("Index");
        }

        // =====================================================================
        // 5. CLONE / FORK TRIP
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Duplicate(int id)
        {
            var userId = CurrentUserId;
            try
            {
                var newTripIdParam = new ObjectParameter("NewTripId", typeof(int));
                db.sp_CloneTrip(id, userId, null, newTripIdParam);

                TempData["SuccessMessage"] = "Itinerary cloned successfully to your trips!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Could not clone trip: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // =====================================================================
        // 6. QUICK ADD CITY STOP TO TRIP (AJAX)
        // =====================================================================
        [HttpPost]
        public async Task<JsonResult> QuickAddCityStop(int tripId, int cityId)
        {
            var userId = CurrentUserId;
            var trip = await db.Trips.Include(t => t.TripStops).FirstOrDefaultAsync(t => t.TripId == tripId && t.UserId == userId);
            if (trip == null)
            {
                return Json(new { success = false, message = "Trip not found." }, JsonRequestBehavior.AllowGet);
            }

            var city = await db.DestinationCities.FindAsync(cityId);
            if (city == null)
            {
                return Json(new { success = false, message = "City not found." }, JsonRequestBehavior.AllowGet);
            }

            int nextOrder = trip.TripStops.Count + 1;
            var stop = new TripStop
            {
                TripId = tripId,
                CityId = cityId,
                StopOrder = nextOrder,
                ArrivalDate = trip.StartDate,
                DepartureDate = trip.StartDate.AddDays(3),
                AccommodationCost = city.AvgDailyCost * 3,
                TransportCost = 100.00m,
                TransportMode = "Flight",
                Notes = $"Added from city catalog: {city.Name}",
                CreatedAt = DateTime.UtcNow
            };

            db.TripStops.Add(stop);
            await db.SaveChangesAsync();

            return Json(new { success = true, message = $"✨ {city.Name} added to '{trip.Title}' as Stop #{nextOrder}!" }, JsonRequestBehavior.AllowGet);
        }

        // =====================================================================
        // 7. GET USER'S TRIPS LIST (JSON for Modals / Dropdowns)
        // =====================================================================
        [HttpGet]
        public async Task<JsonResult> GetMyTripsJson()
        {
            var userId = CurrentUserId;
            var trips = await db.Trips
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new { t.TripId, t.Title, t.StartDate, t.EndDate })
                .ToListAsync();

            return Json(trips, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
