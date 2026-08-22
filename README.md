# GlobeTrotter

## Empowering Personalized Travel Planning

GlobeTrotter is a personalized travel-planning application designed to
simplify the planning of multi-city trips. Users can create trips, add
destinations and activities, organize day-wise itineraries, estimate
budgets, visualize plans, and share itineraries with others.

This repository contains the project planning and technical workflow for
a hackathon implementation using **ASP.NET Core** and a **relational
database**.

------------------------------------------------------------------------

## 1. Project Vision

The vision of GlobeTrotter is to become a personalized, intelligent, and
collaborative travel-planning platform where users can:

-   Discover destinations and activities
-   Create customized multi-city trips
-   Organize travel dates and activities
-   Estimate and monitor trip expenses
-   Visualize itineraries using calendar and list views
-   Share trips publicly or with friends
-   Copy existing public trips
-   Receive intelligent travel recommendations

The platform should make travel planning simple, interactive, and
cost-conscious.

------------------------------------------------------------------------

## 2. Project Mission

The hackathon MVP focuses on building a responsive application that
simplifies multi-city travel planning.

The application should allow users to:

-   Create and manage trips
-   Add cities and travel dates
-   Discover activities
-   Build day-wise itineraries
-   Estimate trip budgets
-   View travel plans on a calendar/timeline
-   Share itineraries
-   Manage their profile and preferences

------------------------------------------------------------------------

## 3. Core Technology Stack

### Backend

-   ASP.NET Core Web API
-   Entity Framework Core
-   ASP.NET Core Identity
-   C#

### Database

-   SQL Server
-   Relational database design
-   Entity Framework Core migrations

### Frontend

The frontend can be implemented using a suitable ASP.NET-compatible UI
approach such as:

-   Razor Pages / MVC
-   Blazor
-   React or another frontend connected to the ASP.NET Core Web API

### External Services

Recommended external APIs:

1.  **Google Maps Platform**
2.  **Gemini API**
3.  **SerpApi** for flight and hotel search
4.  **Unsplash or Pexels** for destination images (optional)
5.  **Weather API** (optional)
6.  **Currency Exchange API** (optional)

------------------------------------------------------------------------

# 4. External API Requirements

## 4.1 Google Maps Platform

### Purpose

Google Maps is used for location-related functionality.

### Possible uses

-   City/location search
-   Map display
-   Selecting locations
-   Place search
-   Attractions and restaurants
-   Latitude/longitude
-   Route visualization
-   Distance between destinations
-   Markers for trip stops

### GlobeTrotter features using Google Maps

-   City Search
-   Activity/Place Search
-   Trip Map
-   Route visualization
-   Multi-city stop visualization

Official documentation:

https://developers.google.com/maps/documentation

------------------------------------------------------------------------

## 4.2 Gemini API

### Purpose

Gemini provides the AI layer of GlobeTrotter.

### Possible uses

-   Approximate trip-budget estimation
-   Personalized travel recommendations
-   Suggested itinerary generation
-   Travel-style recommendations
-   Budget optimization
-   Natural-language trip planning
-   Explaining why a trip is over budget

### Example

User:

> I have ₹80,000 for 7 days in Paris and Rome for two people.

Gemini can help estimate:

-   Accommodation
-   Food
-   Activities
-   Local transportation
-   Miscellaneous expenses

Important:

Gemini should be treated as an AI estimation/recommendation layer. It
should not be treated as a guaranteed source of live hotel or flight
prices.

Official documentation:

https://ai.google.dev/gemini-api/docs

------------------------------------------------------------------------

# 5. API Responsibility Summary

  Feature               External API              Required
  --------------------- ------------------------- --------------------
  Login / Signup        ASP.NET Identity          No external API
  Dashboard             Own database              No
  Create Trip           Own database              No
  My Trips              Own database              No
  City Search           Google Maps / Places      Yes
  Add Stop              Google Maps / Places      Recommended
  Activity Search       Google Places / SerpApi   Recommended
  Maps                  Google Maps               Yes
  Routes                Google Maps Routes        Recommended
  Itinerary Builder     Own database              No
  Itinerary View        Own database              No
  Calendar / Timeline   Own database + frontend   No
  Budget Calculation    ASP.NET + Gemini          Gemini recommended
  Flight Search         SerpApi                   Optional
  Hotel Search          SerpApi                   Optional
  Destination Images    Unsplash/Pexels           Optional
  Weather               Weather API               Optional
  Currency Conversion   Currency API              Optional
  Public Sharing        Own backend/database      No
  Copy Trip             Own backend/database      No
  Profile / Settings    Own database              No
  Admin Dashboard       Own database              No

