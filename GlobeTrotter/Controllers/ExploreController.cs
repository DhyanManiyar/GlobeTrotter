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
        public async Task<ActionResult> Index(
            string q = "", 
            string region = "All",
            string country = "All",
            string cat = "All", 
            int? cityId = null, 
            string price = "All", 
            string duration = "All",
            string sort = "popular", 
            string tab = "all")
        {
            // 1. Distinct Filter Lists
            var allRegions = await db.DestinationCities.Select(c => c.Region).Distinct().OrderBy(r => r).ToListAsync();
            var allCountries = await db.DestinationCities.Select(c => c.Country).Distinct().OrderBy(c => c).ToListAsync();
            var categories = await db.ActivityCategories.OrderBy(ac => ac.CategoryName).ToListAsync();
            var allCities = await db.DestinationCities.OrderBy(c => c.Name).ToListAsync();

            // 2. Cities Query
            var cityQuery = db.DestinationCities.Include(c => c.Activities).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                string cleanQ = q.ToLower().Trim();
                cityQuery = cityQuery.Where(c => c.Name.ToLower().Contains(cleanQ) || 
                                                 c.Country.ToLower().Contains(cleanQ) || 
                                                 c.Region.ToLower().Contains(cleanQ) ||
                                                 c.Description.ToLower().Contains(cleanQ));
            }

            if (!string.IsNullOrEmpty(region) && !region.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                cityQuery = cityQuery.Where(c => c.Region.Equals(region, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(country) && !country.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                cityQuery = cityQuery.Where(c => c.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
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
            else if (sort == "name")
                cityQuery = cityQuery.OrderBy(c => c.Name);
            else
                cityQuery = cityQuery.OrderByDescending(c => c.PopularityScore);

            var cities = await cityQuery.ToListAsync();

            // 3. Activities Query
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
                                               a.DestinationCity.Country.ToLower().Contains(cleanQ) ||
                                               a.ActivityCategory.CategoryName.ToLower().Contains(cleanQ));
            }

            if (!string.IsNullOrEmpty(region) && !region.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                actQuery = actQuery.Where(a => a.DestinationCity.Region.Equals(region, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(country) && !country.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                actQuery = actQuery.Where(a => a.DestinationCity.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(cat) && !cat.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                actQuery = actQuery.Where(a => a.ActivityCategory.CategoryName.Equals(cat, StringComparison.OrdinalIgnoreCase));
            }

            if (cityId.HasValue)
            {
                actQuery = actQuery.Where(a => a.CityId == cityId.Value);
            }

            // Price / Cost filter
            if (price == "free")
                actQuery = actQuery.Where(a => a.EstimatedCost == 0);
            else if (price == "budget")
                actQuery = actQuery.Where(a => a.EstimatedCost > 0 && a.EstimatedCost <= 50);
            else if (price == "mid")
                actQuery = actQuery.Where(a => a.EstimatedCost > 50 && a.EstimatedCost <= 150);
            else if (price == "luxury")
                actQuery = actQuery.Where(a => a.EstimatedCost > 150);

            // Duration filter
            if (duration == "short")
                actQuery = actQuery.Where(a => a.DurationHours < 2.0m);
            else if (duration == "medium")
                actQuery = actQuery.Where(a => a.DurationHours >= 2.0m && a.DurationHours <= 4.0m);
            else if (duration == "long")
                actQuery = actQuery.Where(a => a.DurationHours > 4.0m);

            // Sorting Activities
            if (sort == "cost-low")
                actQuery = actQuery.OrderBy(a => a.EstimatedCost);
            else if (sort == "cost-high")
                actQuery = actQuery.OrderByDescending(a => a.EstimatedCost);
            else if (sort == "rating")
                actQuery = actQuery.OrderByDescending(a => a.Rating);
            else if (sort == "duration-low")
                actQuery = actQuery.OrderBy(a => a.DurationHours);
            else if (sort == "duration-high")
                actQuery = actQuery.OrderByDescending(a => a.DurationHours);
            else
                actQuery = actQuery.OrderByDescending(a => a.IsTopPick).ThenByDescending(a => a.Rating);

            var rawActivities = await actQuery.ToListAsync();

            // 4. Load user's trips & check which activities are added
            var userId = await GetResolvedUserIdAsync();
            var userTrips = !string.IsNullOrEmpty(userId) 
                ? await db.Trips.Include(t => t.TripStops.Select(s => s.TripActivities))
                                .Where(t => t.UserId == userId)
                                .OrderByDescending(t => t.StartDate)
                                .ToListAsync()
                : new List<Trip>();

            var addedActivitiesMap = new Dictionary<int, int>(); // ActivityId -> TripActivityId
            foreach (var t in userTrips)
            {
                foreach (var s in t.TripStops)
                {
                    foreach (var ta in s.TripActivities)
                    {
                        if (ta.ActivityId.HasValue && !addedActivitiesMap.ContainsKey(ta.ActivityId.Value))
                        {
                            addedActivitiesMap[ta.ActivityId.Value] = ta.TripActivityId;
                        }
                    }
                }
            }

            var activities = rawActivities.Select(a => new ActivityExploreItem
            {
                ActivityId = a.ActivityId,
                CityId = a.CityId,
                CityName = a.DestinationCity?.Name ?? "City",
                Country = a.DestinationCity?.Country ?? "",
                Region = a.DestinationCity?.Region ?? "",
                Title = a.Title,
                Description = a.Description,
                CategoryName = a.ActivityCategory?.CategoryName ?? "Sightseeing",
                EstimatedCost = a.EstimatedCost,
                DurationHours = a.DurationHours,
                Rating = a.Rating,
                IsTopPick = a.IsTopPick,
                ImageUrl = a.ImageUrl,
                IsAddedToUserTrip = addedActivitiesMap.ContainsKey(a.ActivityId),
                UserTripActivityId = addedActivitiesMap.ContainsKey(a.ActivityId) ? (int?)addedActivitiesMap[a.ActivityId] : null
            }).ToList();

            var model = new ExploreSearchViewModel
            {
                SearchQuery = q,
                SelectedRegion = region,
                SelectedCountry = country,
                SelectedCategory = cat,
                SelectedCityId = cityId,
                PriceRange = price,
                DurationRange = duration,
                SortBy = sort,
                ActiveTab = tab,
                Cities = cities,
                Activities = activities,
                Categories = categories,
                AllFilterCities = allCities,
                Regions = allRegions,
                Countries = allCountries,
                UserTrips = userTrips,
                AddedActivityIds = addedActivitiesMap.Keys.ToList()
            };

            return View(model);
        }

        // =====================================================================
        // 2. QUICK ADD ACTIVITY TO USER'S TRIP (AJAX)
        // =====================================================================
        [HttpPost]
        public async Task<JsonResult> AddActivityToTrip(int tripId, int activityId, int? tripStopId = null)
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
                return Json(new { success = false, message = "Trip not found or access denied." });
            }

            var activity = await db.Activities.Include(a => a.DestinationCity).Include(a => a.ActivityCategory).FirstOrDefaultAsync(a => a.ActivityId == activityId);
            if (activity == null)
            {
                return Json(new { success = false, message = "Activity not found." });
            }

            // Find target stop: explicit tripStopId, or matching city stop, or first stop, or create new stop
            TripStop stop = null;
            if (tripStopId.HasValue && tripStopId.Value > 0)
            {
                stop = trip.TripStops.FirstOrDefault(s => s.TripStopId == tripStopId.Value);
            }

            if (stop == null)
            {
                stop = trip.TripStops.FirstOrDefault(s => s.CityId == activity.CityId);
            }

            if (stop == null && trip.TripStops.Any())
            {
                stop = trip.TripStops.FirstOrDefault();
            }

            if (stop == null)
            {
                int nextOrder = trip.TripStops.Count + 1;
                stop = new TripStop
                {
                    TripId = tripId,
                    CityId = activity.CityId,
                    StopOrder = nextOrder,
                    ArrivalDate = trip.StartDate,
                    DepartureDate = trip.EndDate,
                    AccommodationCost = 120.00m,
                    AccommodationDetails = $"Stay in {activity.DestinationCity?.Name ?? "City"}",
                    TransportCost = 50.00m,
                    TransportMode = "Flight",
                    Notes = $"Section {nextOrder}: {activity.DestinationCity?.Name}",
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

            return Json(new { 
                success = true, 
                tripActivityId = tripAct.TripActivityId,
                activityId = activity.ActivityId,
                message = $"✨ Added '{activity.Title}' to '{trip.Title}'!" 
            });
        }

        // =====================================================================
        // 3. REMOVE ACTIVITY FROM USER'S TRIP (AJAX)
        // =====================================================================
        [HttpPost]
        public async Task<JsonResult> RemoveActivityFromTrip(int tripId, int activityId)
        {
            var userId = await GetResolvedUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please log in to manage your trips." });
            }

            var trip = await db.Trips
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.UserId == userId);

            if (trip == null)
            {
                return Json(new { success = false, message = "Trip not found." });
            }

            TripActivity foundActivity = null;
            foreach (var stop in trip.TripStops)
            {
                foundActivity = stop.TripActivities.FirstOrDefault(a => a.ActivityId == activityId);
                if (foundActivity != null) break;
            }

            if (foundActivity == null)
            {
                return Json(new { success = false, message = "Activity is not in this trip." });
            }

            string actTitle = foundActivity.CustomTitle;
            db.TripActivities.Remove(foundActivity);
            await db.SaveChangesAsync();

            return Json(new { 
                success = true, 
                activityId = activityId,
                message = $"🗑️ Removed '{actTitle}' from '{trip.Title}'." 
            });
        }

        // =====================================================================
        // 4. QUICK ADD CITY AS NEW SECTION TO USER'S TRIP (AJAX)
        // =====================================================================
        [HttpPost]
        public async Task<JsonResult> AddCityToTrip(
            int tripId, 
            int cityId, 
            DateTime? arrivalDate, 
            DateTime? departureDate, 
            decimal? accommodationCost, 
            decimal? transportCost, 
            string transportMode, 
            string notes)
        {
            var userId = await GetResolvedUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please log in to add destinations to your trips." });
            }

            var trip = await db.Trips
                .Include(t => t.TripStops)
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.UserId == userId);

            if (trip == null)
            {
                return Json(new { success = false, message = "Trip not found." });
            }

            var city = await db.DestinationCities.FindAsync(cityId);
            if (city == null)
            {
                return Json(new { success = false, message = "City not found." });
            }

            int nextOrder = trip.TripStops.Count + 1;
            var arr = arrivalDate ?? (trip.TripStops.Any() ? trip.TripStops.Max(s => s.DepartureDate) : trip.StartDate);
            var dep = departureDate ?? arr.AddDays(3);

            var newStop = new TripStop
            {
                TripId = tripId,
                CityId = cityId,
                StopOrder = nextOrder,
                ArrivalDate = arr,
                DepartureDate = dep,
                AccommodationCost = accommodationCost ?? (city.AvgDailyCost * 2),
                AccommodationDetails = $"Stay in {city.Name}",
                TransportCost = transportCost ?? 50.00m,
                TransportMode = string.IsNullOrEmpty(transportMode) ? "Flight" : transportMode,
                Notes = string.IsNullOrWhiteSpace(notes) ? $"Section {nextOrder}: Exploring {city.Name}, {city.Country}." : notes,
                CreatedAt = DateTime.UtcNow
            };

            db.TripStops.Add(newStop);
            await db.SaveChangesAsync();

            return Json(new { 
                success = true, 
                stopId = newStop.TripStopId, 
                message = $"🌍 Added '{city.Name}, {city.Country}' as Section #{nextOrder} to '{trip.Title}'!" 
            });
        }

        // =====================================================================
        // 5. QUICK VIEW DETAILS (AJAX MODAL DATA)
        // =====================================================================
        [HttpGet]
        public async Task<JsonResult> GetQuickView(string type, int id)
        {
            if (type == "city")
            {
                var city = await db.DestinationCities
                    .Include(c => c.Activities)
                    .FirstOrDefaultAsync(c => c.CityId == id);

                if (city == null) return Json(new { success = false, message = "City not found" }, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    success = true,
                    type = "city",
                    id = city.CityId,
                    name = city.Name,
                    country = city.Country,
                    region = city.Region,
                    description = city.Description,
                    imageUrl = city.ImageUrl,
                    costIndex = city.CostIndex,
                    popularityScore = city.PopularityScore,
                    avgDailyCost = city.AvgDailyCost,
                    currencyCode = city.CurrencyCode,
                    latitude = city.Latitude,
                    longitude = city.Longitude,
                    activitiesCount = city.Activities.Count,
                    activities = city.Activities.Take(4).Select(a => new { a.ActivityId, a.Title, a.EstimatedCost, a.DurationHours, a.Rating, a.ImageUrl }).ToList()
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var act = await db.Activities
                    .Include(a => a.DestinationCity)
                    .Include(a => a.ActivityCategory)
                    .FirstOrDefaultAsync(a => a.ActivityId == id);

                if (act == null) return Json(new { success = false, message = "Activity not found" }, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    success = true,
                    type = "activity",
                    id = act.ActivityId,
                    title = act.Title,
                    description = act.Description,
                    imageUrl = act.ImageUrl,
                    cityName = act.DestinationCity?.Name,
                    country = act.DestinationCity?.Country,
                    region = act.DestinationCity?.Region,
                    categoryName = act.ActivityCategory?.CategoryName ?? "Sightseeing",
                    categoryIcon = act.ActivityCategory?.IconClass ?? "fa-solid fa-compass",
                    estimatedCost = act.EstimatedCost,
                    durationHours = act.DurationHours,
                    rating = act.Rating,
                    isTopPick = act.IsTopPick
                }, JsonRequestBehavior.AllowGet);
            }
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
