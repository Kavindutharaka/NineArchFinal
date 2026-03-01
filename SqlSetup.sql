-- =============================================
-- 9 Arch Tours - Database Setup Script
-- Run this in SQL Server to create database & tables
-- =============================================

-- Create database (run separately if needed)
-- CREATE DATABASE NineArchTours;
-- GO
-- USE NineArchTours;
-- GO

-- =============================================
-- TABLES
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Locations')
CREATE TABLE Locations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LocationImages')
CREATE TABLE LocationImages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LocationId INT NOT NULL,
    ImagePath NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (LocationId) REFERENCES Locations(Id) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Hotels')
CREATE TABLE Hotels (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    StarRating NVARCHAR(50),
    FoodOptions NVARCHAR(50),
    Location NVARCHAR(200),
    Description NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HotelImages')
CREATE TABLE HotelImages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    HotelId INT NOT NULL,
    ImagePath NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (HotelId) REFERENCES Hotels(Id) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Vehicles')
CREATE TABLE Vehicles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Model NVARCHAR(200) NOT NULL,
    Seating INT,
    Luggage INT,
    AirCon NVARCHAR(10),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VehicleImages')
CREATE TABLE VehicleImages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    VehicleId INT NOT NULL,
    ImagePath NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ItineraryTemplates')
CREATE TABLE ItineraryTemplates (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RouteName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Highlights NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);

GO

-- =============================================
-- STORED PROCEDURES
-- =============================================

-- Get all locations with images
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetLocations')
    DROP PROCEDURE sp_GetLocations;
GO
CREATE PROCEDURE sp_GetLocations
AS
BEGIN
    SELECT l.Id, l.Name, l.Description,
           STUFF((SELECT '|' + li.ImagePath FROM LocationImages li WHERE li.LocationId = l.Id FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '') AS Images
    FROM Locations l
    ORDER BY l.Name;
END
GO

-- Insert location
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_InsertLocation')
    DROP PROCEDURE sp_InsertLocation;
GO
CREATE PROCEDURE sp_InsertLocation
    @Name NVARCHAR(200),
    @Description NVARCHAR(MAX)
AS
BEGIN
    INSERT INTO Locations (Name, Description) VALUES (@Name, @Description);
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- Update location
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_UpdateLocation')
    DROP PROCEDURE sp_UpdateLocation;
GO
CREATE PROCEDURE sp_UpdateLocation
    @Id INT,
    @Name NVARCHAR(200),
    @Description NVARCHAR(MAX)
AS
BEGIN
    UPDATE Locations SET Name = @Name, Description = @Description WHERE Id = @Id;
    SELECT @Id AS Id;
END
GO

-- Delete location
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_DeleteLocation')
    DROP PROCEDURE sp_DeleteLocation;
GO
CREATE PROCEDURE sp_DeleteLocation
    @Id INT
AS
BEGIN
    DELETE FROM Locations WHERE Id = @Id;
    SELECT @Id AS DeletedId;
END
GO

-- Insert location image
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_InsertLocationImage')
    DROP PROCEDURE sp_InsertLocationImage;
GO
CREATE PROCEDURE sp_InsertLocationImage
    @LocationId INT,
    @ImagePath NVARCHAR(500)
AS
BEGIN
    INSERT INTO LocationImages (LocationId, ImagePath) VALUES (@LocationId, @ImagePath);
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- Get all hotels with images
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetHotels')
    DROP PROCEDURE sp_GetHotels;
GO
CREATE PROCEDURE sp_GetHotels
AS
BEGIN
    SELECT h.Id, h.Name, h.StarRating, h.FoodOptions, h.Location, h.Description,
           STUFF((SELECT '|' + hi.ImagePath FROM HotelImages hi WHERE hi.HotelId = h.Id FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '') AS Images
    FROM Hotels h
    ORDER BY h.Name;
END
GO

-- Insert hotel
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_InsertHotel')
    DROP PROCEDURE sp_InsertHotel;
GO
CREATE PROCEDURE sp_InsertHotel
    @Name NVARCHAR(200),
    @StarRating NVARCHAR(50),
    @FoodOptions NVARCHAR(50),
    @Location NVARCHAR(200),
    @Description NVARCHAR(MAX)
AS
BEGIN
    INSERT INTO Hotels (Name, StarRating, FoodOptions, Location, Description)
    VALUES (@Name, @StarRating, @FoodOptions, @Location, @Description);
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- Update hotel
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_UpdateHotel')
    DROP PROCEDURE sp_UpdateHotel;
GO
CREATE PROCEDURE sp_UpdateHotel
    @Id INT,
    @Name NVARCHAR(200),
    @StarRating NVARCHAR(50),
    @FoodOptions NVARCHAR(50),
    @Location NVARCHAR(200),
    @Description NVARCHAR(MAX)
AS
BEGIN
    UPDATE Hotels SET Name=@Name, StarRating=@StarRating, FoodOptions=@FoodOptions,
                      Location=@Location, Description=@Description WHERE Id=@Id;
    SELECT @Id AS Id;
END
GO

-- Delete hotel
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_DeleteHotel')
    DROP PROCEDURE sp_DeleteHotel;
GO
CREATE PROCEDURE sp_DeleteHotel
    @Id INT
AS
BEGIN
    DELETE FROM Hotels WHERE Id = @Id;
    SELECT @Id AS DeletedId;
END
GO

-- Insert hotel image
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_InsertHotelImage')
    DROP PROCEDURE sp_InsertHotelImage;
GO
CREATE PROCEDURE sp_InsertHotelImage
    @HotelId INT,
    @ImagePath NVARCHAR(500)
AS
BEGIN
    INSERT INTO HotelImages (HotelId, ImagePath) VALUES (@HotelId, @ImagePath);
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- Get all vehicles
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetVehicles')
    DROP PROCEDURE sp_GetVehicles;
GO
CREATE PROCEDURE sp_GetVehicles
AS
BEGIN
    SELECT v.Id, v.Model, v.Seating, v.Luggage, v.AirCon,
           STUFF((SELECT '|' + vi.ImagePath FROM VehicleImages vi WHERE vi.VehicleId = v.Id FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '') AS Images
    FROM Vehicles v
    ORDER BY v.Model;
END
GO

-- Insert vehicle image
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_InsertVehicleImage')
    DROP PROCEDURE sp_InsertVehicleImage;
GO
CREATE PROCEDURE sp_InsertVehicleImage
    @VehicleId INT,
    @ImagePath NVARCHAR(500)
AS
BEGIN
    INSERT INTO VehicleImages (VehicleId, ImagePath) VALUES (@VehicleId, @ImagePath);
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- Insert vehicle
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_InsertVehicle')
    DROP PROCEDURE sp_InsertVehicle;
GO
CREATE PROCEDURE sp_InsertVehicle
    @Model NVARCHAR(200),
    @Seating INT,
    @Luggage INT,
    @AirCon NVARCHAR(10)
AS
BEGIN
    INSERT INTO Vehicles (Model, Seating, Luggage, AirCon) VALUES (@Model, @Seating, @Luggage, @AirCon);
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- Get all itinerary templates
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetItineraryTemplates')
    DROP PROCEDURE sp_GetItineraryTemplates;
GO
CREATE PROCEDURE sp_GetItineraryTemplates
AS
BEGIN
    SELECT Id, RouteName, Description, Highlights FROM ItineraryTemplates ORDER BY Id;
END
GO

-- Insert itinerary template
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_InsertItineraryTemplate')
    DROP PROCEDURE sp_InsertItineraryTemplate;
GO
CREATE PROCEDURE sp_InsertItineraryTemplate
    @RouteName NVARCHAR(200),
    @Description NVARCHAR(MAX),
    @Highlights NVARCHAR(500)
AS
BEGIN
    INSERT INTO ItineraryTemplates (RouteName, Description, Highlights) VALUES (@RouteName, @Description, @Highlights);
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- Delete itinerary template
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_DeleteItineraryTemplate')
    DROP PROCEDURE sp_DeleteItineraryTemplate;
GO
CREATE PROCEDURE sp_DeleteItineraryTemplate
    @Id INT
AS
BEGIN
    DELETE FROM ItineraryTemplates WHERE Id = @Id;
    SELECT @Id AS DeletedId;
END
GO

-- =============================================
-- SAMPLE DATA
-- =============================================

-- Locations
INSERT INTO Locations (Name, Description) VALUES
('Negombo', 'Coastal town with beautiful beaches, Negombo Lagoon boat rides, Dutch Canal, and fresh seafood restaurants.'),
('Kandy', 'Sacred city featuring Temple of the Tooth Relic, Kandy Lake, Upper Lake viewpoint, and traditional cultural dance performances.'),
('Nuwara Eliya', 'Hill country paradise with tea plantations, Gregory Lake, Victoria Park, colonial architecture, and cool climate.'),
('Colombo', 'Vibrant capital city with Galle Face Green, Independence Square, Gangaramaya Temple, modern shopping malls.'),
('Pinnawala', 'Famous elephant orphanage where you can observe elephants bathing and feeding.'),
('Ella', 'Scenic hill town with Nine Arch Bridge, Little Adams Peak, Ravana Falls, and tea plantations.'),
('Sigiriya', 'Ancient rock fortress UNESCO World Heritage Site with stunning frescoes, Mirror Wall, and panoramic views.'),
('Galle', 'Historic fort city with Dutch colonial architecture, lighthouse, art galleries, and beautiful beaches.'),
('Yala', 'Yala National Park - Sri Lankas premier wildlife sanctuary, home to leopards, elephants, and exotic birds.'),
('Bentota', 'Golden beach paradise with water sports, river safaris, turtle hatchery, and luxury resorts.');
GO

-- Hotels
INSERT INTO Hotels (Name, StarRating, FoodOptions, Location, Description) VALUES
('Vivanta Airport Garden Hotel', '5 Star', 'BB', 'Negombo', 'A luxurious 5-star property near Bandaranaike International Airport, offering modern amenities and easy access to Negombo Beach.'),
('The Golden Crown Hotel', '5 Star', 'BB', 'Kandy', 'The Golden Crown Hotel captures the proud unique Kandyan history in a modern contemporary setting near the sacred tooth relic.'),
('The Golden Ridge Hotel', '5 Star', 'BB', 'Nuwara Eliya', 'Nestled in the misty hills, offering breathtaking views of tea plantations and mountains with colonial charm and modern luxury.'),
('Courtyard by Marriott', '5 Star', 'BB', 'Colombo', 'Located in the heart of Colombo, offering contemporary luxury with panoramic city views near shopping and cultural attractions.'),
('Taj Bentota Resort & Spa', '5 Star', 'HB', 'Bentota', 'Set on the golden beaches of Bentota, this resort offers a world-class spa, water sports, and exquisite dining experiences.'),
('Heritance Kandalama', '5 Star', 'BB', 'Sigiriya', 'A Geoffrey Bawa masterpiece built into a rock face overlooking Kandalama Lake with stunning views of Sigiriya Rock.'),
('Cinnamon Wild Yala', '4 Star', 'FB', 'Yala', 'An eco-lodge bordering Yala National Park, offering a unique wildlife experience with safari excursions.'),
('Jetwing Lighthouse', '5 Star', 'BB', 'Galle', 'A stunning Geoffrey Bawa-designed hotel perched on a rocky headland south of Galle Fort with beautiful ocean views.'),
('98 Acres Resort & Spa', '4 Star', 'BB', 'Ella', 'Perched on a scenic hilltop in Ella, offering panoramic views of Ella Rock and Little Adams Peak surrounded by tea plantations.'),
('Araliya Green City', '5 Star', 'BB', 'Nuwara Eliya', 'Situated in Nuwara Eliya with restaurant, shared lounge, flat-screen TV, and proximity to Hakgala Botanical Garden.');
GO

-- Vehicles
INSERT INTO Vehicles (Model, Seating, Luggage, AirCon) VALUES
('Toyota KDH High Roof', 9, 10, 'Yes'),
('Toyota Prius', 3, 3, 'Yes'),
('Toyota HiAce (Flat Roof)', 6, 6, 'Yes'),
('Toyota Coaster', 27, 20, 'Yes'),
('Mercedes Benz V-Class', 6, 6, 'Yes');
GO

-- Itinerary Templates
INSERT INTO ItineraryTemplates (RouteName, Description, Highlights) VALUES
('Arrival / Negombo', '- Meet & greet on arrival at Bandaranaike International Airport
- Transfer to Negombo (approx. 15 mins)
- Check-in and relax at the hotel
- Evening leisure walk along Negombo Beach
- Boat ride through Negombo Lagoon or Dutch Canal', 'Coastal vibes, seafood dining, relaxation'),

('Negombo → Kandy', '- After breakfast at hotel, drive to Kandy (approx. 3.5 hrs)
- En route visit Pinnawala Elephant Orphanage
- Stop at Spice Garden - Mawanella
- Afternoon city tour of Kandy:
  - Temple of the Sacred Tooth Relic
  - Kandy Lake & Upper Lake View Point
  - Cultural Dance Performance', 'Heritage, culture, scenic views'),

('Kandy → Nuwara Eliya', '- After breakfast at hotel, drive to Nuwara Eliya (approx. 2.5 hrs)
- Visit Ramboda Falls en route
- Stop at Tea Factory & Plantation for a tour and tasting
- Gregory Lake, Victoria Park, Colonial Post Office', 'Tea estates, waterfalls, hill-country charm'),

('Nuwara Eliya → Colombo', '- After breakfast at hotel, drive to Colombo (approx. 5.5 hrs)
- Stop at Devon and St. Clairs Waterfalls
- Afternoon Colombo City Tour:
  - Galle Face Green, Independence Square
  - Old Parliament, Gangaramaya Temple
  - Shopping at One Galle Face Mall', 'Shopping, dining, and modern city life'),

('Departure', '- Breakfast at hotel
- Free time for last-minute shopping or leisure
- Transfer to Bandaranaike International Airport for departure', 'Farewell to Sri Lanka'),

('Negombo → Sigiriya', '- After breakfast at hotel, drive to Sigiriya (approx. 4 hrs)
- Climb the iconic Sigiriya Rock Fortress (UNESCO)
- Explore the ancient frescoes and Mirror Wall
- Visit Pidurangala Rock for panoramic views
- Evening safari at Minneriya National Park', 'Ancient ruins, rock fortress, elephant safari'),

('Sigiriya → Kandy', '- After breakfast at hotel, drive to Kandy (approx. 2.5 hrs)
- En route visit Dambulla Cave Temple (UNESCO)
- Stop at a Spice Garden in Matale
- Afternoon Kandy city tour', 'Cave temples, spices, Kandyan culture'),

('Kandy → Ella (by train)', '- After breakfast, board the scenic train to Ella
- One of the most beautiful train rides in the world
- Pass through tea plantations, tunnels, and bridges
- Arrive in Ella and check in at hotel', 'Iconic train ride, tea country, Nine Arch Bridge'),

('Ella Day Tour', '- Visit Nine Arch Bridge
- Hike to Little Adams Peak
- Visit Ravana Falls
- Explore local tea estates and Demodara Loop', 'Nine Arch Bridge, waterfalls, hiking'),

('Ella → Yala', '- After breakfast, drive to Yala (approx. 2.5 hrs)
- Afternoon Yala National Park Safari
- Spot leopards, elephants, crocodiles, and exotic birds', 'Wildlife safari, leopard spotting, nature'),

('Yala → Bentota via Galle', '- Drive to Bentota (approx. 4 hrs)
- En route visit Galle Fort (UNESCO)
- Walk through the charming streets of Galle
- Arrive in Bentota, beach leisure', 'Galle Fort, golden beaches, water sports'),

('Bentota → Colombo', '- Drive to Colombo (approx. 2 hrs)
- Visit Madu River Boat Safari en route
- Colombo City Tour:
  - Galle Face Green, Gangaramaya Temple
  - National Museum, Shopping', 'River safari, city tour, shopping');
GO

PRINT 'Database setup complete!';
GO