------------------------------------------------------------------------

# 6. Recommended MVP API Set

For the hackathon, avoid integrating too many services.

### Core APIs

``` text
Google Maps
    ↓
Cities + Places + Maps + Routes

Gemini
    ↓
AI Budget + Recommendations

SerpApi
    ↓
Flights + Hotels
```

### Optional APIs

``` text
Unsplash/Pexels
    ↓
Destination images

Weather API
    ↓
Weather information

Currency API
    ↓
Currency conversion
```

The core application should still work if optional APIs are unavailable.

------------------------------------------------------------------------

# 7. Application Workflow

## Step 1 - Login / Signup

The user opens GlobeTrotter.

### User actions

-   Sign up
-   Log in
-   Forgot password
-   Log out

### Backend

ASP.NET Core Identity manages authentication.

User information is stored in SQL Server.

------------------------------------------------------------------------

# 8. Step 2 - Dashboard / Home

After successful login, the user reaches the dashboard.

### Dashboard contains

-   Welcome message
-   Upcoming trips
-   Recent trips
-   Plan New Trip button
-   Recommended destinations
-   Budget highlights

### Data source

Most dashboard information comes from the application's own database.

------------------------------------------------------------------------

# 9. Step 3 - Create Trip

User selects:

**Plan New Trip**

### Form

-   Trip name
-   Start date
-   End date
-   Description
-   Optional cover image

Example:

``` text
Trip Name: European Adventure
Start Date: 10 September
End Date: 20 September
Description: 10-day Europe trip
```

After saving, the trip is created in the database.

------------------------------------------------------------------------

# 10. Step 4 - My Trips

Display all trips created by the logged-in user.

Each trip card can show:

-   Trip name
-   Start date
-   End date
-   Number of destinations
-   Estimated budget
-   Edit
-   View
-   Delete

Example:

``` text
European Adventure
10 Sep - 20 Sep
3 destinations
Estimated: ₹95,000

[View] [Edit] [Delete]
```

------------------------------------------------------------------------

# 11. Step 5 - Add Cities / Stops

Inside a trip, the user selects:

**Add Stop**

### Workflow

``` text
Trip
 ↓
Add Stop
 ↓
Search City
 ↓
Google Maps / Places
 ↓
Select City
 ↓
Set Arrival Date
 ↓
Set Departure Date
 ↓
Save Stop
```

Example:

``` text
London
10 Sep - 13 Sep

Paris
13 Sep - 17 Sep

Rome
17 Sep - 20 Sep
```

The selected city information should be stored in the application's
database.

------------------------------------------------------------------------

# 12. Step 6 - Activity Search

After selecting a city, the user can discover activities.

Example:

``` text
Paris

Search activities:
[Eiffel Tower]

Filters:
- Category
- Cost
- Duration
```

Possible activities:

``` text
Eiffel Tower
Louvre Museum
Seine River Cruise
Montmartre
Paris Food Tour
```

The user selects:

**Add to Trip**

The selected activity becomes an itinerary item.

------------------------------------------------------------------------

# 13. Step 7 - Itinerary Builder

The user organizes the trip day by day.

Example:

``` text
10 September - Paris

09:00  Eiffel Tower
13:00  Lunch
15:00  Louvre Museum
19:00  Dinner
```

The user should be able to:

-   Add activity
-   Remove activity
-   Change date
-   Change time
-   Reorder activities
-   Move an activity to another day
-   Add notes

------------------------------------------------------------------------

# 14. Step 8 - Itinerary View

The completed itinerary can be displayed by:

### List View

``` text
Day 1
09:00 Eiffel Tower
13:00 Lunch
15:00 Louvre

Day 2
10:00 Montmartre
14:00 Seine Cruise
```

