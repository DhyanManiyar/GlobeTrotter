using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GlobeTrotter.Models;

namespace GlobeTrotter.Models
{
    // =========================================================================
    // 1. DASHBOARD OVERVIEW VIEW MODEL
    // =========================================================================
    public class AdminDashboardViewModel
    {
        public int TotalUsersCount { get; set; }
        public double UserGrowthRate { get; set; } = 12.5;

        public int TotalTripsCount { get; set; }
        public double TripGrowthRate { get; set; } = 8.3;

        public int ActiveUsersTodayCount { get; set; }
        public double BounceRate { get; set; } = 2.3;

        public decimal EstimatedMonthlyRevenue { get; set; }
        public double RevenueGrowthRate { get; set; } = 18.2;

        public int TotalCitiesCount { get; set; }
        public int TotalActivitiesCount { get; set; }
        public int PendingModerationCount { get; set; }

        // Chart Data Series (User Growth & Trip Creation)
        public List<string> ChartDaysLabels { get; set; } = new List<string>();
        public List<int> NewUsersSeries { get; set; } = new List<int>();
        public List<int> ReturningUsersSeries { get; set; } = new List<int>();
        public List<int> TripsCreatedSeries { get; set; } = new List<int>();

        // Activity Feed
        public List<AdminActivityFeedItem> RecentActivities { get; set; } = new List<AdminActivityFeedItem>();
    }

    public class AdminActivityFeedItem
    {
        public string UserName { get; set; }
        public string UserAvatar { get; set; }
        public string ActionText { get; set; }
        public string TargetTitle { get; set; }
        public string TimeAgo { get; set; }
        public string IconClass { get; set; }
        public string BadgeColor { get; set; }
    }

    // =========================================================================
    // 2. USER MANAGEMENT VIEW MODELS
    // =========================================================================
    public class AdminUserListViewModel
    {
        public List<AdminUserRowViewModel> Users { get; set; } = new List<AdminUserRowViewModel>();
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int SuspendedUsers { get; set; }
        public int AdminCount { get; set; }

        public string CurrentRoleFilter { get; set; }
        public string CurrentStatusFilter { get; set; }
        public string SearchQuery { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }

    public class AdminUserRowViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public string RoleName { get; set; } = "User"; // Admin, User, Premium
        public string Status { get; set; } = "Active"; // Active, Suspended, Inactive
        public int TripsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string LastLoginAgo { get; set; } = "2 hours ago";
    }

    public class AdminUserDetailViewModel
    {
        public AspNetUser User { get; set; }
        public string RoleName { get; set; }
        public string Status { get; set; }
        public int TotalTrips { get; set; }
        public int SavedDestinationsCount { get; set; }
        public decimal TotalTripBudgetSpent { get; set; }
        public List<Trip> UserTrips { get; set; } = new List<Trip>();
        public List<SavedDestination> SavedDestinations { get; set; } = new List<SavedDestination>();
        public List<AdminUserAuditLogItem> AuditLogs { get; set; } = new List<AdminUserAuditLogItem>();
    }

    public class AdminUserAuditLogItem
    {
        public string ActionType { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
    }

    // =========================================================================
    // 3. CONTENT MANAGEMENT VIEW MODELS
    // =========================================================================
    public class AdminContentViewModel
    {
        public string ActiveTab { get; set; } = "cities"; // cities, activities, categories
        public List<DestinationCity> Cities { get; set; } = new List<DestinationCity>();
        public List<Activity> Activities { get; set; } = new List<Activity>();
        public List<ActivityCategory> Categories { get; set; } = new List<ActivityCategory>();
        public string SearchQuery { get; set; }
        public string RegionFilter { get; set; }
    }

    public class CityFormViewModel
    {
        public int CityId { get; set; }

        [Required]
        [Display(Name = "City Name")]
        public string Name { get; set; }

        [Required]
        public string Country { get; set; }

        [Required]
        public string Region { get; set; } = "Europe";

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Cover Image URL")]
        public string ImageUrl { get; set; }

        [Display(Name = "Cost Index (Budget/Moderate/Luxury)")]
        public string CostIndex { get; set; } = "Moderate";

        [Display(Name = "Average Daily Cost ($)")]
        public decimal AvgDailyCost { get; set; } = 150m;

