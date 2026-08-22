using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GlobeTrotter.Models
{
    // =========================================================================
    // USER PROFILE VIEW MODELS
    // =========================================================================
    public class UserProfileViewModel
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string LastName { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();

        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Home City")]
        public string City { get; set; }

        [Display(Name = "Country")]
        public string Country { get; set; }

        [Display(Name = "Profile Photo URL")]
        public string AvatarUrl { get; set; }

        [Display(Name = "Bio & Travel Preferences")]
        [StringLength(1000)]
        public string Bio { get; set; }

        [Display(Name = "Preferred Currency")]
        public string PreferredCurrency { get; set; } = "USD";

        [Display(Name = "Language")]
        public string LanguagePreference { get; set; } = "English";

        public DateTime MemberSince { get; set; }

        // User Stats
        public int TotalTripsCount { get; set; }
        public int UpcomingTripsCount { get; set; }
        public int CompletedTripsCount { get; set; }
        public int CitiesVisitedCount { get; set; }
        public decimal TotalBudgetManaged { get; set; }
        public int WishlistSavedCount { get; set; }

        // Trips lists for upcoming vs previous tabs
        public List<TripCardViewModel> UpcomingTrips { get; set; } = new List<TripCardViewModel>();
        public List<TripCardViewModel> PreviousTrips { get; set; } = new List<TripCardViewModel>();
        public List<DestinationCity> WishlistCities { get; set; } = new List<DestinationCity>();
    }

    // =========================================================================
    // EXPLORE & SEARCH (CITY & ACTIVITY) VIEW MODELS
    // =========================================================================
    public class ExploreSearchViewModel
    {
        public string SearchQuery { get; set; }
        public string SelectedCategory { get; set; } = "All";
        public int? SelectedCityId { get; set; }
        public string PriceRange { get; set; } = "All";
        public string SortBy { get; set; } = "popular";
        public string ActiveTab { get; set; } = "all"; // "all", "cities", "activities"

        public List<DestinationCity> Cities { get; set; } = new List<DestinationCity>();
        public List<ActivityExploreItem> Activities { get; set; } = new List<ActivityExploreItem>();
        public List<ActivityCategory> Categories { get; set; } = new List<ActivityCategory>();
        public List<DestinationCity> AllFilterCities { get; set; } = new List<DestinationCity>();
        public List<Trip> UserTrips { get; set; } = new List<Trip>();
    }

    public class ActivityExploreItem
    {
        public int ActivityId { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public string Country { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal DurationHours { get; set; }
        public decimal Rating { get; set; }
        public bool IsTopPick { get; set; }
        public string ImageUrl { get; set; }
    }
}
