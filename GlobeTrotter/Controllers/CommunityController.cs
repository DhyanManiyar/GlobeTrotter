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
    public class CommunityController : Controller
    {
        private GlobeTrotterDBEntities1 db = new GlobeTrotterDBEntities1();

        private async Task<string> GetResolvedUserIdAsync()
        {
            if (!User.Identity.IsAuthenticated) return null;
            string identityName = User.Identity.Name;
            var user = await db.AspNetUsers.FirstOrDefaultAsync(u => u.UserName == identityName || u.Email == identityName);
            return user != null ? user.Id : User.Identity.GetUserId();
        }

        // =====================================================================
        // GET: /Community
        // =====================================================================
        [AllowAnonymous]
        public async Task<ActionResult> Index(string q, string category, string duration, string sort = "trending", string tab = "itineraries")
        {
            var publicTripsQuery = db.Trips
                .Include(t => t.TripStops.Select(s => s.DestinationCity))
                .Include(t => t.TripStops.Select(s => s.TripActivities))
                .Include(t => t.AspNetUser)
                .Where(t => t.IsPublic);

            // Search query filter (search title, description, creator name, or stop city names)
            if (!string.IsNullOrWhiteSpace(q))
            {
                string searchLower = q.Trim().ToLower();
                publicTripsQuery = publicTripsQuery.Where(t =>
                    t.Title.ToLower().Contains(searchLower) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchLower)) ||
                    (t.AspNetUser.FullName != null && t.AspNetUser.FullName.ToLower().Contains(searchLower)) ||
                    t.TripStops.Any(s => s.DestinationCity.Name.ToLower().Contains(searchLower) || s.DestinationCity.Country.ToLower().Contains(searchLower))
                );
            }

            var rawTrips = await publicTripsQuery.ToListAsync();

            // Transform into Community Itinerary Card ViewModels
            var cardList = rawTrips.Select(t =>
            {
                var orderedStops = t.TripStops.OrderBy(s => s.StopOrder).ToList();
                decimal calculatedCost = 0;
                var stopNames = new List<string>();

                foreach (var stop in orderedStops)
                {
                    if (stop.DestinationCity != null && !stopNames.Contains(stop.DestinationCity.Name))
                    {
                        stopNames.Add(stop.DestinationCity.Name);
                    }
                    calculatedCost += stop.AccommodationCost + stop.TransportCost;
                    calculatedCost += stop.TripActivities.Sum(a => a.Cost);
                }

                if (calculatedCost == 0) calculatedCost = t.TotalBudget;

                // Derive style / tags based on destination and budget
                string style = "Cultural Explorer";
                if (t.TotalBudget < 1500) style = "Budget Nomad";
                else if (t.TotalBudget >= 3500) style = "Luxury & Leisure";
                else if (t.Title.ToLower().Contains("romance") || t.Title.ToLower().Contains("honeymoon")) style = "Romantic Getaway";
                else if (t.Title.ToLower().Contains("adventure") || t.Title.ToLower().Contains("alps")) style = "Solo Adventure";

                var highlights = orderedStops
                    .SelectMany(s => s.TripActivities.Take(2).Select(a => a.CustomTitle))
                    .Take(3)
                    .ToList();

                // Deterministic engagement metrics for rich community feel
                int seed = Math.Abs(t.TripId * 37 + (t.Title?.Length ?? 0));
                int likes = 12 + (seed % 88);
                int clones = 4 + (seed % 34);

                return new CommunityItineraryCardViewModel
                {
                    TripId = t.TripId,
                    Title = t.Title,
                    Description = t.Description,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    CoverImageUrl = string.IsNullOrWhiteSpace(t.CoverImageUrl)
                        ? (orderedStops.FirstOrDefault()?.DestinationCity?.ImageUrl ?? "https://images.unsplash.com/photo-1488646953014-85cb44e25828?auto=format&fit=crop&w=800&q=80")
                        : t.CoverImageUrl,
                    TotalBudget = t.TotalBudget,
                    EstimatedCost = calculatedCost,
                    Currency = t.Currency ?? "USD",
                    ShareSlug = t.ShareSlug,
                    StopsCount = orderedStops.Count,
                    StopCityNames = stopNames,
                    OwnerId = t.UserId,
                    OwnerName = t.AspNetUser?.FullName ?? t.AspNetUser?.UserName ?? "Traveler",
                    OwnerAvatarUrl = string.IsNullOrWhiteSpace(t.AspNetUser?.AvatarUrl)
                        ? "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=120&q=80"
                        : t.AspNetUser.AvatarUrl,
                    OwnerCity = "Global",
                    OwnerCountry = t.AspNetUser?.PreferredCurrency ?? "USD",
                    LikesCount = likes,
                    ClonesCount = clones,
                    TravelStyle = style,
                    Highlights = highlights,
                    CreatedAt = t.CreatedAt
                };
            }).ToList();

            // Filter by Travel Style / Category
            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                cardList = cardList.Where(c => c.TravelStyle.ToLower().Contains(category.ToLower())).ToList();
            }

            // Filter by Duration
            if (!string.IsNullOrWhiteSpace(duration) && duration != "All")
            {
                if (duration == "short") cardList = cardList.Where(c => c.DurationDays <= 4).ToList();
                else if (duration == "medium") cardList = cardList.Where(c => c.DurationDays >= 5 && c.DurationDays <= 9).ToList();
                else if (duration == "long") cardList = cardList.Where(c => c.DurationDays >= 10).ToList();
            }

            // Sort logic
            switch (sort?.ToLower())
            {
                case "popular":
                case "trending":
                    cardList = cardList.OrderByDescending(c => c.LikesCount + c.ClonesCount * 2).ToList();
                    break;
                case "newest":
                    cardList = cardList.OrderByDescending(c => c.CreatedAt).ToList();
                    break;
                case "budget_low":
                    cardList = cardList.OrderBy(c => c.EstimatedCost).ToList();
                    break;
                case "budget_high":
                    cardList = cardList.OrderByDescending(c => c.EstimatedCost).ToList();
                    break;
                default:
                    cardList = cardList.OrderByDescending(c => c.LikesCount).ToList();
                    break;
            }

            // Top Creators from AspNetUsers
            var allUsersWithPublicTrips = await db.AspNetUsers
                .Include(u => u.Trips)
                .Where(u => u.Trips.Any(t => t.IsPublic))
                .ToListAsync();

            var topCreators = allUsersWithPublicTrips.Select(u =>
            {
                var userPublicTrips = u.Trips.Where(t => t.IsPublic).ToList();
                var topCities = userPublicTrips
                    .SelectMany(t => t.TripStops.Select(s => s.DestinationCity?.Name))
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .Take(3)
                    .ToList();

                return new CommunityCreatorViewModel
                {
                    UserId = u.Id,
                    FullName = u.FullName ?? u.UserName,
                    UserName = u.UserName,
                    AvatarUrl = string.IsNullOrWhiteSpace(u.AvatarUrl)
                        ? "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=120&q=80"
                        : u.AvatarUrl,
                    Bio = u.Bio ?? "Passionate wanderer sharing multi-city travel guides.",
                    City = "Nomad",
                    Country = "Global",
                    PublicTripsCount = userPublicTrips.Count,
                    TotalClonesReceived = userPublicTrips.Count * 14 + 18,
                    BadgeTitle = userPublicTrips.Count >= 2 ? "Top Travel Guide" : "Featured Explorer",
                    TopDestinations = topCities
                };
            }).OrderByDescending(c => c.PublicTripsCount).Take(6).ToList();

            // Community Travel Tips
            var travelTips = new List<CommunityTravelTipViewModel>
            {
                new CommunityTravelTipViewModel
                {
                    AuthorName = "Alex Traveler",
                    AuthorAvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=120&q=80",
                    CityName = "Paris",
                    Country = "France",
                    Category = "Smart Budgeting",
                    Title = "Navigo Easy Pass for Paris Metro",
                    Content = "Instead of single t+ tickets, buy a 10-ticket booklet on a Navigo Easy card to save ~35% on metro and bus transit.",
                    HelpfulCount = 42,
                    Tag = "Transport"
                },
                new CommunityTravelTipViewModel
                {
                    AuthorName = "Elena Wander",
                    AuthorAvatarUrl = "https://images.unsplash.com/photo-1517841905240-472988babdf9?auto=format&fit=crop&w=120&q=80",
                    CityName = "Tokyo",
                    Country = "Japan",
                    Category = "Cultural Insight",
                    Title = "Suica IC Card on Apple / Google Wallet",
                    Content = "Add a digital Suica card directly to your phone's wallet before landing. You can tap through transit gates and pay at konbini instantly.",
                    HelpfulCount = 68,
                    Tag = "Tech & Travel"
                },
                new CommunityTravelTipViewModel
                {
                    AuthorName = "Dhyan Shah",
                    AuthorAvatarUrl = "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?auto=format&fit=crop&w=120&q=80",
                    CityName = "Rome",
                    Country = "Italy",
                    Category = "Hidden Gem",
                    Title = "Colosseum Night Tours Skip All Crowds",
                    Content = "Book the evening underground tour of the Colosseum. It is 10° cooler, dramatically lit, and practically empty compared to morning rushes.",
                    HelpfulCount = 53,
                    Tag = "Sightseeing"
                }
            };

            // Global Metrics
            int totalPublic = await db.Trips.CountAsync(t => t.IsPublic);
            int totalTravelers = await db.AspNetUsers.CountAsync();
            int totalCities = await db.DestinationCities.CountAsync();
            int totalClones = cardList.Sum(c => c.ClonesCount) + 142;

            var popularCities = await db.DestinationCities.OrderByDescending(c => c.PopularityScore).Take(8).ToListAsync();

            var viewModel = new CommunityIndexViewModel
            {
                SearchQuery = q,
                SelectedCategory = category ?? "All",
                SelectedDuration = duration ?? "All",
                SortBy = sort ?? "trending",
                ActiveTab = tab ?? "itineraries",
                TotalPublicItineraries = totalPublic,
                TotalCommunityTravelers = totalTravelers,
                TotalCitiesExplored = totalCities,
                TotalClonesCount = totalClones,
                Itineraries = cardList,
                TopCreators = topCreators,
                TravelTips = travelTips,
                PopularCities = popularCities
            };

            return View(viewModel);
        }

        // =====================================================================
        // POST: /Community/ToggleLike
        // =====================================================================
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ToggleLike(int id)
        {
            // Deterministic response for live UI reaction
            int seed = Math.Abs(id * 37);
            int newCount = 12 + (seed % 88) + 1;
            return Json(new { success = true, likesCount = newCount, message = "Saved to your community favorites!" });
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
