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
    [Authorize]
    public class UserController : Controller
    {
        private readonly GlobeTrotterDBEntities1 db = new GlobeTrotterDBEntities1();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var userName = User?.Identity?.Name ?? "";
            // Strict role separation: Administrators belong strictly to /Admin
            if (userName.ToLower().Contains("admin"))
            {
                filterContext.Result = RedirectToAction("Index", "Admin");
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // Helper: Resolve valid user ID that exists in GlobeTrotterDB AspNetUsers table
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

        // =====================================================================
        // 1. USER DASHBOARD: GET /User or GET /User/Index
        // =====================================================================
        public async Task<ActionResult> Index()
        {
            var userId = await GetResolvedUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var userEntity = await db.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);

            // Fetch trips
            var trips = await db.Trips
                .Include(t => t.TripStops.Select(s => s.DestinationCity))
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .Include(t => t.TripExpenses)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            var today = DateTime.Today;
            var tripCards = new List<TripCardViewModel>();
            int totalCitiesCount = 0;
            decimal totalEstimatedCost = 0m;

            foreach (var t in trips)
            {
                decimal stayCost = t.TripStops.Sum(ts => ts.AccommodationCost);
                decimal transportCost = t.TripStops.Sum(ts => ts.TransportCost);
                decimal activityCost = t.TripStops.SelectMany(ts => ts.TripActivities).Sum(ta => ta.Cost);
                decimal miscCost = t.TripExpenses.Sum(te => te.Amount);
                decimal totalTripCost = stayCost + transportCost + activityCost + miscCost;
                totalEstimatedCost += totalTripCost;

                string tripStatus = t.TripStops.Count == 0 ? "Draft" 
                    : (t.StartDate > today ? "Upcoming" 
                    : (t.StartDate <= today && t.EndDate >= today ? "Active" : "Completed"));

                int progress = 20;
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
                Trips = tripCards,
                RecommendedCities = recommendedCities
            };

            return View(viewModel);
        }

        // =====================================================================
        // 2. USER TRIP MANAGEMENT: GET /User/Trip or GET /User/Trips
        // =====================================================================
        [ActionName("Trip")]
        public async Task<ActionResult> Trip(string status = "All", string search = "")
        {
            var userId = await GetResolvedUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var userEntity = await db.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);

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
                decimal stayCost = t.TripStops.Sum(ts => ts.AccommodationCost);
                decimal transportCost = t.TripStops.Sum(ts => ts.TransportCost);
                decimal activityCost = t.TripStops.SelectMany(ts => ts.TripActivities).Sum(ta => ta.Cost);
                decimal miscCost = t.TripExpenses.Sum(te => te.Amount);
                decimal totalTripCost = stayCost + transportCost + activityCost + miscCost;
                totalEstimatedCost += totalTripCost;

                string tripStatus = t.TripStops.Count == 0 ? "Draft" 
                    : (t.StartDate > today ? "Upcoming" 
                    : (t.StartDate <= today && t.EndDate >= today ? "Active" : "Completed"));

                int progress = 20;
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

            var filteredTripCards = tripCards;
            if (!string.IsNullOrEmpty(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filteredTripCards = tripCards.Where(c => c.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

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

        public async Task<ActionResult> Trips(string status = "All", string search = "")
        {
            return await Trip(status, search);
        }

        // =====================================================================
        // 3. PLAN TRIP (SCREEN 4): GET /User/Create
        // =====================================================================
        public async Task<ActionResult> Create()
        {
            var cities = await db.DestinationCities.OrderBy(c => c.Name).ToListAsync();
            var defaultCity = cities.FirstOrDefault();
            int defaultCityId = defaultCity != null ? defaultCity.CityId : 1;

            var suggestions = await db.Activities
                .Include(a => a.DestinationCity)
                .Include(a => a.ActivityCategory)
                .Where(a => a.CityId == defaultCityId)
                .Take(6)
                .Select(a => new ActivitySuggestionItem
                {
                    ActivityId = a.ActivityId,
                    CityId = a.CityId,
                    CityName = a.DestinationCity.Name,
                    Title = a.Title,
                    Category = a.ActivityCategory != null ? a.ActivityCategory.CategoryName : "Sightseeing",
                    Cost = a.EstimatedCost,
                    ImageUrl = a.ImageUrl,
                    DurationHours = a.DurationHours,
                    IsSelected = false
                })
                .ToListAsync();

            if (suggestions.Count < 6)
            {
                var extra = await db.Activities
                    .Include(a => a.DestinationCity)
                    .Include(a => a.ActivityCategory)
                    .Where(a => a.CityId != defaultCityId)
                    .Take(6 - suggestions.Count)
                    .Select(a => new ActivitySuggestionItem
                    {
                        ActivityId = a.ActivityId,
                        CityId = a.CityId,
                        CityName = a.DestinationCity.Name,
                        Title = a.Title,
                        Category = a.ActivityCategory != null ? a.ActivityCategory.CategoryName : "Sightseeing",
                        Cost = a.EstimatedCost,
                        ImageUrl = a.ImageUrl,
                        DurationHours = a.DurationHours,
                        IsSelected = false
                    })
                    .ToListAsync();
                suggestions.AddRange(extra);
            }

            var model = new CreateTripViewModel
            {
                Title = defaultCity != null ? $"{defaultCity.Name} Getaway & Exploration" : "My Dream Multi-City Journey",
                PlaceCityId = defaultCityId,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(14),
                TotalBudget = 2500.00m,
                Currency = "USD",
                CoverImageUrl = defaultCity?.ImageUrl ?? "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?auto=format&fit=crop&w=1200&q=80",
                AvailableCities = cities,
                Suggestions = suggestions
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateTripViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCities = await db.DestinationCities.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            var userId = await GetResolvedUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var selectedCity = await db.DestinationCities.FindAsync(model.PlaceCityId);
            string coverUrl = string.IsNullOrWhiteSpace(model.CoverImageUrl) 
                ? (selectedCity?.ImageUrl ?? "https://images.unsplash.com/photo-1488646953014-85cb44e25828?auto=format&fit=crop&w=1200&q=80")
                : model.CoverImageUrl;

            string baseSlug = (model.Title ?? "trip").ToLower().Replace(" ", "-").Replace("&", "and");
            string shareSlug = $"{baseSlug}-{Guid.NewGuid().ToString().Substring(0, 8)}";

            var trip = new Trip
            {
                UserId = userId,
                Title = model.Title,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                TotalBudget = model.TotalBudget,
                Currency = model.Currency ?? "USD",
                CoverImageUrl = coverUrl,
                IsPublic = model.IsPublic,
                ShareSlug = shareSlug,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Trips.Add(trip);
            await db.SaveChangesAsync();

            // Create Section 1 (Initial Stop)
            decimal stayCostEstimate = (selectedCity?.AvgDailyCost ?? 150m) * Math.Max(1, (model.EndDate - model.StartDate).Days);
            var initialStop = new TripStop
            {
                TripId = trip.TripId,
                CityId = model.PlaceCityId,
                StopOrder = 1,
                ArrivalDate = model.StartDate,
                DepartureDate = model.EndDate,
                AccommodationCost = stayCostEstimate,
                AccommodationDetails = $"Stay in {selectedCity?.Name ?? "City"} (Est. ${selectedCity?.AvgDailyCost ?? 150:N0}/night)",
                TransportCost = 150.00m,
                TransportMode = "Flight",
                Notes = $"Section 1 of {trip.Title}. Explore major landmarks, local cuisine, and culture."
            };

            db.TripStops.Add(initialStop);
            await db.SaveChangesAsync();

            // Attach pre-selected activities
            if (!string.IsNullOrEmpty(model.SelectedActivityIdsJson))
            {
                try
                {
                    var activityIds = model.SelectedActivityIdsJson
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(idStr => int.TryParse(idStr.Trim(), out int val) ? val : 0)
                        .Where(id => id > 0)
                        .Distinct()
                        .ToList();

                    int actIndex = 1;
                    foreach (var actId in activityIds)
                    {
                        var activityObj = await db.Activities.Include(a => a.ActivityCategory).FirstOrDefaultAsync(a => a.ActivityId == actId);
                        if (activityObj != null)
                        {
                            db.TripActivities.Add(new TripActivity
                            {
                                TripStopId = initialStop.TripStopId,
                                ActivityId = activityObj.ActivityId,
                                CustomTitle = activityObj.Title,
                                CategoryName = activityObj.ActivityCategory != null ? activityObj.ActivityCategory.CategoryName : "Sightseeing",
                                Cost = activityObj.EstimatedCost,
                                DurationHours = activityObj.DurationHours,
                                OrderIndex = actIndex++,
                                Notes = "Selected during trip creation",
                                IsCompleted = false
                            });
                        }
                    }
                    await db.SaveChangesAsync();
                }
                catch { }
            }

            TempData["SuccessMessage"] = $"✨ Trip '{trip.Title}' created successfully! Now build your detailed section itinerary.";
            return RedirectToAction("BuildItinerary", "User", new { id = trip.TripId });
        }

        // =====================================================================
        // 4. BUILD ITINERARY (SCREEN 5): GET /User/BuildItinerary/{id}
        // =====================================================================
        public async Task<ActionResult> BuildItinerary(int id)
        {
            var trip = await db.Trips
                .Include(t => t.TripStops.Select(s => s.DestinationCity))
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip == null)
            {
                TempData["ErrorMessage"] = "Trip not found.";
                return RedirectToAction("Trip", "User");
            }

            var orderedStops = trip.TripStops.OrderBy(s => s.StopOrder).ToList();
            var sections = new List<ItinerarySectionViewModel>();
            decimal totalCalculatedCost = 0;
            int sectionCounter = 1;

            foreach (var stop in orderedStops)
            {
                decimal sectionTotal = stop.AccommodationCost + stop.TransportCost;
                decimal actTotal = stop.TripActivities.Sum(a => a.Cost);
                sectionTotal += actTotal;
                totalCalculatedCost += sectionTotal;

                sections.Add(new ItinerarySectionViewModel
                {
                    TripStopId = stop.TripStopId,
                    SectionNumber = sectionCounter++,
                    PlaceName = stop.DestinationCity?.Name ?? "Stop " + stop.StopOrder,
                    Country = stop.DestinationCity?.Country ?? "",
                    CityImageUrl = stop.DestinationCity?.ImageUrl,
                    ArrivalDate = stop.ArrivalDate,
                    DepartureDate = stop.DepartureDate,
                    SectionBudget = sectionTotal,
                    AccommodationCost = stop.AccommodationCost,
                    AccommodationDetails = stop.AccommodationDetails,
                    TransportCost = stop.TransportCost,
                    TransportMode = stop.TransportMode,
                    Notes = stop.Notes,
                    Activities = stop.TripActivities.OrderBy(a => a.OrderIndex).ToList()
                });
            }

            var availableCities = await db.DestinationCities.OrderBy(c => c.Name).ToListAsync();
            var availableActivities = await db.Activities.OrderBy(a => a.Title).ToListAsync();
            var user = await db.AspNetUsers.FindAsync(trip.UserId);

            var viewModel = new BuildItineraryViewModel
            {
                TripId = trip.TripId,
                Title = trip.Title,
                Description = trip.Description,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                TotalBudget = trip.TotalBudget,
                Currency = trip.Currency ?? "USD",
                TotalCalculatedCost = totalCalculatedCost,
                CoverImageUrl = trip.CoverImageUrl,
                ShareSlug = trip.ShareSlug,
                IsPublic = trip.IsPublic,
                OwnerName = user?.FullName ?? user?.UserName ?? "Traveler",
                OwnerAvatarUrl = user?.AvatarUrl ?? "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=120&q=80",
                Sections = sections,
                AvailableCities = availableCities,
                AvailableActivities = availableActivities
            };

            return View(viewModel);
        }

        // =====================================================================
        // 5. SECTION & ACTIVITY MANAGEMENT
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddSection(AddSectionViewModel model)
        {
            var trip = await db.Trips.Include(t => t.TripStops).FirstOrDefaultAsync(t => t.TripId == model.TripId);
            if (trip == null) return HttpNotFound();

            int nextOrder = trip.TripStops.Count > 0 ? trip.TripStops.Max(s => s.StopOrder) + 1 : 1;
            var city = await db.DestinationCities.FindAsync(model.CityId);

            var stop = new TripStop
            {
                TripId = model.TripId,
                CityId = model.CityId,
                StopOrder = nextOrder,
                ArrivalDate = model.ArrivalDate,
                DepartureDate = model.DepartureDate,
                AccommodationCost = model.AccommodationCost,
                AccommodationDetails = string.IsNullOrWhiteSpace(model.AccommodationDetails)
                    ? $"Stay in {city?.Name ?? "City"}"
                    : model.AccommodationDetails,
                TransportCost = model.TransportCost,
                TransportMode = model.TransportMode ?? "Flight",
                Notes = model.Notes
            };

            db.TripStops.Add(stop);
            if (model.DepartureDate > trip.EndDate) trip.EndDate = model.DepartureDate;

            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Added Section {nextOrder} ({city?.Name}) to your itinerary!";
            return RedirectToAction("BuildItinerary", new { id = model.TripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteSection(int stopId, int tripId)
        {
            var stop = await db.TripStops.Include(s => s.TripActivities).FirstOrDefaultAsync(s => s.TripStopId == stopId && s.TripId == tripId);
            if (stop != null)
            {
                db.TripActivities.RemoveRange(stop.TripActivities);
                db.TripStops.Remove(stop);
                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Section removed successfully.";
            }
            return RedirectToAction("BuildItinerary", new { id = tripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddActivityToSection(int tripId, AddActivityToSectionViewModel model)
        {
            var stop = await db.TripStops.Include(s => s.TripActivities).FirstOrDefaultAsync(s => s.TripStopId == model.TripStopId);
            if (stop == null) return HttpNotFound();

            int nextOrder = stop.TripActivities.Count > 0 ? stop.TripActivities.Max(a => a.OrderIndex) + 1 : 1;
            var activity = new TripActivity
            {
                TripStopId = model.TripStopId,
                CustomTitle = model.Title,
                CategoryName = model.CategoryName ?? "Sightseeing",
                Cost = model.Cost,
                DurationHours = model.DurationHours,
                OrderIndex = nextOrder,
                Notes = model.Notes,
                IsCompleted = false
            };

            db.TripActivities.Add(activity);
            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Activity '{model.Title}' added to section!";
            return RedirectToAction("BuildItinerary", new { id = tripId });
        }

        // =====================================================================
        // 6. EDIT, DUPLICATE, DELETE
        // =====================================================================
        public async Task<ActionResult> Edit(int id)
        {
            var trip = await db.Trips.FindAsync(id);
            if (trip == null) return HttpNotFound();

            var cities = await db.DestinationCities.OrderBy(c => c.Name).ToListAsync();
            var firstStop = await db.TripStops.FirstOrDefaultAsync(s => s.TripId == id);

            var model = new EditTripViewModel
            {
                TripId = trip.TripId,
                Title = trip.Title,
                PlaceCityId = firstStop != null ? firstStop.CityId : (cities.FirstOrDefault()?.CityId ?? 1),
                Description = trip.Description,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                TotalBudget = trip.TotalBudget,
                Currency = trip.Currency ?? "USD",
                CoverImageUrl = trip.CoverImageUrl,
                IsPublic = trip.IsPublic,
                AvailableCities = cities
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditTripViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCities = await db.DestinationCities.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            var trip = await db.Trips.FindAsync(model.TripId);
            if (trip == null) return HttpNotFound();

            trip.Title = model.Title;
            trip.Description = model.Description;
            trip.StartDate = model.StartDate;
            trip.EndDate = model.EndDate;
            trip.TotalBudget = model.TotalBudget;
            trip.Currency = model.Currency;
            trip.CoverImageUrl = model.CoverImageUrl;
            trip.IsPublic = model.IsPublic;
            trip.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Trip details updated successfully.";
            return RedirectToAction("Trip", "User");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var trip = await db.Trips
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .Include(t => t.TripExpenses)
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip != null)
            {
                foreach (var stop in trip.TripStops.ToList())
                {
                    db.TripActivities.RemoveRange(stop.TripActivities);
                }
                db.TripStops.RemoveRange(trip.TripStops);
                db.TripExpenses.RemoveRange(trip.TripExpenses);
                db.Trips.Remove(trip);
                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Trip deleted successfully.";
            }

            return RedirectToAction("Trip", "User");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Duplicate(int id)
        {
            var userId = await GetResolvedUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var originalTrip = await db.Trips
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .Include(t => t.TripExpenses)
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (originalTrip == null) return HttpNotFound();

            var newTrip = new Trip
            {
                UserId = userId,
                Title = "Copy of " + originalTrip.Title,
                Description = originalTrip.Description,
                StartDate = originalTrip.StartDate,
                EndDate = originalTrip.EndDate,
                TotalBudget = originalTrip.TotalBudget,
                Currency = originalTrip.Currency,
                CoverImageUrl = originalTrip.CoverImageUrl,
                IsPublic = false,
                ShareSlug = $"copy-{Guid.NewGuid().ToString().Substring(0, 8)}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Trips.Add(newTrip);
            await db.SaveChangesAsync();

            foreach (var stop in originalTrip.TripStops.OrderBy(s => s.StopOrder))
            {
                var newStop = new TripStop
                {
                    TripId = newTrip.TripId,
                    CityId = stop.CityId,
                    StopOrder = stop.StopOrder,
                    ArrivalDate = stop.ArrivalDate,
                    DepartureDate = stop.DepartureDate,
                    AccommodationCost = stop.AccommodationCost,
                    AccommodationDetails = stop.AccommodationDetails,
                    TransportCost = stop.TransportCost,
                    TransportMode = stop.TransportMode,
                    Notes = stop.Notes
                };
                db.TripStops.Add(newStop);
                await db.SaveChangesAsync();

                foreach (var act in stop.TripActivities.OrderBy(a => a.OrderIndex))
                {
                    db.TripActivities.Add(new TripActivity
                    {
                        TripStopId = newStop.TripStopId,
                        ActivityId = act.ActivityId,
                        CustomTitle = act.CustomTitle,
                        CategoryName = act.CategoryName,
                        Cost = act.Cost,
                        DurationHours = act.DurationHours,
                        OrderIndex = act.OrderIndex,
                        Notes = act.Notes,
                        IsCompleted = false
                    });
                }
            }

            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Cloned '{originalTrip.Title}' into your trips!";
            return RedirectToAction("BuildItinerary", "User", new { id = newTrip.TripId });
        }

        // =====================================================================
        // 7. AJAX SUGGESTIONS
        // =====================================================================
        [HttpGet]
        public async Task<JsonResult> GetCityActivities(int cityId)
        {
            var activities = await db.Activities
                .Include(a => a.DestinationCity)
                .Include(a => a.ActivityCategory)
                .Where(a => a.CityId == cityId)
                .Take(6)
                .Select(a => new
                {
                    ActivityId = a.ActivityId,
                    CityId = a.CityId,
                    CityName = a.DestinationCity.Name,
                    Title = a.Title,
                    Category = a.ActivityCategory != null ? a.ActivityCategory.CategoryName : "Sightseeing",
                    Cost = a.EstimatedCost,
                    ImageUrl = a.ImageUrl,
                    DurationHours = a.DurationHours
                })
                .ToListAsync();

            var city = await db.DestinationCities.FindAsync(cityId);

            return Json(new
            {
                success = true,
                cityName = city?.Name,
                cityImage = city?.ImageUrl,
                avgDailyCost = city?.AvgDailyCost,
                activities = activities
            }, JsonRequestBehavior.AllowGet);
        }

        // =====================================================================
        // 8. TRIP CALENDAR & VERTICAL TIMELINE VIEW: GET /User/Calendar
        // =====================================================================
        public async Task<ActionResult> Calendar(int? tripId, string view = "timeline")
        {
            var userId = await GetResolvedUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var allUserTrips = await db.Trips
                .Include(t => t.TripStops.Select(s => s.DestinationCity))
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            if (allUserTrips.Count == 0)
            {
                return View(new TripCalendarViewModel
                {
                    AllUserTrips = new List<Trip>(),
                    Days = new List<CalendarDayViewModel>(),
                    ActiveViewMode = view ?? "timeline"
                });
            }

            // Determine selected trip
            Trip selectedTrip = null;
            if (tripId.HasValue)
            {
                selectedTrip = allUserTrips.FirstOrDefault(t => t.TripId == tripId.Value);
            }
            if (selectedTrip == null)
            {
                var today = DateTime.Today;
                selectedTrip = allUserTrips.FirstOrDefault(t => t.StartDate >= today) ?? allUserTrips.FirstOrDefault();
            }

            // Build day-by-day plan from StartDate to EndDate
            int totalDays = Math.Max(1, (selectedTrip.EndDate - selectedTrip.StartDate).Days + 1);
            var daysList = new List<CalendarDayViewModel>();
            var orderedStops = selectedTrip.TripStops.OrderBy(s => s.StopOrder).ToList();

            decimal grandCalculatedCost = 0m;
            int totalActivitiesCount = 0;
            int totalCompletedActivities = 0;

            for (int i = 0; i < totalDays; i++)
            {
                var currentDayDate = selectedTrip.StartDate.AddDays(i);

                // Find active stop for this day
                var activeStop = orderedStops.FirstOrDefault(s => currentDayDate >= s.ArrivalDate && currentDayDate <= s.DepartureDate);
                if (activeStop == null)
                {
                    // Fallback to nearest stop based on order
                    activeStop = orderedStops.FirstOrDefault();
                }

                bool isFirstDay = activeStop != null && currentDayDate.Date == activeStop.ArrivalDate.Date;
                int stopDaysCount = activeStop != null ? Math.Max(1, (activeStop.DepartureDate - activeStop.ArrivalDate).Days + 1) : 1;
                decimal dailyStayCost = activeStop != null ? (activeStop.AccommodationCost / stopDaysCount) : 0m;
                decimal dayTransportCost = isFirstDay && activeStop != null ? activeStop.TransportCost : 0m;

                // Activities for this stop
                var stopActivities = activeStop != null ? activeStop.TripActivities.OrderBy(a => a.OrderIndex).ToList() : new List<TripActivity>();

                decimal dayActivitiesCost = stopActivities.Sum(a => a.Cost);
                decimal dailyBudgetTotal = dailyStayCost + dayTransportCost + dayActivitiesCost;

                grandCalculatedCost += dailyBudgetTotal;
                totalActivitiesCount += stopActivities.Count;
                totalCompletedActivities += stopActivities.Count(a => a.IsCompleted);

                daysList.Add(new CalendarDayViewModel
                {
                    DayNumber = i + 1,
                    Date = currentDayDate,
                    StopId = activeStop?.TripStopId ?? 0,
                    StopOrder = activeStop?.StopOrder ?? 1,
                    CityName = activeStop?.DestinationCity?.Name ?? "Transit Stop",
                    Country = activeStop?.DestinationCity?.Country ?? "",
                    CityImageUrl = activeStop?.DestinationCity?.ImageUrl ?? "https://images.unsplash.com/photo-1488646953014-85cb44e25828?auto=format&fit=crop&w=600&q=80",
                    AccommodationDetails = activeStop?.AccommodationDetails ?? "Standard Lodging",
                    AccommodationCostPerDay = dailyStayCost,
                    TransportMode = activeStop?.TransportMode ?? "Flight",
                    TransportCost = dayTransportCost,
                    Notes = activeStop?.Notes ?? "Explore city highlights and local culinary spots.",
                    IsFirstDayOfStop = isFirstDay,
                    IsTransitDay = isFirstDay && (activeStop?.StopOrder > 1),
                    DailyBudgetTotal = dailyBudgetTotal,
                    Activities = stopActivities
                });
            }

            var viewModel = new TripCalendarViewModel
            {
                SelectedTripId = selectedTrip.TripId,
                SelectedTrip = selectedTrip,
                AllUserTrips = allUserTrips,
                Days = daysList,
                TotalDays = totalDays,
                TotalCalculatedCost = grandCalculatedCost,
                TotalActivitiesCount = totalActivitiesCount,
                TotalCompletedActivities = totalCompletedActivities,
                ActiveViewMode = view ?? "timeline"
            };

            return View(viewModel);
        }

        public async Task<ActionResult> Timeline(int? tripId)
        {
            return await Calendar(tripId, "timeline");
        }

        // =====================================================================
        // 9. AJAX ACTIVITY QUICK EDIT, REORDER & TOGGLE
        // =====================================================================
        [HttpPost]
        public async Task<JsonResult> UpdateActivityOrder(int activityId, int newOrderIndex)
        {
            var activity = await db.TripActivities.FindAsync(activityId);
            if (activity == null) return Json(new { success = false, message = "Activity not found" });

            activity.OrderIndex = newOrderIndex;
            await db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<JsonResult> QuickEditActivity(QuickEditActivityViewModel model)
        {
            var activity = await db.TripActivities.FindAsync(model.TripActivityId);
            if (activity == null) return Json(new { success = false, message = "Activity not found" });

            if (!string.IsNullOrWhiteSpace(model.Title)) activity.CustomTitle = model.Title;
            if (!string.IsNullOrWhiteSpace(model.CategoryName)) activity.CategoryName = model.CategoryName;
            activity.Cost = model.Cost;
            activity.DurationHours = model.DurationHours;
            if (!string.IsNullOrWhiteSpace(model.Notes)) activity.Notes = model.Notes;
            if (!string.IsNullOrWhiteSpace(model.TimeOfDay)) activity.TimeOfDay = model.TimeOfDay;
            activity.IsCompleted = model.IsCompleted;

            await db.SaveChangesAsync();
            return Json(new { 
                success = true, 
                title = activity.CustomTitle, 
                cost = activity.Cost,
                category = activity.CategoryName,
                duration = activity.DurationHours,
                isCompleted = activity.IsCompleted
            });
        }

        [HttpPost]
        public async Task<JsonResult> ToggleActivityStatus(int activityId)
        {
            var activity = await db.TripActivities.FindAsync(activityId);
            if (activity == null) return Json(new { success = false, message = "Activity not found" });

            activity.IsCompleted = !activity.IsCompleted;
            await db.SaveChangesAsync();
            return Json(new { success = true, isCompleted = activity.IsCompleted });
        }

        [HttpPost]
        public async Task<JsonResult> DeleteActivityAjax(int activityId)
        {
            var activity = await db.TripActivities.FindAsync(activityId);
            if (activity == null) return Json(new { success = false, message = "Activity not found" });

            db.TripActivities.Remove(activity);
            await db.SaveChangesAsync();
            return Json(new { success = true });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}