### Calendar View

The same activities are displayed inside a calendar.

The UI can contain:

``` text
[ List ] [ Calendar ]
```

This is the **view mode toggle**.

No external API is required for this feature.

------------------------------------------------------------------------

# 15. Step 9 - Trip Budget

The application calculates the estimated cost.

### Categories

``` text
Transportation
Accommodation
Activities
Meals
Miscellaneous
```

Example:

``` text
Flights          ₹30,000
Hotels           ₹35,000
Activities       ₹10,000
Food             ₹12,000
Transport         ₹5,000
Miscellaneous     ₹3,000
------------------------
Total            ₹95,000
```

### AI enhancement

Gemini can estimate costs that the user has not manually entered.

The UI can show:

``` text
Total Estimated Cost: ₹95,000
User Budget:          ₹1,00,000
Remaining:             ₹5,000
```

------------------------------------------------------------------------

# 16. Step 10 - Budget Alerts

The application compares the estimated cost with the user's budget.

Example:

``` text
Budget: ₹80,000
Estimated: ₹92,000

⚠ Trip is approximately ₹12,000 over budget.
```

The system can suggest:

-   Cheaper accommodation
-   Fewer activities
-   Lower-cost transportation
-   Reducing the number of destinations

Gemini can provide personalized recommendations.

------------------------------------------------------------------------

# 17. Step 11 - Trip Calendar / Timeline

The user can view the complete trip as:

### Calendar

``` text
10 Sep
London

11 Sep
London

12 Sep
London

13 Sep
Paris

14 Sep
Paris
```

### Vertical timeline

``` text
London
  ↓
13 Sep
  ↓
Paris
  ↓
17 Sep
  ↓
Rome
```

The user can expand individual days to see activities.

------------------------------------------------------------------------

# 18. Step 12 - Public / Shared Itinerary

The user selects:

**Share Trip**

The backend creates a unique public token.

Conceptually:

``` text
Trip
 ↓
Generate Public Share ID
 ↓
Public URL
 ↓
Read-only itinerary
```

The public page contains:

-   Trip name
-   Destinations
-   Dates
-   Activities
-   Estimated budget
-   Map
-   Itinerary

The public user cannot modify the original trip.

------------------------------------------------------------------------

# 20. Step 14 - Profile / Settings

The user can manage:

-   Name
-   Email
-   Profile image
-   Language preference
-   Saved destinations
-   Privacy
-   Account deletion

This information is stored in the application's database.

------------------------------------------------------------------------

# 21. Step 15 - Admin Dashboard

The admin dashboard is optional.

It can display:

``` text
Total Users
Total Trips
Public Trips
Popular Cities
Popular Activities
User Engagement
```

Charts can show:

-   Trips created per month
-   Most popular destinations
-   Most popular activities
-   Average trip budget

No external API is required.

------------------------------------------------------------------------

# 22. Relational Database Design

The database should be the main source of truth for application data.

Suggested tables:

``` text
Users
Trips
Cities
TripStops
Activities
ItineraryItems
Expenses
SavedDestinations
SharedTrips
```

### Relationships

``` text
Users
  │
  └── 1:N ── Trips
                │
                ├── 1:N ── TripStops
                │              │
                │              └── N:1 ── Cities
                │
                ├── 1:N ── ItineraryItems
                │              │
                │              └── N:1 ── Activities
                │
                └── 1:N ── Expenses
```

------------------------------------------------------------------------

# 23. Recommended Backend Architecture

``` text
Frontend
    │
    │ HTTP / JSON
    ↓
ASP.NET Core Web API
    │
    ├── Authentication
    ├── Trip Service
    ├── City Service
    ├── Activity Service
    ├── Itinerary Service
    ├── Budget Service
    ├── Sharing Service
    └── Admin Service
    │
    ↓
Entity Framework Core
    │
    ↓
SQL Server
```

External services are accessed from the backend:

``` text
ASP.NET Core
    │
    ├── Google Maps / Places
    ├── Gemini
    ├── SerpApi
    └── Optional APIs
```

API keys must remain on the server and should not be exposed in frontend
code.

