-- =====================================================================================
-- GLOBETROTTER - ADDITIONAL DUMMY ACTIVITIES SEED SCRIPT
-- Run this AFTER the main schema has been created and seeded.
-- Adds 30+ more activities across all cities, plus extra trip stops & scheduled activities
-- =====================================================================================

USE GlobeTrotterDB;
GO

PRINT '>> Adding more Activities catalog entries...';

-- Existing max ActivityId is 23. We start from 24.
SET IDENTITY_INSERT dbo.Activities ON;

INSERT INTO dbo.Activities (ActivityId, CityId, CategoryId, Title, Description, ImageUrl, EstimatedCost, DurationHours, Rating, IsTopPick) VALUES

-- Paris (CityId: 1)
(24, 1, 5, 'Versailles Palace & Gardens Full Day', 'Explore the opulent Palace of Versailles, the Hall of Mirrors, and the spectacular French formal gardens.', 'https://images.unsplash.com/photo-1551613261-3f7dc68e4e1f?auto=format&fit=crop&w=800&q=80', 80.00, 6.00, 4.91, 1),
(25, 1, 3, 'Paris Catacombs Underground Tour', 'Descend 20 metres beneath the city streets into the eerie ossuary holding the bones of over 6 million Parisians.', 'https://images.unsplash.com/photo-1541963463532-d68292c34b19?auto=format&fit=crop&w=800&q=80', 30.00, 2.00, 4.80, 0),
(26, 1, 6, 'Moulin Rouge Cabaret Show', 'The world-famous Montmartre cabaret - feathers, sequins, and spectacular French can-can.', 'https://images.unsplash.com/photo-1494526585095-c41746248156?auto=format&fit=crop&w=800&q=80', 120.00, 2.50, 4.87, 1),

