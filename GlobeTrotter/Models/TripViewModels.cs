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
        [Display(Name = "Trip Title")]
        public string Title { get; set; }

        [Display(Name = "Trip Description & Notes")]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select the departure start date")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(14);

        [Required(ErrorMessage = "Please select the return end date")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(21);

        [Required(ErrorMessage = "Please set a target budget")]
        [Range(0, 1000000, ErrorMessage = "Budget must be a positive number")]
        [Display(Name = "Target Budget")]
        public decimal TotalBudget { get; set; } = 2000.00m;

        [Display(Name = "Currency")]
        public string Currency { get; set; } = "USD";

        [Display(Name = "Cover Photo URL (Optional)")]
        public string CoverImageUrl { get; set; }

        [Display(Name = "Make this trip public in community")]
        public bool IsPublic { get; set; } = false;
    }

    public class EditTripViewModel : CreateTripViewModel
    {
        public int TripId { get; set; }
    }

    public class QuickAddStopViewModel
    {
        [Required]
        public int TripId { get; set; }

        [Required]
        public int CityId { get; set; }

        [Required]
        public DateTime ArrivalDate { get; set; }

        [Required]
        public DateTime DepartureDate { get; set; }

        public decimal AccommodationCost { get; set; } = 0.00m;
        public decimal TransportCost { get; set; } = 0.00m;
        public string TransportMode { get; set; } = "Flight";
        public string Notes { get; set; }
    }
}