        [Display(Name = "Popularity Score (1-100)")]
        public int PopularityScore { get; set; } = 85;

        public bool IsFeatured { get; set; } = true;
        public string Status { get; set; } = "Active";
    }

    public class ActivityFormViewModel
    {
        public int ActivityId { get; set; }

        [Required]
        public int CityId { get; set; }

        [Required]
        public string Title { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        public string Description { get; set; }

        [Display(Name = "Estimated Cost ($)")]
        public decimal EstimatedCost { get; set; } = 40m;

        [Display(Name = "Duration (Hours)")]
        public decimal DurationHours { get; set; } = 2.5m;

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; }

        public string BookingUrl { get; set; }
    }

    // =========================================================================
    // 4. ANALYTICS VIEW MODELS
    // =========================================================================
    public class AdminAnalyticsViewModel
    {
        public string DateRange { get; set; } = "30d";
        public int TotalTripsCount { get; set; }
        public int ActiveUsersCount { get; set; }
        public string AvgSessionDuration { get; set; } = "8m 32s";

        public List<string> TopCitiesLabels { get; set; } = new List<string>();
        public List<int> TopCitiesCounts { get; set; } = new List<int>();

        public List<string> CategoryLabels { get; set; } = new List<string>();
        public List<int> CategoryCounts { get; set; } = new List<int>();

        public List<FunnelStepItem> FunnelSteps { get; set; } = new List<FunnelStepItem>();
    }

    public class FunnelStepItem
    {
        public string StepName { get; set; }
        public int UserCount { get; set; }
        public double ConversionPercentage { get; set; }
    }

    // =========================================================================
    // 5. MODERATION QUEUE VIEW MODELS
    // =========================================================================
    public class AdminModerationViewModel
    {
        public string CurrentStatusFilter { get; set; } = "Pending";
        public List<ModerationQueueItem> Items { get; set; } = new List<ModerationQueueItem>();
        public int PendingCount { get; set; }
        public int InReviewCount { get; set; }
        public int ResolvedCount { get; set; }
    }

    public class ModerationQueueItem
    {
        public int ReportId { get; set; }
        public string ContentType { get; set; } // Trip, City, Activity, User
        public string ContentTitle { get; set; }
        public string ReportedBy { get; set; }
        public string Reason { get; set; } // Spam, Inappropriate, Offensive, Other
        public string Priority { get; set; } // High, Medium, Low
        public string Status { get; set; } // Pending, In Review, Resolved
        public DateTime SubmittedAt { get; set; }
    }

    // =========================================================================
    // 6. SYSTEM SETTINGS VIEW MODELS
    // =========================================================================
    public class AdminSettingsViewModel
    {
        public string ActiveTab { get; set; } = "general";

        // General
        public string PlatformName { get; set; } = "GlobeTrotter";
        public string Tagline { get; set; } = "The Intelligent Multi-City Travel Operating System";
        public string DefaultCurrency { get; set; } = "USD";
        public string DefaultLanguage { get; set; } = "English (US)";
        public string RegistrationMode { get; set; } = "Open";
        public int MaxTripDurationDays { get; set; } = 60;
        public int MaxStopsPerTrip { get; set; } = 15;

        // Email Templates
        public string WelcomeEmailSubject { get; set; } = "Welcome to GlobeTrotter - Start Your Journey";
        public string WelcomeEmailBody { get; set; } = "Hello {{UserName}},\n\nWelcome to GlobeTrotter! Start planning your next multi-city journey now.";
        public string TripShareEmailSubject { get; set; } = "{{UserName}} has shared a travel itinerary with you!";

        // API Integrations
        public string GoogleMapsApiKey { get; set; } = "AIzaSyD_DEMO_KEY_GLOBETROTTER_PLATFORM";
        public string WeatherApiKey { get; set; } = "wt_live_83921049281048201";
        public string CurrencyExchangeApiKey { get; set; } = "fx_tier1_9918274019283";

        // Feature Toggles
        public bool EnableCommunitySharing { get; set; } = true;
        public bool EnableAiItineraryGenerator { get; set; } = true;
        public bool EnablePublicTripCloning { get; set; } = true;
        public bool EnableMaintenanceMode { get; set; } = false;
    }
}