-- Tokyo (CityId: 2)
(27, 2, 5, 'Meiji Shrine Morning Meditation Walk', 'A serene walk through the forested Meiji Shrine complex with a private blessing ceremony by Shinto priests.', 'https://images.unsplash.com/photo-1528360983277-13d401cdc186?auto=format&fit=crop&w=800&q=80', 10.00, 2.00, 4.88, 0),
(28, 2, 2, 'Ramen & Izakaya Alley Crawl in Shinjuku', 'Navigate Golden Gai narrow alleyways sampling authentic tonkotsu ramen, gyoza, and Japanese craft whisky.', 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80', 55.00, 3.00, 4.93, 1),
(29, 2, 6, 'TeamLab Borderless Digital Art Museum', 'An immersive world of floating lights, flowing water, and computer-generated art that reacts to your body.', 'https://images.unsplash.com/photo-1542744173-8e7e53415bb0?auto=format&fit=crop&w=800&q=80', 35.00, 3.00, 4.97, 1),

-- Rome (CityId: 3)
(30, 3, 5, 'Trevi Fountain & Gelato Walking Tour', 'Toss a coin in the baroque Trevi Fountain then discover Rome best gelaterias in a 2-hour stroll.', 'https://images.unsplash.com/photo-1529154036614-a60975f5c760?auto=format&fit=crop&w=800&q=80', 30.00, 2.50, 4.85, 0),
(31, 3, 6, 'Rome Aperitivo & Night Food Tour', 'Enjoy classic Aperol Spritz and bruschetta in piazzas, ending at a rooftop bar with views over the Forum.', 'https://images.unsplash.com/photo-1414235077428-338989a2e8c0?auto=format&fit=crop&w=800&q=80', 60.00, 3.00, 4.89, 1),

-- New York City (CityId: 4)
(32, 4, 1, 'Statue of Liberty & Ellis Island Ferry', 'Cross New York Harbour to Lady Liberty and explore the moving immigration history of Ellis Island.', 'https://images.unsplash.com/photo-1485738422979-f5c462d49f74?auto=format&fit=crop&w=800&q=80', 25.00, 4.00, 4.84, 1),
(33, 4, 2, 'Chelsea Market Food Hall & High Line Walk', 'Sample artisan cheeses, lobster rolls, and craft burritos then stroll the elevated garden railway above Chelsea.', 'https://images.unsplash.com/photo-1477959858617-67f85cf4f1df?auto=format&fit=crop&w=800&q=80', 45.00, 3.00, 4.78, 0),
(34, 4, 6, 'Brooklyn Jazz Bar & Rooftop Night Tour', 'An evening of live bebop jazz in Williamsburg followed by a rooftop cocktail with Manhattan skyline views.', 'https://images.unsplash.com/photo-1522383225653-ed111181a951?auto=format&fit=crop&w=800&q=80', 80.00, 3.50, 4.91, 1),

-- Bali (CityId: 5)
(35, 5, 5, 'Tanah Lot Sunset Temple Ceremony', 'Witness a sacred Balinese Hindu ritual at the cliff-edge sea temple as the sun dips into the Indian Ocean.', 'https://images.unsplash.com/photo-1518548419970-58e3b4079ab2?auto=format&fit=crop&w=800&q=80', 20.00, 2.50, 4.90, 1),
(36, 5, 2, 'Balinese Cooking Class in Seminyak', 'Shop for fresh spices at a local market then learn to cook 5 authentic Balinese dishes with a chef.', 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80', 55.00, 4.00, 4.94, 1),
(37, 5, 3, 'Bali Surfing Lesson at Kuta Beach', 'Take a beginner surf lesson with an expert local surf coach on the gentle waves of Kuta Beach.', 'https://images.unsplash.com/photo-1510798831971-661eb04b3739?auto=format&fit=crop&w=800&q=80', 35.00, 2.50, 4.82, 0),

-- Cairo (CityId: 6)
(38, 6, 2, 'Khan el-Khalili Bazaar Spice & Street Food Tour', 'Wander Cairo legendary medieval market and taste koshari, falafel, and karkadeh with a local foodie guide.', 'https://images.unsplash.com/photo-1553913861-c0fddf2619ee?auto=format&fit=crop&w=800&q=80', 25.00, 3.00, 4.80, 0),
(39, 6, 6, 'Nile Felucca Sunset Cruise & Dinner', 'Sail the timeless Nile River on a traditional wooden felucca boat as Cairo skyline glows at dusk.', 'https://images.unsplash.com/photo-1571992249655-8e2e86d1ed88?auto=format&fit=crop&w=800&q=80', 35.00, 2.50, 4.83, 1),

-- Kyoto (CityId: 7)
(40, 7, 4, 'Gion Geisha District Night Walk', 'Explore the lantern-lit cobblestone alleys of Gion and catch a glimpse of geiko and maiko entertainers.', 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?auto=format&fit=crop&w=800&q=80', 20.00, 2.50, 4.88, 1),
(41, 7, 5, 'Philosopher Path Cherry Blossom Stroll', 'Walk along the scenic canal-side path from Nanzen-ji to Ginkaku-ji through thousands of sakura trees.', 'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?auto=format&fit=crop&w=800&q=80', 5.00, 2.00, 4.93, 1),
(42, 7, 2, 'Traditional Kaiseki Dinner Experience', 'A multi-course seasonal Japanese fine dining experience in a 300-year-old Machiya townhouse.', 'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80', 110.00, 2.50, 4.96, 1),

-- Barcelona (CityId: 8)
(43, 8, 1, 'Park Guell Mosaic Terraces & City Views', 'Explore Gaudi dreamlike park, wander colourful ceramic mosaic sculptures and enjoy sweeping views.', 'https://images.unsplash.com/photo-1523531294919-4bcd7c65e216?auto=format&fit=crop&w=800&q=80', 14.00, 2.00, 4.83, 0),
(44, 8, 6, 'Barceloneta Beach Sunset Paella & Flamenco', 'Feast on authentic paella at a beachfront restaurant then enjoy a passionate live flamenco performance.', 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80', 75.00, 3.00, 4.90, 1),
(45, 8, 3, 'Montjuic Castle Cable Car & Paragliding', 'Ascend Montjuic by cable car then take a tandem paraglide off the hilltop for thrilling coastal views.', 'https://images.unsplash.com/photo-1576610616656-d3aa5d1f4534?auto=format&fit=crop&w=800&q=80', 95.00, 3.50, 4.86, 0),

-- Cape Town (CityId: 9)
(46, 9, 3, 'Table Mountain Aerial Cableway Hike', 'Hike or ride the revolving cable car to the flat-topped summit of Table Mountain for 360 ocean views.', 'https://images.unsplash.com/photo-1580818439868-1da7f85e6c19?auto=format&fit=crop&w=800&q=80', 28.00, 4.00, 4.92, 1),
(47, 9, 3, 'Boulders Beach Penguin Colony Visit', 'Walk boardwalks past hundreds of African penguins nesting on a pristine white sand beach.', 'https://images.unsplash.com/photo-1553337515-e47ff9d14cf2?auto=format&fit=crop&w=800&q=80', 15.00, 2.00, 4.87, 1),
(48, 9, 2, 'Cape Winelands Stellenbosch & Franschhoek Tour', 'Visit award-winning wine estates, cycle between vineyards, and enjoy a gourmet cheese platter.', 'https://images.unsplash.com/photo-1474722883778-792e7990302f?auto=format&fit=crop&w=800&q=80', 90.00, 7.00, 4.91, 1),

-- Bangkok (CityId: 10)
(49, 10, 4, 'Grand Palace & Wat Phra Kaew Royal Temple', 'Marvel at the sacred Emerald Buddha and the dazzling gilded spires of Bangkok holiest royal complex.', 'https://images.unsplash.com/photo-1508009603885-50cf7c579365?auto=format&fit=crop&w=800&q=80', 18.00, 3.00, 4.91, 1),
(50, 10, 2, 'Floating Market & Street Food Tuk-Tuk Tour', 'Zip through traffic on a tuk-tuk stopping at Damnoen Saduak floating market and best street food stalls.', 'https://images.unsplash.com/photo-1547981609-4b6bfe67ca0b?auto=format&fit=crop&w=800&q=80', 45.00, 4.00, 4.89, 1),
(51, 10, 6, 'Chao Phraya River Dinner Cruise', 'Cruise the luminous river with Thai classical dance, live music, and a 5-course set dinner buffet.', 'https://images.unsplash.com/photo-1508193638397-1c4234db14d8?auto=format&fit=crop&w=800&q=80', 60.00, 3.00, 4.85, 0),

-- London (CityId: 11)
(52, 11, 1, 'Tower of London & Crown Jewels Tour', 'Explore 1,000 years of history in the medieval tower, including the dazzling Crown Jewels vault.', 'https://images.unsplash.com/photo-1543799813-0574d7e18de0?auto=format&fit=crop&w=800&q=80', 35.00, 3.00, 4.86, 1),
(53, 11, 2, 'Borough Market Gourmet Food Crawl', 'London oldest food market: sample artisan cheese, smoked meats, fresh oysters, and warm beef brisket.', 'https://images.unsplash.com/photo-1533900298318-6b8da08a523e?auto=format&fit=crop&w=800&q=80', 40.00, 2.50, 4.82, 0),
(54, 11, 6, 'West End Musical & Theatreland Night Out', 'Experience world-class theatre in the heart of London, from Hamilton to The Lion King.', 'https://images.unsplash.com/photo-1503095396549-807759245b35?auto=format&fit=crop&w=800&q=80', 110.00, 3.00, 4.94, 1),

-- Rio de Janeiro (CityId: 12)
(55, 12, 1, 'Christ the Redeemer Sunrise Tour', 'Ascend Corcovado by private van at dawn for an intimate encounter with the iconic statue above the clouds.', 'https://images.unsplash.com/photo-1483729558449-99ef09a8c325?auto=format&fit=crop&w=800&q=80', 40.00, 3.00, 4.95, 1),
(56, 12, 3, 'Copacabana Beach Volleyball & Caipirinhas', 'Play beach volleyball on the most iconic sand in the world then cool off with fresh lemon caipirinhas.', 'https://images.unsplash.com/photo-1519923041107-d113f5c5e4b8?auto=format&fit=crop&w=800&q=80', 20.00, 3.00, 4.79, 0),
(57, 12, 6, 'Rio Samba School Night & Carnival Rehearsal', 'Join a live rehearsal at one of Rio legendary samba schools and learn the basic samba steps.', 'https://images.unsplash.com/photo-1516750105099-4b8a83e217ee?auto=format&fit=crop&w=800&q=80', 55.00, 3.00, 4.90, 1);

SET IDENTITY_INSERT dbo.Activities OFF;
GO

PRINT '>> Activities catalog expanded. Adding extra TripStops and TripActivities...';

-- Extra TripStops: Bali extension for Trip 1, NYC extension for Trip 2
SET IDENTITY_INSERT dbo.TripStops ON;
INSERT INTO dbo.TripStops (TripStopId, TripId, CityId, StopOrder, ArrivalDate, DepartureDate, AccommodationCost, AccommodationDetails, TransportCost, TransportMode, Notes) VALUES
(5, 1, 5, 3, '2026-09-18', '2026-09-22', 380.00, 'Alaya Resort Ubud - 4 nights', 450.00, 'Flight', 'Rome to Bali via Singapore. Relax and unwind after Europe.'),
(6, 2, 4, 3, '2026-10-16', '2026-10-19', 850.00, 'Mandarin Oriental New York - 3 nights', 650.00, 'Flight', 'Extend the trip to New York before flying home.');
SET IDENTITY_INSERT dbo.TripStops OFF;
GO

-- Extra TripActivities for the new stops + richer existing stops
SET IDENTITY_INSERT dbo.TripActivities ON;

-- Stop 5 (Bali extension for Trip 1)
INSERT INTO dbo.TripActivities (TripActivityId, TripStopId, ActivityId, CustomTitle, CategoryName, ScheduledDate, TimeOfDay, StartTime, EndTime, Cost, DurationHours, OrderIndex, Notes) VALUES
(13, 5, 15, 'Mount Batur Sunrise Trek', 'Adventure & Outdoor', '2026-09-18', 'Morning', '04:00', '10:00', 45.00, 6.00, 1, 'Start at 4 AM to catch the sunrise above the clouds.'),
(14, 5, 16, 'Ubud Monkey Forest & Spa', 'Relaxation & Wellness', '2026-09-19', 'Afternoon', '13:00', '17:00', 35.00, 4.00, 1, 'Balinese flower bath after the forest walk.'),
(15, 5, 35, 'Tanah Lot Sunset Temple', 'Culture & History', '2026-09-20', 'Evening', '17:30', '20:00', 20.00, 2.50, 1, 'Arrive early to find a good spot at the clifftop.'),
(16, 5, 36, 'Balinese Cooking Class', 'Food & Dining', '2026-09-21', 'Morning', '09:00', '13:00', 55.00, 4.00, 2, 'Market shopping first, then cook 5 dishes!'),
(17, 5, 37, 'Kuta Beach Surf Lesson', 'Adventure & Outdoor', '2026-09-21', 'Afternoon', '15:00', '17:30', 35.00, 2.50, 1, 'Beginner-friendly gentle waves.'),
(18, 5, 17, 'Nusa Penida Snorkeling', 'Adventure & Outdoor', '2026-09-22', 'Morning', '07:00', '12:00', 55.00, 5.00, 1, 'Last big activity before the flight home!');

-- Stop 6 (New York extension for Trip 2)
INSERT INTO dbo.TripActivities (TripActivityId, TripStopId, ActivityId, CustomTitle, CategoryName, ScheduledDate, TimeOfDay, StartTime, EndTime, Cost, DurationHours, OrderIndex, Notes) VALUES
(19, 6, 12, 'Summit One Vanderbilt', 'Sightseeing', '2026-10-16', 'Afternoon', '14:00', '16:00', 48.00, 2.00, 1, 'Pre-book timed-entry tickets.'),
(20, 6, 32, 'Statue of Liberty Ferry', 'Sightseeing', '2026-10-17', 'Morning', '09:00', '13:00', 25.00, 4.00, 1, 'Buy ferry tickets in advance online.'),
(21, 6, 33, 'Chelsea Market & High Line', 'Food & Dining', '2026-10-17', 'Afternoon', '14:30', '17:30', 45.00, 3.00, 2, 'Great for shots on the elevated park!'),
(22, 6, 13, 'Broadway Musical Night Out', 'Nightlife & Entertainment', '2026-10-17', 'Evening', '19:00', '22:00', 140.00, 3.00, 3, 'Book Hamilton tickets weeks in advance!'),
(23, 6, 14, 'Central Park Bike Tour', 'Adventure & Outdoor', '2026-10-18', 'Morning', '09:00', '11:00', 38.00, 2.00, 1, 'Leisurely last morning before airport transfer.'),
(24, 6, 34, 'Brooklyn Jazz & Rooftop Bar', 'Nightlife & Entertainment', '2026-10-18', 'Evening', '20:00', '23:00', 80.00, 3.50, 2, 'Best Manhattan skyline view from Brooklyn side!');

-- Richer activities on existing Stop 3 (Tokyo) and Stop 4 (Kyoto)
INSERT INTO dbo.TripActivities (TripActivityId, TripStopId, ActivityId, CustomTitle, CategoryName, ScheduledDate, TimeOfDay, StartTime, EndTime, Cost, DurationHours, OrderIndex, Notes) VALUES
(25, 3, 27, 'Meiji Shrine Morning Walk', 'Culture & History', '2026-10-05', 'Morning', '07:00', '09:00', 10.00, 2.00, 0, 'Start the trip with a calming shrine visit.'),
(26, 3, 28, 'Shinjuku Ramen Alley Crawl', 'Food & Dining', '2026-10-09', 'Evening', '19:00', '22:00', 55.00, 3.00, 2, 'Golden Gai is a must-visit Tokyo gem.'),
(27, 3, 29, 'TeamLab Borderless Museum', 'Nightlife & Entertainment', '2026-10-09', 'Afternoon', '14:00', '17:00', 35.00, 3.00, 1, 'Book digital art museum tickets online.'),
(28, 4, 40, 'Gion Geisha Night Walk', 'Culture & History', '2026-10-13', 'Evening', '18:30', '21:00', 20.00, 2.50, 2, 'Best chance to spot geiko in Gion Kobu.'),
(29, 4, 41, 'Philosophers Path Walk', 'Relaxation & Wellness', '2026-10-14', 'Morning', '08:00', '10:00', 5.00, 2.00, 1, 'Peaceful morning stroll along the canal.'),
(30, 4, 42, 'Kaiseki Fine Dining Dinner', 'Food & Dining', '2026-10-14', 'Evening', '19:00', '21:30', 110.00, 2.50, 2, 'Traditional multi-course Japanese dinner.');

SET IDENTITY_INSERT dbo.TripActivities OFF;
GO

PRINT '=====================================================================================';
PRINT '>> DONE! Added 34 new Activities, 2 new TripStops, 18 new TripActivities.';
PRINT '=====================================================================================';
GO
