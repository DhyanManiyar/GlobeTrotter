# GlobeTrotter — Project Specification & Architecture Document

## 1. Executive Summary
**GlobeTrotter** is an intelligent, personalized, and collaborative multi-city travel planning platform built with **ASP.NET MVC**, **Entity Framework**, and modern responsive frontend design. It empowers travelers to discover destinations, design structured day-wise multi-city itineraries, automatically calculate and track budgets, visualize timelines, and share travel plans with the community.

---

## 2. System Architecture & Tech Stack
* **Framework**: ASP.NET MVC (.NET Framework 4.8 / C#)
* **ORM & Database**: Entity Framework 6 (Code First) with SQL Server / LocalDB
* **Authentication**: ASP.NET Identity (Cookie-based auth, role management)
* **Frontend**: Responsive HTML5, Modern CSS (Glassmorphism, custom design system, CSS variables), JavaScript (Modular Vanilla JS / jQuery / AJAX)
* **Visualizations & UI Components**:
  * **Charts**: Chart.js for budget breakdowns and analytics
  * **Icons**: FontAwesome / Bootstrap Icons
  * **Animations & Interactivity**: Drag-and-drop itinerary reordering, interactive modals, responsive calendar/timeline views

---

## 3. Detailed Feature Breakdown & Screen Specifications

### 3.1. Authentication & User Management
* **Login & Registration**: Secure registration with email, password validation, and remember-me functionality.
* **Profile & Preferences**: Manage user profile (name, avatar, preferred currency, bio, language preference).
* **Role System**: Standard User vs Admin access.

### 3.2. Dashboard & Home Hub
* **Personalized Welcome**: User greetings, stats (Trips planned, Countries explored, Budget saved).
* **Recent & Upcoming Trips**: Quick-access cards with countdowns and progress indicators.
* **Quick Actions**: Prominent "Plan New Trip" CTA, "Explore Cities", "Browse Community Plans".
* **Inspiration & Recommendations**: Curated popular destinations carousel with cost estimates.

### 3.3. Trip Creation & Management
* **Create Trip**: Form specifying Trip Name, Start/End Dates, Total Budget, Description, and Cover Photo.
* **My Trips Dashboard**:
  * Grid and list view of all trips (Upcoming, Past, Drafts).
  * Trip cards with date duration, destination tags, budget gauge, and quick actions (Edit, View, Delete, Duplicate).

### 3.4. Itinerary Builder (Core Engine)
* **Multi-City Stops**:
  * Add multiple city stops with arrival and departure dates.
  * Reorder cities dynamically (adjusts chronology automatically).
* **Day-Wise Activity Scheduler**:
  * Assign pre-populated or custom activities to specific days and stops.
  * Track time slots (Morning, Afternoon, Evening) and durations.
* **Cost Tracking per Stop**:
  * Add accommodation costs, transport between stops, and meal allowances.

### 3.5. City & Activity Discovery Catalogs
* **City Search & Discovery**:
  * Search by name, country, or region.
  * Filters for cost index (Budget, Mid-range, Luxury) and popularity.
  * One-click "Add to Trip" stop integration.
* **Activity Search & Catalog**:
  * Filter by category: Sightseeing, Food & Dining, Adventure, Culture, Nightlife, Relaxation.
  * Filter by cost range and duration.
  * Rich activity details with images, descriptions, estimated prices, and reviews.

### 3.6. Budget & Financial Analytics
* **Automated Cost Calculation**: Real-time aggregation of Transport + Stay + Activities + Meals + Misc.
* **Interactive Visualizations**:
  * Donut chart for category cost breakdown.
  * Bar chart for daily spend distribution.
* **Budget Health & Alerts**:
  * Target Budget vs Estimated Cost comparison progress bar.
  * Overbudget alerts highlighting costly days or categories.
  * Average daily expense calculations.

### 3.7. Visual Itinerary, Timeline & Calendar Views
* **View Modes**:
  * **Timeline View**: Vertical chronological travel journey with route badges.
  * **Calendar View**: Day-by-day interactive matrix.
  * **Detailed List View**: Printable/exportable itinerary breakdown with notes and booking codes.

### 3.8. Social Sharing & Community Itineraries
* **Public Sharable Links**: Unique slug/URL for viewing read-only trip itineraries.
* **Community Features**:
  * "Copy / Fork Trip" button: Allows other users to clone an itinerary into their own account to customize.
  * Social sharing integrations (Direct link copy, WhatsApp, Twitter/X, Email).

### 3.9. Admin & Analytics Dashboard (Differentiator)
* **Platform Insights**: Active travelers, total trips created, most popular destinations and activities.
* **Data Management**: Manage pre-populated cities, activities, categories, and manage users.

---

## 4. Relational Data Model Schema (Entity Framework)

```
+-----------------------------------------------------------------------------------+
|                                 ApplicationUser                                   |
| (Id, Email, FullName, AvatarUrl, PreferredCurrency, Bio, CreatedAt)               |
+-----------------------------------------------------------------------------------+
                                         | 1
                                         |
                                         | *
+-----------------------------------------------------------------------------------+
|                                      Trip                                         |
| (TripId, UserId, Title, Description, StartDate, EndDate, CoverImageUrl,          |
|  TotalBudget, Currency, IsPublic, ShareSlug, CreatedAt, UpdatedAt)                |
+-----------------------------------------------------------------------------------+
                                         | 1
                                         |
                                         | *
+-----------------------------------------------------------------------------------+
|                                   TripStop                                        |
| (TripStopId, TripId, CityId, StopOrder, ArrivalDate, DepartureDate,               |
|  AccommodationCost, TransportCost, TransportMode, Notes)                          |
+-----------------------------------------------------------------------------------+
              | *                                                        | 1
              |                                                          |
              | *                                                        | *
+------------------------------------+             +--------------------------------+
|          TripActivity              |             |        DestinationCity         |
| (TripActivityId, TripStopId,       |             | (CityId, Name, Country, Region,|
|  ActivityId, CustomTitle,          |             |  Description, ImageUrl,        |
|  ScheduledDate, TimeOfDay, Cost,   |             |  CostIndex, PopularityScore,   |
|  DurationHours, Category, Notes)   |             |  AvgDailyCost, Latitude,       |
+------------------------------------+             |  Longitude)                    |
              | *                                  +--------------------------------+
              |                                                  | 1
              | 0..1                                             |
+------------------------------------+                           | *
|              Activity              |---------------------------+
| (ActivityId, CityId, Title,        |
|  Description, Category, ImageUrl,  |
|  EstimatedCost, DurationHours,     |
|  Rating)                           |
+------------------------------------+
```

---

## 5. Seed Data Strategy
To ensure the application looks rich and ready for live demonstration immediately:
* **Cities**: 15+ top global destinations (e.g., Paris, Tokyo, Rome, New York, Bali, Cairo, Kyoto, Barcelona, Cape Town, Bangkok, London, etc.) with high-quality imagery, geo-coords, and cost profiles.
* **Activities**: 50+ pre-seeded activities across categories with realistic costs and timings.
* **Sample Featured Trips**: Curated public itineraries (e.g., "7-Day European Romance", "10-Day Japan Heritage & Cuisine Tour", "Ultimate Southeast Asia Explorer") ready for cloning and demonstration.
