using System;
using System.Collections.Generic;

namespace GlobeTrotter.Models
{
    public class CommunityIndexViewModel
    {
        public string SearchQuery { get; set; }
        public string SelectedCategory { get; set; } = "All"; // "All", "Solo", "Budget", "Luxury", "Cultural", "Nature"
        public string SelectedDuration { get; set; } = "All"; // "All", "short", "medium", "long"
        public string SortBy { get; set; } = "trending"; // "trending", "popular", "newest", "budget_low", "budget_high"
        public string ActiveTab { get; set; } = "itineraries"; // "itineraries", "creators", "tips"

        // Metrics & Stats
        public int TotalPublicItineraries { get; set; }
        public int TotalCommunityTravelers { get; set; }
        public int TotalCitiesExplored { get; set; }
        public int TotalClonesCount { get; set; }

        // Data collections
        public List<CommunityItineraryCardViewModel> Itineraries { get; set; } = new List<CommunityItineraryCardViewModel>();
        public List<CommunityCreatorViewModel> TopCreators { get; set; } = new List<CommunityCreatorViewModel>();
        public List<CommunityTravelTipViewModel> TravelTips { get; set; } = new List<CommunityTravelTipViewModel>();
        public List<DestinationCity> PopularCities { get; set; } = new List<DestinationCity>();
    }

    public class CommunityItineraryCardViewModel
    {
        public int TripId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationDays => Math.Max(1, (EndDate - StartDate).Days + 1);
        public string CoverImageUrl { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Currency { get; set; } = "USD";
        public string ShareSlug { get; set; }
        public int StopsCount { get; set; }
        public List<string> StopCityNames { get; set; } = new List<string>();
        public string OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string OwnerAvatarUrl { get; set; }
        public string OwnerCity { get; set; }
        public string OwnerCountry { get; set; }
        public int LikesCount { get; set; }
        public int ClonesCount { get; set; }
        public string TravelStyle { get; set; } // "Solo Adventure", "Romantic", "Budget Nomad", "Cultural Explorer"
        public List<string> Highlights { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
    }

    public class CommunityCreatorViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string AvatarUrl { get; set; }
        public string Bio { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public int PublicTripsCount { get; set; }
        public int TotalClonesReceived { get; set; }
        public string BadgeTitle { get; set; }
        public List<string> TopDestinations { get; set; } = new List<string>();
    }

    public class CommunityTravelTipViewModel
    {
        public string AuthorName { get; set; }
        public string AuthorAvatarUrl { get; set; }
        public string CityName { get; set; }
        public string Country { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int HelpfulCount { get; set; }
        public string Tag { get; set; }
    }
}
