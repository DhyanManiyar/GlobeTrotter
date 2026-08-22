using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GlobeTrotter.Models
{
    public class TripDashboardViewModel
    {
        public AspNetUser User { get; set; }
        public int TotalTripsCount { get; set; }
        public int UpcomingTripsCount { get; set; }
        public int CompletedTripsCount { get; set; }
        public int TotalCitiesPlanned { get; set; }
        public decimal TotalBudgetPlanned { get; set; }
        public decimal TotalEstimatedCost { get; set; }
        public int SavedDestinationsCount { get; set; }
        public List<TripCardViewModel> Trips { get; set; } = new List<TripCardViewModel>();
        public List<DestinationCity> RecommendedCities { get; set; } = new List<DestinationCity>();
    }

    public class TripCardViewModel
    {
        public int TripId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationDays => Math.Max(1, (EndDate - StartDate).Days + 1);
        public string CoverImageUrl { get; set; }
        public decimal TotalBudget { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal EstimatedCost { get; set; }
        public decimal RemainingBudget => TotalBudget - EstimatedCost;
        public bool IsOverBudget => TotalBudget > 0 && EstimatedCost > TotalBudget;
        public bool IsPublic { get; set; }
        public string ShareSlug { get; set; }
        public int StopsCount { get; set; }
        public List<string> StopCityNames { get; set; } = new List<string>();
        public string Status { get; set; } // "Upcoming", "Active", "Completed", "Draft"
        public int ProgressPercent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTripViewModel
    {
        [Required(ErrorMessage = "Please enter a memorable trip title")]
        [StringLength(150, ErrorMessage = "Trip title cannot exceed 150 characters")]
        [Display(Name = "Trip Name / Starting Point")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please select a destination place")]
        [Display(Name = "Select a Place")]
        public int PlaceCityId { get; set; }

        [Display(Name = "Trip Description & Notes")]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select the departure start date")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(7);

        [Required(ErrorMessage = "Please select the return end date")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(14);

        [Display(Name = "Target Budget")]
        public decimal TotalBudget { get; set; } = 2500.00m;

        [Display(Name = "Currency")]
        public string Currency { get; set; } = "USD";

        [Display(Name = "Cover Photo URL (Optional)")]
        public string CoverImageUrl { get; set; }

        [Display(Name = "Make this trip public in community")]
        public bool IsPublic { get; set; } = false;

        // Suggestions for Places to Visit / Activities to perform (Screen 4)
        public List<DestinationCity> AvailableCities { get; set; } = new List<DestinationCity>();
        public List<ActivitySuggestionItem> Suggestions { get; set; } = new List<ActivitySuggestionItem>();
        public string SelectedActivityIdsJson { get; set; }
    }

    public class ActivitySuggestionItem
    {
        public int ActivityId { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public decimal Cost { get; set; }
        public string ImageUrl { get; set; }
        public decimal DurationHours { get; set; }
        public bool IsSelected { get; set; }
    }

    public class EditTripViewModel : CreateTripViewModel
    {
        public int TripId { get; set; }
    }

    // Screen 5: Build Itinerary ViewModel
    public class BuildItineraryViewModel
    {
        public int TripId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalBudget { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal TotalCalculatedCost { get; set; }
        public List<ItinerarySectionViewModel> Sections { get; set; } = new List<ItinerarySectionViewModel>();
        public List<DestinationCity> AvailableCities { get; set; } = new List<DestinationCity>();
        public List<Activity> AvailableActivities { get; set; } = new List<Activity>();
    }

    public class ItinerarySectionViewModel
    {
        public int TripStopId { get; set; }
        public int SectionNumber { get; set; }
        public string PlaceName { get; set; }
        public string Country { get; set; }
        public string CityImageUrl { get; set; }
        public DateTime ArrivalDate { get; set; }
        public DateTime DepartureDate { get; set; }
        public string DateRangeDisplay => $"{ArrivalDate:MMM dd, yyyy} to {DepartureDate:MMM dd, yyyy}";
        public decimal SectionBudget { get; set; }
        public decimal AccommodationCost { get; set; }
        public string AccommodationDetails { get; set; }
        public decimal TransportCost { get; set; }
        public string TransportMode { get; set; }
        public string Notes { get; set; }
        public List<TripActivity> Activities { get; set; } = new List<TripActivity>();
    }

    public class AddSectionViewModel
    {
        [Required]
        public int TripId { get; set; }

        [Required(ErrorMessage = "Please select a destination place")]
        [Display(Name = "Place / City")]
        public int CityId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime ArrivalDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime DepartureDate { get; set; }

        [Display(Name = "Accommodation Budget / Hotel")]
        public decimal AccommodationCost { get; set; } = 0.00m;

        [Display(Name = "Hotel / Stay Information")]
        public string AccommodationDetails { get; set; }

        [Display(Name = "Transport Budget")]
        public decimal TransportCost { get; set; } = 0.00m;

        [Display(Name = "Transport Mode")]
        public string TransportMode { get; set; } = "Flight";

        [Display(Name = "Section Notes & Details")]
        public string Notes { get; set; }
    }

    public class AddActivityToSectionViewModel
    {
        [Required]
        public int TripStopId { get; set; }

        [Required]
        public string Title { get; set; }

        public string CategoryName { get; set; } = "Sightseeing";

        public decimal Cost { get; set; } = 0.00m;

        public decimal DurationHours { get; set; } = 2.0m;

        public string Notes { get; set; }
    }
}
