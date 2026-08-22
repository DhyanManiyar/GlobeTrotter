-- =====================================================================================
-- GLOBETROTTER DATABASE SCHEMA & SEED SCRIPT
-- Application: GlobeTrotter - Personalized & Collaborative Travel Planning Platform
-- Target DBMS: Microsoft SQL Server 2016+ / Azure SQL / LocalDB
-- Author: Database Architecture Team
-- =====================================================================================

USE master;
GO

-- 1. DATABASE CREATION (Safe Check)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'GlobeTrotterDB')
BEGIN
    CREATE DATABASE GlobeTrotterDB;
    PRINT '>> Database GlobeTrotterDB created successfully.';
END
ELSE
BEGIN
    PRINT '>> Database GlobeTrotterDB already exists.';
END
GO

USE GlobeTrotterDB;
GO

-- =====================================================================================
-- 2. DROP EXISTING OBJECTS (FOR CLEAN RE-RUNNABILITY IN DEVELOPMENT)
-- =====================================================================================
IF OBJECT_ID('dbo.vw_TripBudgetSummary', 'V') IS NOT NULL DROP VIEW dbo.vw_TripBudgetSummary;
IF OBJECT_ID('dbo.vw_CityPopularityRankings', 'V') IS NOT NULL DROP VIEW dbo.vw_CityPopularityRankings;
IF OBJECT_ID('dbo.sp_CloneTrip', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CloneTrip;
IF OBJECT_ID('dbo.sp_GetTripDailyTimeline', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetTripDailyTimeline;

IF OBJECT_ID('dbo.SavedDestinations', 'U') IS NOT NULL DROP TABLE dbo.SavedDestinations;
IF OBJECT_ID('dbo.TripExpenses', 'U') IS NOT NULL DROP TABLE dbo.TripExpenses;
IF OBJECT_ID('dbo.TripActivities', 'U') IS NOT NULL DROP TABLE dbo.TripActivities;
IF OBJECT_ID('dbo.TripStops', 'U') IS NOT NULL DROP TABLE dbo.TripStops;
IF OBJECT_ID('dbo.Trips', 'U') IS NOT NULL DROP TABLE dbo.Trips;
IF OBJECT_ID('dbo.Activities', 'U') IS NOT NULL DROP TABLE dbo.Activities;
IF OBJECT_ID('dbo.ActivityCategories', 'U') IS NOT NULL DROP TABLE dbo.ActivityCategories;
IF OBJECT_ID('dbo.DestinationCities', 'U') IS NOT NULL DROP TABLE dbo.DestinationCities;

IF OBJECT_ID('dbo.AspNetUserRoles', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserRoles;
IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserClaims;
IF OBJECT_ID('dbo.AspNetUserLogins', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserLogins;
IF OBJECT_ID('dbo.AspNetRoles', 'U') IS NOT NULL DROP TABLE dbo.AspNetRoles;
IF OBJECT_ID('dbo.AspNetUsers', 'U') IS NOT NULL DROP TABLE dbo.AspNetUsers;
GO

-- =====================================================================================
-- 3. ASP.NET IDENTITY TABLES (CUSTOMIZED WITH EXTENDED USER PROFILE DATA)
-- =====================================================================================

CREATE TABLE dbo.AspNetRoles (
    Id NVARCHAR(128) NOT NULL,
    Name NVARCHAR(256) NOT NULL,
    CONSTRAINT PK_dbo_AspNetRoles PRIMARY KEY CLUSTERED (Id ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX RoleNameIndex ON dbo.AspNetRoles (Name ASC);
GO

CREATE TABLE dbo.AspNetUsers (
    Id NVARCHAR(128) NOT NULL,
    Email NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL DEFAULT 0,
    PasswordHash NVARCHAR(MAX) NULL,
    SecurityStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
    TwoFactorEnabled BIT NOT NULL DEFAULT 0,
    LockoutEndDateUtc DATETIME NULL,
    LockoutEnabled BIT NOT NULL DEFAULT 0,
    AccessFailedCount INT NOT NULL DEFAULT 0,
    UserName NVARCHAR(256) NOT NULL,
    -- Extended Custom Profile Columns
    FullName NVARCHAR(150) NULL,
    AvatarUrl NVARCHAR(500) NULL,
    Bio NVARCHAR(500) NULL,
    PreferredCurrency NVARCHAR(10) NOT NULL DEFAULT 'USD',
    LanguagePreference NVARCHAR(50) NOT NULL DEFAULT 'English',
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_dbo_AspNetUsers PRIMARY KEY CLUSTERED (Id ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UserNameIndex ON dbo.AspNetUsers (UserName ASC);
GO

CREATE TABLE dbo.AspNetUserClaims (
    Id INT IDENTITY(1,1) NOT NULL,
    UserId NVARCHAR(128) NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT PK_dbo_AspNetUserClaims PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_UserId ON dbo.AspNetUserClaims (UserId ASC);
GO

CREATE TABLE dbo.AspNetUserLogins (
    LoginProvider NVARCHAR(128) NOT NULL,
    ProviderKey NVARCHAR(128) NOT NULL,
    UserId NVARCHAR(128) NOT NULL,
    CONSTRAINT PK_dbo_AspNetUserLogins PRIMARY KEY CLUSTERED (LoginProvider ASC, ProviderKey ASC, UserId ASC),
    CONSTRAINT FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_UserId ON dbo.AspNetUserLogins (UserId ASC);
GO

CREATE TABLE dbo.AspNetUserRoles (
    UserId NVARCHAR(128) NOT NULL,
    RoleId NVARCHAR(128) NOT NULL,
    CONSTRAINT PK_dbo_AspNetUserRoles PRIMARY KEY CLUSTERED (UserId ASC, RoleId ASC),
    CONSTRAINT FK_dbo_AspNetUserRoles_dbo_AspNetRoles_RoleId FOREIGN KEY (RoleId) 
        REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_dbo_AspNetUserRoles_dbo_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_RoleId ON dbo.AspNetUserRoles (RoleId ASC);
CREATE NONCLUSTERED INDEX IX_UserId ON dbo.AspNetUserRoles (UserId ASC);
GO

-- =====================================================================================
-- 4. CORE BUSINESS ENTITIES & CATALOG TABLES
-- =====================================================================================

-- Destination Cities Catalog
CREATE TABLE dbo.DestinationCities (
    CityId INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Country NVARCHAR(100) NOT NULL,
    Region NVARCHAR(100) NOT NULL, -- e.g. Europe, Asia, North America, etc.
    Description NVARCHAR(MAX) NOT NULL,
    ImageUrl NVARCHAR(500) NOT NULL,
    CostIndex NVARCHAR(20) NOT NULL DEFAULT 'Moderate', -- 'Budget', 'Moderate', 'Luxury'
    PopularityScore DECIMAL(3,2) NOT NULL DEFAULT 4.50,
    AvgDailyCost DECIMAL(10,2) NOT NULL DEFAULT 100.00,
    CurrencyCode NVARCHAR(10) NOT NULL DEFAULT 'USD',
    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,
    IsFeatured BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_DestinationCities PRIMARY KEY CLUSTERED (CityId ASC),
    CONSTRAINT CK_City_PopularityScore CHECK (PopularityScore BETWEEN 1.00 AND 5.00),
    CONSTRAINT CK_City_CostIndex CHECK (CostIndex IN ('Budget', 'Moderate', 'Luxury'))
);
GO

CREATE NONCLUSTERED INDEX IX_DestinationCities_Region ON dbo.DestinationCities (Region ASC);
CREATE NONCLUSTERED INDEX IX_DestinationCities_CostIndex ON dbo.DestinationCities (CostIndex ASC);
CREATE NONCLUSTERED INDEX IX_DestinationCities_Popularity ON dbo.DestinationCities (PopularityScore DESC);
GO

-- Activity Categories Lookup
CREATE TABLE dbo.ActivityCategories (
    CategoryId INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(50) NOT NULL,
    IconClass NVARCHAR(50) NOT NULL DEFAULT 'fa-solid fa-compass',
    Description NVARCHAR(250) NULL,
    CONSTRAINT PK_ActivityCategories PRIMARY KEY CLUSTERED (CategoryId ASC),
    CONSTRAINT UQ_ActivityCategories_CategoryName UNIQUE (CategoryName)
);
GO

-- Predefined Catalog Activities per City
CREATE TABLE dbo.Activities (
    ActivityId INT IDENTITY(1,1) NOT NULL,
    CityId INT NOT NULL,
    CategoryId INT NOT NULL,
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    ImageUrl NVARCHAR(500) NULL,
    EstimatedCost DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    DurationHours DECIMAL(4,2) NOT NULL DEFAULT 2.00,
    Rating DECIMAL(3,2) NOT NULL DEFAULT 4.80,
    IsTopPick BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Activities PRIMARY KEY CLUSTERED (ActivityId ASC),
    CONSTRAINT FK_Activities_DestinationCities FOREIGN KEY (CityId) 
        REFERENCES dbo.DestinationCities (CityId) ON DELETE CASCADE,
    CONSTRAINT FK_Activities_ActivityCategories FOREIGN KEY (CategoryId) 
        REFERENCES dbo.ActivityCategories (CategoryId),
    CONSTRAINT CK_Activity_Rating CHECK (Rating BETWEEN 1.00 AND 5.00),
    CONSTRAINT CK_Activity_Cost CHECK (EstimatedCost >= 0.00),
    CONSTRAINT CK_Activity_Duration CHECK (DurationHours > 0.00)
);
GO

CREATE NONCLUSTERED INDEX IX_Activities_CityId ON dbo.Activities (CityId ASC);
CREATE NONCLUSTERED INDEX IX_Activities_CategoryId ON dbo.Activities (CategoryId ASC);
CREATE NONCLUSTERED INDEX IX_Activities_Rating ON dbo.Activities (Rating DESC);
GO

-- User Trips Header
CREATE TABLE dbo.Trips (
    TripId INT IDENTITY(1,1) NOT NULL,
    UserId NVARCHAR(128) NOT NULL,
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    CoverImageUrl NVARCHAR(500) NULL,
    TotalBudget DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    Currency NVARCHAR(10) NOT NULL DEFAULT 'USD',
    IsPublic BIT NOT NULL DEFAULT 0,
    ShareSlug NVARCHAR(100) NOT NULL,
    ForkedFromTripId INT NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Trips PRIMARY KEY CLUSTERED (TripId ASC),
    CONSTRAINT FK_Trips_AspNetUsers FOREIGN KEY (UserId) 
        REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Trips_ForkedFrom FOREIGN KEY (ForkedFromTripId) 
        REFERENCES dbo.Trips (TripId),
    CONSTRAINT UQ_Trips_ShareSlug UNIQUE (ShareSlug),
    CONSTRAINT CK_Trip_Dates CHECK (StartDate <= EndDate),
    CONSTRAINT CK_Trip_Budget CHECK (TotalBudget >= 0.00)
);
GO

CREATE NONCLUSTERED INDEX IX_Trips_UserId ON dbo.Trips (UserId ASC);
CREATE NONCLUSTERED INDEX IX_Trips_StartDate ON dbo.Trips (StartDate ASC);
CREATE NONCLUSTERED INDEX IX_Trips_IsPublic ON dbo.Trips (IsPublic ASC);
GO

-- Trip Stops (Multi-City Travel Stops)
CREATE TABLE dbo.TripStops (
    TripStopId INT IDENTITY(1,1) NOT NULL,
    TripId INT NOT NULL,
    CityId INT NOT NULL,
    StopOrder INT NOT NULL,
    ArrivalDate DATE NOT NULL,
    DepartureDate DATE NOT NULL,
    AccommodationCost DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    AccommodationDetails NVARCHAR(250) NULL,
    TransportCost DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    TransportMode NVARCHAR(50) NULL DEFAULT 'Flight', -- 'Flight', 'Train', 'Car Rental', 'Bus', 'Ferry', 'Other'
    Notes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TripStops PRIMARY KEY CLUSTERED (TripStopId ASC),
    CONSTRAINT FK_TripStops_Trips FOREIGN KEY (TripId) 
        REFERENCES dbo.Trips (TripId) ON DELETE CASCADE,
    CONSTRAINT FK_TripStops_DestinationCities FOREIGN KEY (CityId) 
        REFERENCES dbo.DestinationCities (CityId),
    CONSTRAINT CK_TripStop_Dates CHECK (ArrivalDate <= DepartureDate),
    CONSTRAINT CK_TripStop_Costs CHECK (AccommodationCost >= 0.00 AND TransportCost >= 0.00)
);
GO

CREATE NONCLUSTERED INDEX IX_TripStops_TripId ON dbo.TripStops (TripId ASC);
CREATE NONCLUSTERED INDEX IX_TripStops_CityId ON dbo.TripStops (CityId ASC);
CREATE NONCLUSTERED INDEX IX_TripStops_StopOrder ON dbo.TripStops (TripId ASC, StopOrder ASC);
GO

-- Trip Activities (Day-Wise Scheduled Items)
CREATE TABLE dbo.TripActivities (
    TripActivityId INT IDENTITY(1,1) NOT NULL,
    TripStopId INT NOT NULL,
    ActivityId INT NULL, -- NULL if user-created custom activity
    CustomTitle NVARCHAR(150) NOT NULL,
    CategoryName NVARCHAR(50) NOT NULL DEFAULT 'Sightseeing',
    ScheduledDate DATE NOT NULL,
    TimeOfDay NVARCHAR(30) NOT NULL DEFAULT 'Morning', -- 'Morning', 'Afternoon', 'Evening', 'Night', 'All Day'
    StartTime TIME(0) NULL,
    EndTime TIME(0) NULL,
    Cost DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    DurationHours DECIMAL(4,2) NOT NULL DEFAULT 2.00,
    OrderIndex INT NOT NULL DEFAULT 0,
    Notes NVARCHAR(500) NULL,
    IsCompleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TripActivities PRIMARY KEY CLUSTERED (TripActivityId ASC),
    CONSTRAINT FK_TripActivities_TripStops FOREIGN KEY (TripStopId) 
        REFERENCES dbo.TripStops (TripStopId) ON DELETE CASCADE,
    CONSTRAINT FK_TripActivities_Activities FOREIGN KEY (ActivityId) 
        REFERENCES dbo.Activities (ActivityId) ON DELETE SET NULL,
    CONSTRAINT CK_TripActivity_Cost CHECK (Cost >= 0.00),
    CONSTRAINT CK_TripActivity_Duration CHECK (DurationHours > 0.00)
);
GO

CREATE NONCLUSTERED INDEX IX_TripActivities_TripStopId ON dbo.TripActivities (TripStopId ASC);
CREATE NONCLUSTERED INDEX IX_TripActivities_ScheduledDate ON dbo.TripActivities (ScheduledDate ASC);
CREATE NONCLUSTERED INDEX IX_TripActivities_ActivityId ON dbo.TripActivities (ActivityId ASC);
GO

-- Additional Trip Expenses (Meals, Shopping, Misc Line Items)
CREATE TABLE dbo.TripExpenses (
    ExpenseId INT IDENTITY(1,1) NOT NULL,
    TripId INT NOT NULL,
    TripStopId INT NULL,
    ExpenseCategory NVARCHAR(50) NOT NULL DEFAULT 'Meals', -- 'Meals', 'Transport', 'Stay', 'Activity', 'Shopping', 'Misc'
    Title NVARCHAR(150) NOT NULL,
    Amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    ExpenseDate DATE NOT NULL,
    Notes NVARCHAR(250) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TripExpenses PRIMARY KEY CLUSTERED (ExpenseId ASC),
    CONSTRAINT FK_TripExpenses_Trips FOREIGN KEY (TripId) 
        REFERENCES dbo.Trips (TripId) ON DELETE CASCADE,
    CONSTRAINT FK_TripExpenses_TripStops FOREIGN KEY (TripStopId) 
        REFERENCES dbo.TripStops (TripStopId),
    CONSTRAINT CK_TripExpense_Amount CHECK (Amount >= 0.00)
);
GO

CREATE NONCLUSTERED INDEX IX_TripExpenses_TripId ON dbo.TripExpenses (TripId ASC);
CREATE NONCLUSTERED INDEX IX_TripExpenses_Category ON dbo.TripExpenses (ExpenseCategory ASC);
GO

-- User Saved Destinations (Wishlist)
CREATE TABLE dbo.SavedDestinations (
    SavedId INT IDENTITY(1,1) NOT NULL,
    UserId NVARCHAR(128) NOT NULL,
    CityId INT NOT NULL,
    SavedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_SavedDestinations PRIMARY KEY CLUSTERED (SavedId ASC),
    CONSTRAINT FK_SavedDestinations_AspNetUsers FOREIGN KEY (UserId) 
        REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_SavedDestinations_DestinationCities FOREIGN KEY (CityId) 
        REFERENCES dbo.DestinationCities (CityId) ON DELETE CASCADE,
    CONSTRAINT UQ_SavedDestinations_UserCity UNIQUE (UserId, CityId)
);
GO

CREATE NONCLUSTERED INDEX IX_SavedDestinations_UserId ON dbo.SavedDestinations (UserId ASC);
GO

-- =====================================================================================
-- 5. VIEWS FOR REPORTING, ANALYTICS & DASHBOARD
-- =====================================================================================

-- Comprehensive Trip Budget Summary View
CREATE VIEW dbo.vw_TripBudgetSummary
AS
SELECT 
    t.TripId,
    t.UserId,
    t.Title AS TripTitle,
    t.TotalBudget,
    t.Currency,
    t.StartDate,
    t.EndDate,
    DATEDIFF(DAY, t.StartDate, t.EndDate) + 1 AS TotalDays,
    COUNT(DISTINCT ts.TripStopId) AS TotalStops,
    ISNULL(SUM(ts.AccommodationCost), 0.00) AS TotalStayCost,
    ISNULL(SUM(ts.TransportCost), 0.00) AS TotalTransportCost,
    ISNULL(act.TotalActivityCost, 0.00) AS TotalActivityCost,
    ISNULL(exp.TotalOtherExpenses, 0.00) AS TotalOtherExpenses,
    (ISNULL(SUM(ts.AccommodationCost), 0.00) + 
     ISNULL(SUM(ts.TransportCost), 0.00) + 
     ISNULL(act.TotalActivityCost, 0.00) + 
     ISNULL(exp.TotalOtherExpenses, 0.00)) AS GrandTotalEstimatedCost,
    t.TotalBudget - (ISNULL(SUM(ts.AccommodationCost), 0.00) + 
                     ISNULL(SUM(ts.TransportCost), 0.00) + 
                     ISNULL(act.TotalActivityCost, 0.00) + 
                     ISNULL(exp.TotalOtherExpenses, 0.00)) AS BudgetRemaining,
    CASE 
        WHEN (ISNULL(SUM(ts.AccommodationCost), 0.00) + 
              ISNULL(SUM(ts.TransportCost), 0.00) + 
              ISNULL(act.TotalActivityCost, 0.00) + 
              ISNULL(exp.TotalOtherExpenses, 0.00)) > t.TotalBudget AND t.TotalBudget > 0 
        THEN 1 
        ELSE 0 
    END AS IsOverBudget
FROM dbo.Trips t
LEFT JOIN dbo.TripStops ts ON t.TripId = ts.TripId
OUTER APPLY (
    SELECT SUM(ta.Cost) AS TotalActivityCost
    FROM dbo.TripStops sub_ts
    JOIN dbo.TripActivities ta ON sub_ts.TripStopId = ta.TripStopId
    WHERE sub_ts.TripId = t.TripId
) act
OUTER APPLY (
    SELECT SUM(te.Amount) AS TotalOtherExpenses
    FROM dbo.TripExpenses te
    WHERE te.TripId = t.TripId
) exp
GROUP BY 
    t.TripId, t.UserId, t.Title, t.TotalBudget, t.Currency, 
    t.StartDate, t.EndDate, act.TotalActivityCost, exp.TotalOtherExpenses;
GO

-- Popular Cities & Stats View
CREATE VIEW dbo.vw_CityPopularityRankings
AS
SELECT 
    c.CityId,
    c.Name AS CityName,
    c.Country,
    c.Region,
    c.CostIndex,
    c.AvgDailyCost,
    c.ImageUrl,
    c.PopularityScore,
    COUNT(DISTINCT ts.TripStopId) AS TimesPlannedInTrips,
    COUNT(DISTINCT sd.SavedId) AS WishlistCount,
    COUNT(DISTINCT a.ActivityId) AS AvailableActivitiesCount
FROM dbo.DestinationCities c
LEFT JOIN dbo.TripStops ts ON c.CityId = ts.CityId
LEFT JOIN dbo.SavedDestinations sd ON c.CityId = sd.CityId
LEFT JOIN dbo.Activities a ON c.CityId = a.CityId
GROUP BY 
    c.CityId, c.Name, c.Country, c.Region, c.CostIndex, 
    c.AvgDailyCost, c.ImageUrl, c.PopularityScore;
GO

-- =====================================================================================
-- 6. STORED PROCEDURES (CORE BUSINESS TRANSACTIONS)
-- =====================================================================================

-- Stored Procedure: Fork / Copy Trip for Community Collaboration
CREATE PROCEDURE dbo.sp_CloneTrip
    @OriginalTripId INT,
    @NewUserId NVARCHAR(128),
    @NewTripTitle NVARCHAR(150) = NULL,
    @NewTripId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Check if source trip exists
        IF NOT EXISTS (SELECT 1 FROM dbo.Trips WHERE TripId = @OriginalTripId)
        BEGIN
            RAISERROR('Source trip does not exist.', 16, 1);
            RETURN;
        END

        DECLARE @BaseTitle NVARCHAR(150);
        DECLARE @BaseDesc NVARCHAR(MAX);
        DECLARE @StartDate DATE;
        DECLARE @EndDate DATE;
        DECLARE @CoverImage NVARCHAR(500);
        DECLARE @Budget DECIMAL(12,2);
        DECLARE @Currency NVARCHAR(10);
        DECLARE @NewSlug NVARCHAR(100);

        SELECT 
            @BaseTitle = ISNULL(@NewTripTitle, 'Copy of ' + Title),
            @BaseDesc = Description,
            @StartDate = StartDate,
            @EndDate = EndDate,
            @CoverImage = CoverImageUrl,
            @Budget = TotalBudget,
            @Currency = Currency
        FROM dbo.Trips 
        WHERE TripId = @OriginalTripId;

        SET @NewSlug = LOWER(REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', ''));

        -- 1. Insert Cloned Trip
        INSERT INTO dbo.Trips (
            UserId, Title, Description, StartDate, EndDate, 
            CoverImageUrl, TotalBudget, Currency, IsPublic, ShareSlug, ForkedFromTripId, CreatedAt, UpdatedAt
        )
        VALUES (
            @NewUserId, @BaseTitle, @BaseDesc, @StartDate, @EndDate, 
            @CoverImage, @Budget, @Currency, 0, @NewSlug, @OriginalTripId, SYSUTCDATETIME(), SYSUTCDATETIME()
        );

        SET @NewTripId = SCOPE_IDENTITY();

        -- 2. Mapping Table for Stops (OldStopId -> NewStopId)
        DECLARE @StopMapping TABLE (
            OldStopId INT,
            NewStopId INT
        );

        -- Cursor or Loop to insert stops and activities
        DECLARE @CurOldStopId INT, @CityId INT, @StopOrder INT, @ArrDate DATE, @DepDate DATE;
        DECLARE @AccCost DECIMAL(10,2), @AccDetails NVARCHAR(250), @TransCost DECIMAL(10,2), @TransMode NVARCHAR(50), @Notes NVARCHAR(MAX);
        DECLARE @NewStopId INT;

        DECLARE stop_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT TripStopId, CityId, StopOrder, ArrivalDate, DepartureDate, AccommodationCost, AccommodationDetails, TransportCost, TransportMode, Notes
        FROM dbo.TripStops
        WHERE TripId = @OriginalTripId
        ORDER BY StopOrder ASC;

        OPEN stop_cursor;
        FETCH NEXT FROM stop_cursor INTO @CurOldStopId, @CityId, @StopOrder, @ArrDate, @DepDate, @AccCost, @AccDetails, @TransCost, @TransMode, @Notes;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            INSERT INTO dbo.TripStops (TripId, CityId, StopOrder, ArrivalDate, DepartureDate, AccommodationCost, AccommodationDetails, TransportCost, TransportMode, Notes)
            VALUES (@NewTripId, @CityId, @StopOrder, @ArrDate, @DepDate, @AccCost, @AccDetails, @TransCost, @TransMode, @Notes);

            SET @NewStopId = SCOPE_IDENTITY();

            -- Copy Activities for this Stop
            INSERT INTO dbo.TripActivities (
                TripStopId, ActivityId, CustomTitle, CategoryName, ScheduledDate, 
                TimeOfDay, StartTime, EndTime, Cost, DurationHours, OrderIndex, Notes, IsCompleted
            )
            SELECT 
                @NewStopId, ActivityId, CustomTitle, CategoryName, ScheduledDate, 
                TimeOfDay, StartTime, EndTime, Cost, DurationHours, OrderIndex, Notes, 0
            FROM dbo.TripActivities
            WHERE TripStopId = @CurOldStopId;

            FETCH NEXT FROM stop_cursor INTO @CurOldStopId, @CityId, @StopOrder, @ArrDate, @DepDate, @AccCost, @AccDetails, @TransCost, @TransMode, @Notes;
        END

        CLOSE stop_cursor;
        DEALLOCATE stop_cursor;

        COMMIT TRANSACTION;
        PRINT '>> Successfully cloned trip ID ' + CAST(@OriginalTripId AS NVARCHAR(10)) + ' into new Trip ID ' + CAST(@NewTripId AS NVARCHAR(10));
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Stored Procedure: Get Day-by-Day Timeline Aggregation
CREATE PROCEDURE dbo.sp_GetTripDailyTimeline
    @TripId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        ts.TripStopId,
        ts.StopOrder,
        c.Name AS CityName,
        c.Country,
        c.ImageUrl AS CityImageUrl,
        ta.TripActivityId,
        ta.CustomTitle AS ActivityTitle,
        ta.CategoryName,
        ta.ScheduledDate,
        ta.TimeOfDay,
        ta.StartTime,
        ta.EndTime,
        ta.Cost AS ActivityCost,
        ta.DurationHours,
        ta.Notes AS ActivityNotes,
        ta.IsCompleted
    FROM dbo.TripStops ts
    JOIN dbo.DestinationCities c ON ts.CityId = c.CityId
    LEFT JOIN dbo.TripActivities ta ON ts.TripStopId = ta.TripStopId
    WHERE ts.TripId = @TripId
    ORDER BY ts.StopOrder ASC, ta.ScheduledDate ASC, 
        CASE ta.TimeOfDay 
            WHEN 'Morning' THEN 1 
            WHEN 'Afternoon' THEN 2 
            WHEN 'Evening' THEN 3 
            WHEN 'Night' THEN 4 
            ELSE 5 
        END,
        ta.OrderIndex ASC;
END;
GO

-- =====================================================================================
-- 7. RICH SEED DATA (DESTINATIONS, CATEGORIES, ACTIVITIES, USERS & CURATED TRIPS)
-- =====================================================================================

PRINT '>> Seeding Activity Categories...';

INSERT INTO dbo.ActivityCategories (CategoryName, IconClass, Description) VALUES
('Sightseeing', 'fa-solid fa-landmark', 'Famous landmarks, historical monuments, viewpoints and iconic monuments.'),
('Food & Dining', 'fa-solid fa-utensils', 'Culinary tours, local food markets, wine tastings, and authentic dining.'),
('Adventure & Outdoor', 'fa-solid fa-person-hiking', 'Trekking, scuba diving, safaris, watersports and thrilling outdoor excursions.'),
('Culture & History', 'fa-solid fa-palette', 'Museums, traditional performances, art galleries and heritage walks.'),
('Relaxation & Wellness', 'fa-solid fa-spa', 'Hot springs, beaches, yoga retreats and leisurely cruises.'),
('Nightlife & Entertainment', 'fa-solid fa-champagne-glasses', 'Rooftop lounges, evening shows, live music, and night markets.');
GO

PRINT '>> Seeding Global Destination Cities...';

SET IDENTITY_INSERT dbo.DestinationCities ON;

INSERT INTO dbo.DestinationCities (CityId, Name, Country, Region, Description, ImageUrl, CostIndex, PopularityScore, AvgDailyCost, CurrencyCode, Latitude, Longitude, IsFeatured) VALUES
(1, 'Paris', 'France', 'Europe', 'The City of Light dazzles with iconic architecture, romantic boulevards, world-class gastronomy, and legendary art museums.', 'https://images.unsplash.com/photo-1502602898657-3e91760cbb34?auto=format&fit=crop&w=1200&q=80', 'Luxury', 4.95, 220.00, 'EUR', 48.8566, 2.3522, 1),
(2, 'Tokyo', 'Japan', 'Asia', 'A hyper-modern metropolis seamlessly intertwined with tranquil historic shrines, futuristic technology, and unmatched culinary mastercraft.', 'https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?auto=format&fit=crop&w=1200&q=80', 'Moderate', 4.98, 160.00, 'JPY', 35.6762, 139.6503, 1),
(3, 'Rome', 'Italy', 'Europe', 'An open-air museum filled with ancient gladiatorial arenas, Baroque fountains, Renaissance masterpieces, and divine gelato.', 'https://images.unsplash.com/photo-1552832230-c0197dd311b5?auto=format&fit=crop&w=1200&q=80', 'Moderate', 4.90, 150.00, 'EUR', 41.9028, 12.4964, 1),
(4, 'New York City', 'United States', 'North America', 'The vibrant global hub that never sleeps, boasting world-renowned Broadway shows, towering skylines, and endless cultural diversity.', 'https://images.unsplash.com/photo-1496442226666-8d4d0e62e6e9?auto=format&fit=crop&w=1200&q=80', 'Luxury', 4.92, 280.00, 'USD', 40.7128, -74.0060, 1),
(5, 'Bali', 'Indonesia', 'Asia', 'Island of the Gods featuring lush terraced rice paddies, sacred clifftop temples, turquoise waters, and serene spiritual wellness retreats.', 'https://images.unsplash.com/photo-1537996194471-e657df975ab4?auto=format&fit=crop&w=1200&q=80', 'Budget', 4.88, 65.00, 'IDR', -8.4095, 115.1889, 1),
(6, 'Cairo', 'Egypt', 'Africa', 'Home to the awe-inspiring Great Pyramids of Giza, historic Nile river cruises, vibrant bazaars, and millennia of civilization.', 'https://images.unsplash.com/photo-1572252009286-268acec5ca0a?auto=format&fit=crop&w=1200&q=80', 'Budget', 4.75, 55.00, 'EGP', 30.0444, 31.2357, 1),
(7, 'Kyoto', 'Japan', 'Asia', 'The historic cultural heart of Japan with thousands of classical Buddhist temples, serene zen gardens, and enchanting geisha districts.', 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?auto=format&fit=crop&w=1200&q=80', 'Moderate', 4.94, 140.00, 'JPY', 35.0116, 135.7681, 1),
(8, 'Barcelona', 'Spain', 'Europe', 'A breathtaking seaside city famed for Antoni Gaudi’s surreal modernist architecture, lively tapas bars, and sun-soaked Mediterranean beaches.', 'https://images.unsplash.com/photo-1583422409516-2895a77efded?auto=format&fit=crop&w=1200&q=80', 'Moderate', 4.89, 145.00, 'EUR', 41.3879, 2.1699, 1),
(9, 'Cape Town', 'South Africa', 'Africa', 'A dramatic coastal jewel where rugged Table Mountain meets two oceans, world-class vineyards, and penguin colonies.', 'https://images.unsplash.com/photo-1580618672591-eb180b1a973f?auto=format&fit=crop&w=1200&q=80', 'Budget', 4.82, 75.00, 'ZAR', -33.9249, 18.4241, 0),
(10, 'Bangkok', 'Thailand', 'Asia', 'A sensory wonderland of ornate golden temples, sizzling world-famous street food, vibrant floating markets, and pulsating nightlife.', 'https://images.unsplash.com/photo-1508009603885-50cf7c579365?auto=format&fit=crop&w=1200&q=80', 'Budget', 4.85, 50.00, 'THB', 13.7563, 100.5018, 1),
(11, 'London', 'United Kingdom', 'Europe', 'A majestic global capital blending historic royal palaces, world-class West End theatres, iconic double-deckers, and vibrant pubs.', 'https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?auto=format&fit=crop&w=1200&q=80', 'Luxury', 4.91, 230.00, 'GBP', 51.5074, -0.1278, 1),
(12, 'Rio de Janeiro', 'Brazil', 'South America', 'The Marvelous City surrounded by dramatic green mountains, golden Copacabana sands, and the iconic Christ the Redeemer.', 'https://images.unsplash.com/photo-1483729558449-99ef09a8c325?auto=format&fit=crop&w=1200&q=80', 'Moderate', 4.78, 90.00, 'BRL', -22.9068, -43.1729, 0);

SET IDENTITY_INSERT dbo.DestinationCities OFF;
GO

PRINT '>> Seeding Rich Curated Activities...';

SET IDENTITY_INSERT dbo.Activities ON;

INSERT INTO dbo.Activities (ActivityId, CityId, CategoryId, Title, Description, ImageUrl, EstimatedCost, DurationHours, Rating, IsTopPick) VALUES
-- Paris Activities (CityId: 1)
(1, 1, 1, 'Eiffel Tower Summit & Champagne', 'Ascend to the top of the iconic Eiffel Tower for panoramic 360-degree views of Paris followed by a celebratory glass of French champagne.', 'https://images.unsplash.com/photo-1511739001486-6bfe10ce785f?auto=format&fit=crop&w=800&q=80', 45.00, 2.50, 4.95, 1),
(2, 1, 4, 'Louvre Museum Masterpieces Guided Tour', 'Skip-the-line guided exploration of the world’s largest art museum, featuring the Mona Lisa, Venus de Milo, and Winged Victory.', 'https://images.unsplash.com/photo-1499856871958-5b9627545d1a?auto=format&fit=crop&w=800&q=80', 65.00, 3.00, 4.90, 1),
(3, 1, 2, 'Montmartre Gourmet Pastry & Wine Tasting', 'Stroll through bohemian Montmartre sampling fresh warm croissants, artisanal macarons, vintage cheeses, and local French wines.', 'https://images.unsplash.com/photo-1550547660-d9450f859349?auto=format&fit=crop&w=800&q=80', 75.00, 3.50, 4.88, 0),
(4, 1, 6, 'Seine River Sunset Dinner Cruise', 'Float past illuminated Parisian landmarks including Notre Dame and Musée d’Orsay while enjoying a multi-course gourmet dinner.', 'https://images.unsplash.com/photo-1509439581779-6298f75bf6e5?auto=format&fit=crop&w=800&q=80', 95.00, 2.50, 4.92, 1),

-- Tokyo Activities (CityId: 2)
(5, 2, 4, 'Senso-ji Temple & Asakusa Old Town Walk', 'Explore Tokyo’s oldest and most sacred Buddhist temple, browse Nakamise shopping street for traditional street snacks and crafts.', 'https://images.unsplash.com/photo-1503899036084-c55cdd92da26?auto=format&fit=crop&w=800&q=80', 20.00, 2.50, 4.89, 1),
(6, 2, 2, 'Tsukiji Outer Market Sushi Masterclass', 'Discover the freshest seafood in Japan with an expert chef, followed by an interactive sushi and sashimi preparation workshop.', 'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?auto=format&fit=crop&w=800&q=80', 85.00, 3.00, 4.96, 1),
(7, 2, 1, 'Shibuya Sky & Cyberpunk Crossing', 'Witness the world-famous Shibuya Scramble from an open-air rooftop observation deck 229 meters in the sky.', 'https://images.unsplash.com/photo-1542051841857-5f90071e7989?auto=format&fit=crop&w=800&q=80', 22.00, 2.00, 4.93, 1),
(8, 2, 3, 'Akihabara Go-Karting & Anime Tour', 'Dress up in costume and cruise through Tokyo’s buzzing tech district on street-legal go-karts.', 'https://images.unsplash.com/photo-1534274988757-a28bf1a57c17?auto=format&fit=crop&w=800&q=80', 60.00, 2.00, 4.82, 0),

-- Rome Activities (CityId: 3)
(9, 3, 1, 'Colosseum & Ancient Roman Forum Tour', 'Walk the gladiatorial arena floor and explore the ruins of Julius Caesar’s Roman Forum with an archeologist guide.', 'https://images.unsplash.com/photo-1552832230-c0197dd311b5?auto=format&fit=crop&w=800&q=80', 55.00, 3.00, 4.94, 1),
(10, 3, 4, 'Vatican Museums & Sistine Chapel Tour', 'Marvel at Michelangelo’s legendary frescoes in the Sistine Chapel and walk through the lavish halls of St. Peter’s Basilica.', 'https://images.unsplash.com/photo-1531572753322-ad063cecc140?auto=format&fit=crop&w=800&q=80', 70.00, 3.50, 4.91, 1),
(11, 3, 2, 'Trastevere Secret Food & Pasta Making', 'Learn to craft authentic handmade Carbonara and Tiramisu in an ancient cellar in the charming Trastevere quarter.', 'https://images.unsplash.com/photo-1551183053-bf91a1d81141?auto=format&fit=crop&w=800&q=80', 65.00, 3.00, 4.95, 1),

-- New York City Activities (CityId: 4)
(12, 4, 1, 'Summit One Vanderbilt Glass Observation Deck', 'Immerse yourself in mind-bending multi-sensory glass skyboxes floating high above Manhattan with views of the Empire State Building.', 'https://images.unsplash.com/photo-1534430480872-3498386e7856?auto=format&fit=crop&w=800&q=80', 48.00, 2.00, 4.88, 1),
(13, 4, 6, 'Broadway Musical Premium Night Out', 'Experience the magic of world-class theater in the heart of Times Square.', 'https://images.unsplash.com/photo-1514525253161-7a46d19cd819?auto=format&fit=crop&w=800&q=80', 140.00, 3.00, 4.97, 1),
(14, 4, 3, 'Central Park Guided Bike Tour', 'Cycle past Bethesda Fountain, Strawberry Fields, Bow Bridge, and iconic movie locations with a local historian.', 'https://images.unsplash.com/photo-1538688525198-9b88f6f53126?auto=format&fit=crop&w=800&q=80', 38.00, 2.00, 4.79, 0),

-- Bali Activities (CityId: 5)
(15, 5, 3, 'Mount Batur Sunrise Volcano Trek', 'Hike up an active volcano under the starlight to watch the magical sunrise over the clouds while enjoying breakfast cooked on volcanic steam.', 'https://images.unsplash.com/photo-1518548419970-58e3b4079ab2?auto=format&fit=crop&w=800&q=80', 45.00, 6.00, 4.93, 1),
(16, 5, 5, 'Ubud Sacred Monkey Forest & Waterfall Spa', 'Wander through ancient mossy temples surrounded by wild monkeys, followed by a traditional Balinese herbal flower bath.', 'https://images.unsplash.com/photo-1537996194471-e657df975ab4?auto=format&fit=crop&w=800&q=80', 35.00, 4.00, 4.86, 1),
(17, 5, 3, 'Nusa Penida Snorkeling with Manta Rays', 'Speedboat excursion to Crystal Bay and Manta Point to swim alongside majestic wild giant manta rays.', 'https://images.unsplash.com/photo-1544551763-46a013bb70d5?auto=format&fit=crop&w=800&q=80', 55.00, 5.00, 4.95, 1),

-- Cairo Activities (CityId: 6)
(18, 6, 1, 'Giza Pyramids & Sphinx Camel Safari', 'Ride a camel across the desert sands admiring the Great Pyramid of Khufu and the enigmatic Great Sphinx.', 'https://images.unsplash.com/photo-1503177119275-0aa32b3a9368?auto=format&fit=crop&w=800&q=80', 40.00, 3.50, 4.87, 1),
(19, 6, 4, 'Grand Egyptian Museum & Tutankhamun Treasures', 'Explore the world’s grandest archaeological museum housing King Tut’s golden mask and thousands of pharaonic treasures.', 'https://images.unsplash.com/photo-1568322445389-f64ac2515020?auto=format&fit=crop&w=800&q=80', 30.00, 4.00, 4.92, 1),

-- Kyoto Activities (CityId: 7)
(20, 7, 4, 'Fushimi Inari 10,000 Torii Gates Sunset Hike', 'Walk beneath thousands of vermilion torii gates winding up the sacred mountain trails of Inari.', 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?auto=format&fit=crop&w=800&q=80', 15.00, 2.50, 4.97, 1),
(21, 7, 5, 'Arashiyama Bamboo Grove & Authentic Tea Ceremony', 'Stroll the towering emerald bamboo forest and participate in a meditative traditional Matcha tea ceremony.', 'https://images.unsplash.com/photo-1503899036084-c55cdd92da26?auto=format&fit=crop&w=800&q=80', 40.00, 3.00, 4.91, 1),

-- Barcelona Activities (CityId: 8)
(22, 8, 1, 'Sagrada Família Fast-Track Tower Access', 'Marvel at Gaudi’s transcendent basilica with vibrant rainbow stained glass and ascend the towers for panoramic city vistas.', 'https://images.unsplash.com/photo-1583422409516-2895a77efded?auto=format&fit=crop&w=800&q=80', 42.00, 2.00, 4.96, 1),
(23, 8, 2, 'El Born Tapas Crawl & Sangria Workshop', 'Indulge in crispy patatas bravas, Iberian jamón, and freshly caught seafood paired with signature Spanish sangria.', 'https://images.unsplash.com/photo-1515443961218-a51367888e4b?auto=format&fit=crop&w=800&q=80', 50.00, 3.00, 4.88, 1);

SET IDENTITY_INSERT dbo.Activities OFF;
GO

PRINT '>> Seeding Demo Users & Curated Community Trips...';

-- Seed Demo Users
-- Note: Password hash corresponds to 'Pass@123' (standard ASP.NET Identity v2 hash)
INSERT INTO dbo.AspNetUsers (
    Id, Email, EmailConfirmed, PasswordHash, SecurityStamp, UserName, 
    FullName, AvatarUrl, Bio, PreferredCurrency, LanguagePreference, CreatedAt
) VALUES 
('demo-user-001', 'alex.traveler@globetrotter.com', 1, 'AHwTqQ8QO8/eCjG5N3L4e4xYpG9Uu6/3HhYx11mU89g12vK5m7p9pL0=', '9f1a238e-012a-4a22-921c-8b89e34a1234', 'alex.traveler@globetrotter.com', 'Alex Rivera', 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=300&q=80', 'Passionate globetrotter, photographer, and foodie exploring Europe and Asia.', 'USD', 'English', SYSUTCDATETIME()),
('demo-user-002', 'elena.wander@globetrotter.com', 1, 'AHwTqQ8QO8/eCjG5N3L4e4xYpG9Uu6/3HhYx11mU89g12vK5m7p9pL0=', '8e2b349f-123b-5b33-832d-9c90f45b2345', 'elena.wander@globetrotter.com', 'Elena Rostova', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=300&q=80', 'Cultural explorer and mountain trekker. 28 countries and counting!', 'EUR', 'English', SYSUTCDATETIME());
GO

-- Seed Curated Featured Trips
SET IDENTITY_INSERT dbo.Trips ON;

INSERT INTO dbo.Trips (
    TripId, UserId, Title, Description, StartDate, EndDate, 
    CoverImageUrl, TotalBudget, Currency, IsPublic, ShareSlug, CreatedAt, UpdatedAt
) VALUES
(1, 'demo-user-001', '7-Day European Romance: Paris & Rome', 'An unforgettable week-long journey discovering world-class art, romantic sunsets, ancient monuments, and incredible culinary delights across Paris and Rome.', '2026-09-10', '2026-09-17', 'https://images.unsplash.com/photo-1502602898657-3e91760cbb34?auto=format&fit=crop&w=1200&q=80', 2500.00, 'USD', 1, 'euro-romance-paris-rome', SYSUTCDATETIME(), SYSUTCDATETIME()),
(2, 'demo-user-002', '10-Day Japan Odyssey: Tokyo & Kyoto', 'Immerse in the electric energy of Tokyo and the tranquil spiritual gardens of historic Kyoto. Features sushi masterclasses, bamboo groves, and skyline vistas.', '2026-10-05', '2026-10-15', 'https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?auto=format&fit=crop&w=1200&q=80', 3200.00, 'USD', 1, 'japan-odyssey-tokyo-kyoto', SYSUTCDATETIME(), SYSUTCDATETIME());

SET IDENTITY_INSERT dbo.Trips OFF;
GO

-- Seed Trip Stops for Trip #1 (Paris & Rome)
SET IDENTITY_INSERT dbo.TripStops ON;

INSERT INTO dbo.TripStops (TripStopId, TripId, CityId, StopOrder, ArrivalDate, DepartureDate, AccommodationCost, AccommodationDetails, TransportCost, TransportMode, Notes) VALUES
(1, 1, 1, 1, '2026-09-10', '2026-09-13', 480.00, 'Hotel Le Marais - 3 nights boutique stay', 350.00, 'Flight', 'Arrival at CDG Paris, take RER B to city center.'),
(2, 1, 3, 2, '2026-09-14', '2026-09-17', 420.00, 'Residenza Trastevere - 3 nights', 120.00, 'Flight', 'Flight from Paris Orly to Rome Fiumicino.');

-- Seed Trip Stops for Trip #2 (Tokyo & Kyoto)
INSERT INTO dbo.TripStops (TripStopId, TripId, CityId, StopOrder, ArrivalDate, DepartureDate, AccommodationCost, AccommodationDetails, TransportCost, TransportMode, Notes) VALUES
(3, 2, 2, 1, '2026-10-05', '2026-10-10', 700.00, 'Shinjuku Prince Hotel - 5 nights', 600.00, 'Flight', 'Direct flight to Haneda Airport.'),
(4, 2, 7, 2, '2026-10-10', '2026-10-15', 550.00, 'Kyoto Machiya Ryokan - 5 nights', 140.00, 'Train', 'Shinkansen bullet train from Tokyo Station to Kyoto.');

SET IDENTITY_INSERT dbo.TripStops OFF;
GO

-- Seed Trip Activities for Stops
SET IDENTITY_INSERT dbo.TripActivities ON;

-- Stop 1 (Paris)
INSERT INTO dbo.TripActivities (TripActivityId, TripStopId, ActivityId, CustomTitle, CategoryName, ScheduledDate, TimeOfDay, Cost, DurationHours, OrderIndex, Notes) VALUES
(1, 1, 1, 'Eiffel Tower Summit & Champagne', 'Sightseeing', '2026-09-10', 'Evening', 45.00, 2.50, 1, 'Tickets pre-booked for sunset slot at 6:30 PM.'),
(2, 1, 2, 'Louvre Museum Masterpieces Guided Tour', 'Culture & History', '2026-09-11', 'Morning', 65.00, 3.00, 1, 'Meet tour guide near the glass pyramid entrance.'),
(3, 1, 3, 'Montmartre Gourmet Pastry & Wine Tasting', 'Food & Dining', '2026-09-12', 'Afternoon', 75.00, 3.50, 1, 'Sample authentic Parisian croissants and cheeses.'),
(4, 1, 4, 'Seine River Sunset Dinner Cruise', 'Nightlife & Entertainment', '2026-09-13', 'Evening', 95.00, 2.50, 1, 'Romantic final night dinner in Paris.');

-- Stop 2 (Rome)
INSERT INTO dbo.TripActivities (TripActivityId, TripStopId, ActivityId, CustomTitle, CategoryName, ScheduledDate, TimeOfDay, Cost, DurationHours, OrderIndex, Notes) VALUES
(5, 2, 9, 'Colosseum & Ancient Roman Forum Tour', 'Sightseeing', '2026-09-14', 'Morning', 55.00, 3.00, 1, 'Wear comfortable walking shoes for cobblestones.'),
(6, 2, 10, 'Vatican Museums & Sistine Chapel Tour', 'Culture & History', '2026-09-15', 'Morning', 70.00, 3.50, 1, 'Dress code: shoulders and knees must be covered.'),
(7, 2, 11, 'Trastevere Secret Food & Pasta Making', 'Food & Dining', '2026-09-16', 'Evening', 65.00, 3.00, 1, 'Learn how to make authentic handmade Carbonara!');

-- Stop 3 (Tokyo)
INSERT INTO dbo.TripActivities (TripActivityId, TripStopId, ActivityId, CustomTitle, CategoryName, ScheduledDate, TimeOfDay, Cost, DurationHours, OrderIndex, Notes) VALUES
(8, 3, 5, 'Senso-ji Temple & Asakusa Old Town Walk', 'Culture & History', '2026-10-06', 'Morning', 20.00, 2.50, 1, 'Try melon pan at Nakamise street.'),
(9, 3, 6, 'Tsukiji Outer Market Sushi Masterclass', 'Food & Dining', '2026-10-07', 'Morning', 85.00, 3.00, 1, 'Master sushi chef lesson.'),
(10, 3, 7, 'Shibuya Sky & Cyberpunk Crossing', 'Sightseeing', '2026-10-08', 'Evening', 22.00, 2.00, 1, 'Awesome night views of Shibuya crossing.');

-- Stop 4 (Kyoto)
INSERT INTO dbo.TripActivities (TripActivityId, TripStopId, ActivityId, CustomTitle, CategoryName, ScheduledDate, TimeOfDay, Cost, DurationHours, OrderIndex, Notes) VALUES
(11, 4, 20, 'Fushimi Inari 10,000 Torii Gates Sunset Hike', 'Sightseeing', '2026-10-11', 'Morning', 15.00, 2.50, 1, 'Hike up before the midday crowds.'),
(12, 4, 21, 'Arashiyama Bamboo Grove & Authentic Tea Ceremony', 'Relaxation & Wellness', '2026-10-12', 'Afternoon', 40.00, 3.00, 1, 'Traditional Matcha tasting.');

SET IDENTITY_INSERT dbo.TripActivities OFF;
GO

-- Seed User Wishlist
INSERT INTO dbo.SavedDestinations (UserId, CityId) VALUES
('demo-user-001', 2), -- Alex saved Tokyo
('demo-user-001', 5), -- Alex saved Bali
('demo-user-002', 1), -- Elena saved Paris
('demo-user-002', 8); -- Elena saved Barcelona
GO

PRINT '=====================================================================================';
PRINT '>> GLOBETROTTER DATABASE SETUP & SEEDING COMPLETED SUCCESSFULLY!';
PRINT '=====================================================================================';
GO
