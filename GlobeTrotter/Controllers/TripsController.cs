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

            // Fallback to demo user or first user in AspNetUsers
            var fallback = await db.AspNetUsers.FirstOrDefaultAsync(u => u.Id == "demo-user-001") 
                        ?? await db.AspNetUsers.FirstOrDefaultAsync();

            return fallback != null ? fallback.Id : "demo-user-001";
        }

        // =====================================================================
        // 1. DASHBOARD & MY TRIPS HUB -> REDIRECT TO /User/Trip
        // =====================================================================
        public ActionResult Index(string status = "All", string search = "")
        {
            return RedirectToAction("Trip", "User", new { status = status, search = search });
        }

        // =====================================================================
        // 2. CREATE TRIP (SCREEN 4 - PLAN A NEW TRIP WITH 6 SUGGESTIONS)
        // =====================================================================
        public async Task<ActionResult> Create()
        {
            var cities = await db.DestinationCities.OrderBy(c => c.Name).ToListAsync();
            var defaultCity = cities.FirstOrDefault();
            int defaultCityId = defaultCity != null ? defaultCity.CityId : 1;

            // Load 6 initial suggestions (places / activities)
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

            // If less than 6 in that city, fallback to top activities
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
            if (model.StartDate > model.EndDate)
            {
                ModelState.AddModelError("EndDate", "End date must be on or after start date.");
            }

            if (ModelState.IsValid)
            {
                var userId = await GetResolvedUserIdAsync();
                var selectedCity = await db.DestinationCities.FindAsync(model.PlaceCityId);

                string slug = Guid.NewGuid().ToString("N").Substring(0, 12);
                string coverUrl = !string.IsNullOrWhiteSpace(model.CoverImageUrl) 
                    ? model.CoverImageUrl 
                    : (selectedCity?.ImageUrl ?? "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?auto=format&fit=crop&w=1200&q=80");

                // 1. Create Trip Record
                var trip = new Trip
                {
                    UserId = userId,
                    Title = model.Title,
                    Description = model.Description ?? $"Trip exploring {selectedCity?.Name ?? "destinations"} and surrounding highlights.",
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    TotalBudget = model.TotalBudget > 0 ? model.TotalBudget : 2500.00m,
                    Currency = model.Currency ?? "USD",
                    CoverImageUrl = coverUrl,
                    IsPublic = model.IsPublic,
                    ShareSlug = slug,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.Trips.Add(trip);
                await db.SaveChangesAsync();

                // 2. Automatically Create Section 1 (Initial Stop)
                if (selectedCity != null)
                {
                    int durationDays = Math.Max(1, (model.EndDate - model.StartDate).Days);
                    var initialStop = new TripStop
                    {
                        TripId = trip.TripId,
                        CityId = selectedCity.CityId,
                        StopOrder = 1,
                        ArrivalDate = model.StartDate,
                        DepartureDate = model.EndDate,
                        AccommodationCost = selectedCity.AvgDailyCost * durationDays,
                        AccommodationDetails = $"Recommended Stay in {selectedCity.Name} (Avg ${selectedCity.AvgDailyCost:N0}/day)",
                        TransportCost = 150.00m,
                        TransportMode = "Flight / Train",
                        Notes = $"Section 1: Initial stay & explorations across {selectedCity.Name}, {selectedCity.Country}.",
                        CreatedAt = DateTime.UtcNow
                    };

                    db.TripStops.Add(initialStop);
                    await db.SaveChangesAsync();

                    // 3. Attach any pre-selected suggestion activities from Screen 4
                    if (!string.IsNullOrWhiteSpace(model.SelectedActivityIdsJson))
                    {
                        try
                        {
                            var selectedIds = model.SelectedActivityIdsJson
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(int.Parse)
                                .Distinct()
                                .ToList();

                            int actOrder = 1;
                            foreach (var actId in selectedIds)
                            {
                                var dbAct = await db.Activities.Include(a => a.ActivityCategory).FirstOrDefaultAsync(a => a.ActivityId == actId);
                                if (dbAct != null)
                                {
                                    var tripAct = new TripActivity
                                    {
                                        TripStopId = initialStop.TripStopId,
                                        ActivityId = dbAct.ActivityId,
                                        CustomTitle = dbAct.Title,
                                        CategoryName = dbAct.ActivityCategory != null ? dbAct.ActivityCategory.CategoryName : "Sightseeing",
                                        ScheduledDate = model.StartDate.AddDays(Math.Min(actOrder, durationDays - 1)),
                                        TimeOfDay = "Morning",
                                        Cost = dbAct.EstimatedCost,
                                        DurationHours = dbAct.DurationHours,
                                        OrderIndex = actOrder++,
                                        Notes = dbAct.Description,
                                        IsCompleted = false,
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    db.TripActivities.Add(tripAct);
                                }
                            }
                            await db.SaveChangesAsync();
                        }
                        catch { /* Ignore JSON parse if malformed */ }
                    }
                }

                TempData["SuccessMessage"] = $"✨ Trip '{trip.Title}' initiated! Now customize your itinerary sections below.";
                return RedirectToAction("BuildItinerary", new { id = trip.TripId });
            }

            // Reload dropdown and suggestions if invalid
            model.AvailableCities = await db.DestinationCities.OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        // =====================================================================
        // AJAX: GET 6 SUGGESTIONS WHEN PLACE CHANGES (SCREEN 4)
        // =====================================================================
        [HttpGet]
        public async Task<JsonResult> GetSuggestionsForPlace(int cityId)
        {
            var city = await db.DestinationCities.FindAsync(cityId);
            var activities = await db.Activities
                .Include(a => a.DestinationCity)
                .Include(a => a.ActivityCategory)
                .Where(a => a.CityId == cityId)
                .Take(6)
                .Select(a => new
                {
                    a.ActivityId,
                    a.CityId,
                    CityName = a.DestinationCity.Name,
                    a.Title,
                    Category = a.ActivityCategory != null ? a.ActivityCategory.CategoryName : "Sightseeing",
                    Cost = a.EstimatedCost,
                    a.ImageUrl,
                    a.DurationHours
                })
                .ToListAsync();

            // If less than 6, fill with top activities
            if (activities.Count < 6)
            {
                var extra = await db.Activities
                    .Include(a => a.DestinationCity)
                    .Include(a => a.ActivityCategory)
                    .Where(a => a.CityId != cityId)
                    .Take(6 - activities.Count)
                    .Select(a => new
                    {
                        a.ActivityId,
                        a.CityId,
                        CityName = a.DestinationCity.Name,
                        a.Title,
                        Category = a.ActivityCategory != null ? a.ActivityCategory.CategoryName : "Sightseeing",
                        Cost = a.EstimatedCost,
                        a.ImageUrl,
                        a.DurationHours
                    })
                    .ToListAsync();
                activities.AddRange(extra);
            }

            return Json(new { cityImageUrl = city?.ImageUrl, cityName = city?.Name, suggestions = activities }, JsonRequestBehavior.AllowGet);
        }

        // =====================================================================
        // 3. BUILD ITINERARY (SCREEN 5 - MULTI-SECTION WORKFLOW)
        // =====================================================================
        public async Task<ActionResult> BuildItinerary(int id)
        {
            var userId = await GetResolvedUserIdAsync();
            var trip = await db.Trips
                .Include(t => t.TripStops.Select(s => s.DestinationCity))
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .FirstOrDefaultAsync(t => t.TripId == id && t.UserId == userId);

            if (trip == null)
            {
                return HttpNotFound("Trip not found or unauthorized.");
            }

            var sections = new List<ItinerarySectionViewModel>();
            int sectionNum = 1;
            decimal totalCalculatedCost = 0m;

            var orderedStops = trip.TripStops.OrderBy(s => s.StopOrder).ToList();
            foreach (var stop in orderedStops)
            {
                decimal actCost = stop.TripActivities.Sum(a => a.Cost);
                decimal sectionTotal = stop.AccommodationCost + stop.TransportCost + actCost;
                totalCalculatedCost += sectionTotal;

                sections.Add(new ItinerarySectionViewModel
                {
                    TripStopId = stop.TripStopId,
                    SectionNumber = sectionNum++,
                    PlaceName = stop.DestinationCity?.Name ?? "Destination Stop",
                    Country = stop.DestinationCity?.Country ?? "",
                    CityImageUrl = stop.DestinationCity?.ImageUrl ?? "https://images.unsplash.com/photo-1488646953014-85cb44e25828?auto=format&fit=crop&w=400&q=80",
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
        // PUBLIC SHARE & COMMUNITY ITINERARY VIEW
        // =====================================================================
        [AllowAnonymous]
        public async Task<ActionResult> Share(int? id, string slug)
        {
            var query = db.Trips
                .Include(t => t.TripStops.Select(s => s.DestinationCity))
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .AsQueryable();

            Trip trip = null;
            if (!string.IsNullOrEmpty(slug))
            {
                trip = await query.FirstOrDefaultAsync(t => t.ShareSlug == slug);
            }
            else if (id.HasValue)
            {
                trip = await query.FirstOrDefaultAsync(t => t.TripId == id.Value);
            }

            if (trip == null)
            {
                return HttpNotFound("The requested travel itinerary was not found or is private.");
            }

            var sections = new List<ItinerarySectionViewModel>();
            int sectionNum = 1;
            decimal totalCalculatedCost = 0m;

            var orderedStops = trip.TripStops.OrderBy(s => s.StopOrder).ToList();
            foreach (var stop in orderedStops)
            {
                decimal actCost = stop.TripActivities.Sum(a => a.Cost);
                decimal sectionTotal = stop.AccommodationCost + stop.TransportCost + actCost;
                totalCalculatedCost += sectionTotal;

                sections.Add(new ItinerarySectionViewModel
                {
                    TripStopId = stop.TripStopId,
                    SectionNumber = sectionNum++,
                    PlaceName = stop.DestinationCity?.Name ?? "Destination Stop",
                    Country = stop.DestinationCity?.Country ?? "",
                    CityImageUrl = stop.DestinationCity?.ImageUrl ?? "https://images.unsplash.com/photo-1488646953014-85cb44e25828?auto=format&fit=crop&w=400&q=80",
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
                OwnerName = user?.FullName ?? user?.UserName ?? "Globetrotter Traveler",
                OwnerAvatarUrl = user?.AvatarUrl ?? "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=120&q=80",
                Sections = sections
            };

            return View(viewModel);
        }

        // =====================================================================
        // 4. ADD SECTION TO TRIP (SCREEN 5: + ADD ANOTHER SECTION)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddSection(AddSectionViewModel model)
        {
            var userId = await GetResolvedUserIdAsync();
            var trip = await db.Trips.Include(t => t.TripStops).FirstOrDefaultAsync(t => t.TripId == model.TripId && t.UserId == userId);
            if (trip == null)
            {
                return HttpNotFound("Trip not found or unauthorized.");
            }

            var city = await db.DestinationCities.FindAsync(model.CityId);
            int nextOrder = trip.TripStops.Count + 1;

            var newStop = new TripStop
            {
                TripId = model.TripId,
                CityId = model.CityId,
                StopOrder = nextOrder,
                ArrivalDate = model.ArrivalDate,
                DepartureDate = model.DepartureDate,
                AccommodationCost = model.AccommodationCost > 0 ? model.AccommodationCost : (city?.AvgDailyCost ?? 120m) * Math.Max(1, (model.DepartureDate - model.ArrivalDate).Days),
                AccommodationDetails = !string.IsNullOrWhiteSpace(model.AccommodationDetails) ? model.AccommodationDetails : $"Hotel / Stay in {city?.Name ?? "City"}",
                TransportCost = model.TransportCost > 0 ? model.TransportCost : 80.00m,
                TransportMode = !string.IsNullOrWhiteSpace(model.TransportMode) ? model.TransportMode : "Train",
                Notes = !string.IsNullOrWhiteSpace(model.Notes) ? model.Notes : $"Section {nextOrder}: Travel & exploration of {city?.Name ?? "City"}.",
                CreatedAt = DateTime.UtcNow
            };

            db.TripStops.Add(newStop);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✨ Section {nextOrder} ({city?.Name}) added to itinerary!";
            return RedirectToAction("BuildItinerary", new { id = model.TripId });
        }

        // =====================================================================
        // 5. DELETE SECTION FROM TRIP
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteSection(int stopId, int tripId)
        {
            var userId = await GetResolvedUserIdAsync();
            var stop = await db.TripStops.Include(s => s.TripActivities).FirstOrDefaultAsync(s => s.TripStopId == stopId && s.Trip.UserId == userId);
            if (stop != null)
            {
                db.TripActivities.RemoveRange(stop.TripActivities);
                db.TripStops.Remove(stop);
                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Section removed from itinerary.";
            }

            return RedirectToAction("BuildItinerary", new { id = tripId });
        }

        // =====================================================================
        // 6. ADD ACTIVITY TO SECTION
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddActivityToSection(AddActivityToSectionViewModel model, int tripId)
        {
            var userId = await GetResolvedUserIdAsync();
            var stop = await db.TripStops.FirstOrDefaultAsync(s => s.TripStopId == model.TripStopId && s.Trip.UserId == userId);
            if (stop == null)
            {
                return HttpNotFound("Section not found.");
            }

            var tripAct = new TripActivity
            {
                TripStopId = model.TripStopId,
                CustomTitle = model.Title,
                CategoryName = model.CategoryName ?? "Sightseeing",
                ScheduledDate = stop.ArrivalDate,
                TimeOfDay = "Afternoon",
                Cost = model.Cost,
                DurationHours = model.DurationHours > 0 ? model.DurationHours : 2.0m,
                OrderIndex = stop.TripActivities.Count + 1,
                Notes = model.Notes,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            db.TripActivities.Add(tripAct);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Added '{model.Title}' to section activities!";
            return RedirectToAction("BuildItinerary", new { id = tripId });
        }

        // =====================================================================
        // 7. EDIT TRIP METADATA
        // =====================================================================
        public async Task<ActionResult> Edit(int id)
        {
            var userId = await GetResolvedUserIdAsync();
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
                var userId = await GetResolvedUserIdAsync();
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
        // 8. DELETE TRIP (AJAX / POST)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = await GetResolvedUserIdAsync();
            var trip = await db.Trips
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .FirstOrDefaultAsync(t => t.TripId == id && t.UserId == userId);

            if (trip == null)
            {
                return Json(new { success = false, message = "Trip not found or unauthorized." });
            }

            // Remove activities & stops
            foreach (var stop in trip.TripStops.ToList())
            {
                db.TripActivities.RemoveRange(stop.TripActivities);
            }
            db.TripStops.RemoveRange(trip.TripStops);
            db.Trips.Remove(trip);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trip removed from your dashboard.";
            return RedirectToAction("Index");
        }

        // =====================================================================
        // 9. CLONE / FORK TRIP
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Duplicate(int id)
        {
            var userId = await GetResolvedUserIdAsync();
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
        // 10. QUICK ADD CITY STOP TO TRIP (AJAX)
        // =====================================================================
        [HttpPost]
        public async Task<JsonResult> QuickAddCityStop(int tripId, int cityId)
        {
            var userId = await GetResolvedUserIdAsync();
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
                Notes = $"Section {nextOrder}: Added from city catalog: {city.Name}",
                CreatedAt = DateTime.UtcNow
            };

            db.TripStops.Add(stop);
            await db.SaveChangesAsync();

            return Json(new { success = true, message = $"✨ {city.Name} added to '{trip.Title}' as Section #{nextOrder}!" }, JsonRequestBehavior.AllowGet);
        }

        // =====================================================================
        // 11. GET USER'S TRIPS LIST (JSON for Modals / Dropdowns)
        // =====================================================================
        [HttpGet]
        public async Task<JsonResult> GetMyTripsJson()
        {
            var userId = await GetResolvedUserIdAsync();
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