------------------------------------------------------------------------

# 24. Suggested Development Order

Build the project in the following order.

## Phase 1 - Foundation

-   [ ] Create ASP.NET Core project
-   [ ] Configure SQL Server
-   [ ] Configure Entity Framework Core
-   [ ] Configure ASP.NET Core Identity
-   [ ] Create database entities
-   [ ] Create initial migration

## Phase 2 - Authentication

-   [ ] Signup
-   [ ] Login
-   [ ] Logout
-   [ ] Forgot password
-   [ ] Basic validation

## Phase 3 - Trip Management

-   [ ] Dashboard
-   [ ] Create Trip
-   [ ] My Trips
-   [ ] View Trip
-   [ ] Edit Trip
-   [ ] Delete Trip

## Phase 4 - Destinations

-   [ ] City Search
-   [ ] Google Maps integration
-   [ ] Add Stop
-   [ ] Arrival/departure dates
-   [ ] Reorder cities

## Phase 5 - Activities

-   [ ] Activity Search
-   [ ] Activity categories
-   [ ] Activity filters
-   [ ] Add Activity
-   [ ] Remove Activity
-   [ ] Activity duration and cost

## Phase 6 - Itinerary

-   [ ] Day-wise itinerary
-   [ ] Activity scheduling
-   [ ] Reorder activities
-   [ ] List view
-   [ ] Calendar view

## Phase 7 - Budget

-   [ ] Expense categories
-   [ ] Manual expenses
-   [ ] Automatic total
-   [ ] Average cost per day
-   [ ] Budget alerts
-   [ ] Charts
-   [ ] Gemini budget estimation

## Phase 8 - Travel Search

-   [ ] Flight search using SerpApi
-   [ ] Hotel search using SerpApi
-   [ ] Save selected flight/hotel costs
-   [ ] Add travel costs to budget

## Phase 9 - Sharing

-   [ ] Public itinerary
-   [ ] Public URL
-   [ ] Read-only view
-   [ ] Copy Trip
-   [ ] Social sharing

## Phase 10 - Final Enhancements

-   [ ] Profile/settings
-   [ ] Destination images
-   [ ] Weather
-   [ ] Currency conversion
-   [ ] Admin dashboard
-   [ ] Responsive design
-   [ ] Error handling
-   [ ] API-key security

------------------------------------------------------------------------

# 25. Minimum Viable Product

If time is limited, prioritize:

``` text
1. Login / Signup
2. Dashboard
3. Create Trip
4. My Trips
5. Add Cities
6. Add Activities
7. Itinerary Builder
8. Budget
9. Calendar/List View
10. Public Sharing
```

The optional features can be added after the core workflow works.

------------------------------------------------------------------------

# 26. Final Recommended API Architecture

``` text
                 GLOBETROTTER
                      │
                 ASP.NET Core
                      │
        ┌─────────────┼─────────────┐
        │             │             │
        ↓             ↓             ↓
   SQL Server      Google Maps    Gemini
        │             │             │
        │             │             ├── AI Budget
        │             │             ├── Recommendations
        │             │             └── AI Planning
        │             │
        │             ├── Cities
        │             ├── Places
        │             └── Routes
        │
        └─────────────┐
                      ↓
                    SerpApi
                      │
                 ┌────┴────┐
                 ↓         ↓
              Flights    Hotels
```

### Core external services

**Google Maps**\
Maps, cities, places, routes.

**Gemini**\
AI planning, budget estimation, recommendations.

**SerpApi**\
Flight and hotel search.

### Optional services

**Unsplash/Pexels** → images\
**Weather API** → weather\

------------------------------------------------------------------------

# 27. Project Goal

The final GlobeTrotter workflow should be:

``` text
Login
  ↓
Dashboard
  ↓
Create Trip
  ↓
Add Cities
  ↓
Add Activities
  ↓
Build Itinerary
  ↓
Estimate Budget
  ↓
View Calendar / Timeline
  ↓
Review Trip
  ↓
Share Trip
  ↓
Copy Trip
```

The application should demonstrate that the relational database is the
**core data store**, while external APIs enhance the application with
maps, travel search, and AI capabilities.
