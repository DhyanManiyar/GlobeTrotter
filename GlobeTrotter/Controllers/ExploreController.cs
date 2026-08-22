using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using GlobeTrotter.Models;
using Microsoft.AspNet.Identity;

namespace GlobeTrotter.Controllers
{
    [AllowAnonymous]
    public class ExploreController : Controller
    {
        private readonly GlobeTrotterDBEntities1 db = new GlobeTrotterDBEntities1();

        private async Task<string> GetResolvedUserIdAsync()
        {
            if (!Request.IsAuthenticated) return null;

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

            return "demo-user-001";
        }

        // =====================================================================
        // 1. EXPLORE & SEARCH HUB (CITIES & ACTIVITIES)
        // =====================================================================
        public async Task<ActionResult> Index(string q = "", string cat = "All", int? cityId = null, string price = "All", string sort = "popular", string tab = "all")
        {
            // 1. Cities Query
            var cityQuery = db.DestinationCities.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                string cleanQ = q.ToLower().Trim();
                cityQuery = cityQuery.Where(c => c.Name.ToLower().Contains(cleanQ) || 
                                                 c.Country.ToLower().Contains(cleanQ) || 
                                                 c.Region.ToLower().Contains(cleanQ) ||
                                                 c.Description.ToLower().Contains(cleanQ));
            }

            if (cityId.HasValue)
            {
                cityQuery = cityQuery.Where(c => c.CityId == cityId.Value);
            }

            // Sorting Cities
            if (sort == "cost-low")
                cityQuery = cityQuery.OrderBy(c => c.AvgDailyCost);
            else if (sort == "cost-high")
                cityQuery = cityQuery.OrderByDescending(c => c.AvgDailyCost);
            else
                cityQuery = cityQuery.OrderByDescending(c => c.PopularityScore);

            var cities = await cityQuery.ToListAsync();

            // 2. Activities Query
            var actQuery = db.Activities
                .Include(a => a.DestinationCity)
                .Include(a => a.ActivityCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                string cleanQ = q.ToLower().Trim();
                actQuery = actQuery.Where(a => a.Title.ToLower().Contains(cleanQ) || 
                                               a.Description.ToLower().Contains(cleanQ) || 
                                               a.DestinationCity.Name.ToLower().Contains(cleanQ) ||
                                               a.ActivityCategory.CategoryName.ToLower().Contains(cleanQ));
            }

            if (!string.IsNullOrEmpty(cat) && !cat.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                actQuery = actQuery.Where(a => a.ActivityCategory.CategoryName.Equals(cat, StringComparison.OrdinalIgnoreCase));
            }

            if (cityId.HasValue)
            {
                actQuery = actQuery.Where(a => a.CityId == cityId.Value);
            }

            // Price filter
            if (price == "free")
                actQuery = actQuery.Where(a => a.EstimatedCost == 0);
            else if (price == "budget")
                actQuery = actQuery.Where(a => a.EstimatedCost > 0 && a.EstimatedCost <= 50);
            else if (price == "mid")
                actQuery = actQuery.Where(a => a.EstimatedCost > 50 && a.EstimatedCost <= 150);
            else if (price == "luxury")
                actQuery = actQuery.Where(a => a.EstimatedCost > 150);

            // Sorting Activities
            if (sort == "cost-low")
                actQuery = actQuery.OrderBy(a => a.EstimatedCost);
            else if (sort == "cost-high")
                actQuery = actQuery.OrderByDescending(a => a.EstimatedCost);
            else if (sort == "rating")
                actQuery = actQuery.OrderByDescending(a => a.Rating);
            else
                actQuery = actQuery.OrderByDescending(a => a.IsTopPick).ThenByDescending(a => a.Rating);

            var rawActivities = await actQuery.ToListAsync();
            var activities = rawActivities.Select(a => new ActivityExploreItem
            {
                ActivityId = a.ActivityId,
                CityId = a.CityId,
                CityName = a.DestinationCity?.Name ?? "City",
                Country = a.DestinationCity?.Country ?? "",
                Title = a.Title,
                Description = a.Description,
                CategoryName = a.ActivityCategory?.CategoryName ?? "Sightseeing",
                EstimatedCost = a.EstimatedCost,
                DurationHours = a.DurationHours,
                Rating = a.Rating,
                IsTopPick = a.IsTopPick,
                ImageUrl = a.ImageUrl
            }).ToList();

            var categories = await db.ActivityCategories.OrderBy(ac => ac.CategoryName).ToListAsync();
            var allCities = await db.DestinationCities.OrderBy(c => c.Name).ToListAsync();

            // Load user's trips for the "Add to Trip" modal
            var userId = await GetResolvedUserIdAsync();
            var userTrips = !string.IsNullOrEmpty(userId) 
                ? await db.Trips.Where(t => t.UserId == userId).OrderByDescending(t => t.StartDate).ToListAsync()
                : new List<Trip>();

            var model = new ExploreSearchViewModel
            {
                SearchQuery = q,
                SelectedCategory = cat,
                SelectedCityId = cityId,
                PriceRange = price,
                SortBy = sort,
                ActiveTab = tab,
                Cities = cities,
                Activities = activities,
                Categories = categories,
                AllFilterCities = allCities,
                UserTrips = userTrips
            };

            return View(model);
        }

        // =====================================================================
        // 2. QUICK ADD ACTIVITY TO USER'S TRIP (AJAX)
        // =====================================================================
        [HttpPost]
        public async Task<JsonResult> AddActivityToTrip(int tripId, int activityId)
        {
            var userId = await GetResolvedUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please log in to add activities to your trips." });
            }

            var trip = await db.Trips
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.UserId == userId);

            if (trip == null)
            {
                return Json(new { success = false, message = "Trip not found." });
            }

            var activity = await db.Activities.Include(a => a.ActivityCategory).FirstOrDefaultAsync(a => a.ActivityId == activityId);
            if (activity == null)
            {
                return Json(new { success = false, message = "Activity not found." });
            }

            // Find matching stop or use the first stop or create a new stop
            var stop = trip.TripStops.FirstOrDefault(s => s.CityId == activity.CityId) ?? trip.TripStops.FirstOrDefault();

            if (stop == null)
            {
                stop = new TripStop
                {
                    TripId = tripId,
                    CityId = activity.CityId,
                    StopOrder = 1,
                    ArrivalDate = trip.StartDate,
                    DepartureDate = trip.EndDate,
                    AccommodationCost = 120.00m,
                    AccommodationDetails = $"Stay in {activity.DestinationCity?.Name ?? "City"}",
                    TransportCost = 50.00m,
                    TransportMode = "Flight",
                    Notes = $"Section 1: {activity.DestinationCity?.Name}",
                    CreatedAt = DateTime.UtcNow
                };
                db.TripStops.Add(stop);
                await db.SaveChangesAsync();
            }

            var tripAct = new TripActivity
            {
                TripStopId = stop.TripStopId,
                ActivityId = activity.ActivityId,
                CustomTitle = activity.Title,
                CategoryName = activity.ActivityCategory?.CategoryName ?? "Sightseeing",
                ScheduledDate = stop.ArrivalDate,
                TimeOfDay = "Afternoon",
                Cost = activity.EstimatedCost,
                DurationHours = activity.DurationHours,
                OrderIndex = stop.TripActivities.Count + 1,
                Notes = activity.Description,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            db.TripActivities.Add(tripAct);
            await db.SaveChangesAsync();

            return Json(new { success = true, message = $"✨ '{activity.Title}' added to '{trip.Title}'!" });
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
