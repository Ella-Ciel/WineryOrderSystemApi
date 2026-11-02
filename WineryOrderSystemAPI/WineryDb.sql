----CREATE DATABASE WineryDb;

--Use WineryDb;

---- Roles: User and Admin
--CREATE TABLE Roles (
--    RoleId INT IDENTITY(1,1) PRIMARY KEY,
--    RoleName VARCHAR(15) NOT NULL UNIQUE
--);

---- Users Information
--CREATE TABLE Users (
--    UserId INT IDENTITY(1,1) PRIMARY KEY,
--    FullName VARCHAR(100) NOT NULL,
--    Email VARCHAR(50) NOT NULL UNIQUE,
--    Phone VARCHAR(13) NULL,
--    Address VARCHAR(255) NULL,
--    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
--    RoleId INT NOT NULL CONSTRAINT FK_Users_Roles FOREIGN KEY REFERENCES Roles(RoleId) ON DELETE CASCADE
--);

---- Wines
--CREATE TABLE Wines (
--    ProductId INT IDENTITY(1,1) PRIMARY KEY,
--    WineName VARCHAR(100) NOT NULL,
--    Description VARCHAR(500) NULL,
--    Price DECIMAL(18,2) NOT NULL,
--    StockQuantity INT NOT NULL DEFAULT 0,
--    Category NVARCHAR(50) NULL,
--    CreatedAt DATETIME DEFAULT GETDATE()
--);

---- Orders
--CREATE TABLE Orders (
--    OrderId INT IDENTITY(1,1) PRIMARY KEY,
--    UserId INT NOT NULL CONSTRAINT FK_Orders_Users FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
--    OrderDate DATETIME DEFAULT GETDATE(),
--    TotalAmount DECIMAL(18,2) NOT NULL,
--    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending'
--);

---- Order Items
--CREATE TABLE OrderItems (
--    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
--    OrderId INT NOT NULL CONSTRAINT FK_OrderItems_Orders FOREIGN KEY REFERENCES Orders(OrderId) ON DELETE CASCADE,
--    ProductId INT NOT NULL CONSTRAINT FK_OrderItems_Wines FOREIGN KEY REFERENCES Wines(ProductId),
--    Quantity INT NOT NULL,
--    UnitPrice DECIMAL(18,2) NOT NULL
--);

---- Seed initial Roles
--INSERT INTO Roles (RoleName) VALUES ('Admin'), ('Customer');

--select * from roles;
--select * from Users;
--select * from Wines;
--select * from Orders;
--select * from OrderItems;

---- 2. Seed Users (5 Customers: RoleId = 2, 3 Admins: RoleId = 1)
--INSERT INTO Users (FullName, Email, Phone, Address, RoleId) VALUES 
--('Natsu Dragneel', 'natsu.dragneel@fairytail.guild', '09171234561', 'Magnolia Town, Fiore', 2),
--('Lucy Heartfilia', 'lucy.heartfilia@fairytail.guild', '09171234562', 'Lucy Apartment, Magnolia', 2),
--('Gray Fullbuster', 'gray.fullbuster@fairytail.guild', '09171234563', 'Ice Village, Fiore', 2),
--('Wendy Marvell', 'wendy.marvell@fairytail.guild', '09171234564', 'Cait Shelter, Fiore', 2),
--('Erza Scarlet', 'erza.scarlet@fairytail.guild', '09171234566', 'Fairy Hills Dorm, Magnolia', 1),
--('Makarov Dreyar', 'makarov.dreyar@fairytail.guild', '09171234567', 'Guild Master Hall, Magnolia', 1),
--('Mirajane Strauss', 'mirajane.strauss@fairytail.guild', '09171234568', 'Fairy Tail Bar, Magnolia', 1);
--GO

---- 3. Seed Wines
--INSERT INTO Wines (WineName, Description, Price, StockQuantity, Category) VALUES 
--('Penfolds Grange Shiraz', 'Iconic Australian full-bodied red wine with notes of dark berries, chocolate, and spice.', 38000.00, 10, 'Red Wine'),
--('Cloudy Bay Sauvignon Blanc', 'Famous New Zealand white wine with vibrant lime, passionfruit, and crisp acidity.', 2400.00, 45, 'White Wine'),
--('Dom Pérignon Vintage Champagne', 'Prestigious French vintage champagne with delicate notes of brioche, citrus, and minerality.', 15500.00, 15, 'Sparkling Wine'),
--('Whispering Angel Rosé', 'Classic Côtes de Provence rosé with subtle strawberry, peach, and floral notes.', 1850.00, 60, 'Rosé Wine'),
--('Robert Mondavi Cabernet Sauvignon', 'Rich Napa Valley red wine with flavors of black currant, plum, oak, and cedar.', 3200.00, 30, 'Red Wine');
--GO

---- 4. Seed Orders
--INSERT INTO Orders (UserId, TotalAmount, Status) VALUES 
--(1, 6400.00, 'Completed'),   -- Natsu: 2x Robert Mondavi Cabernet Sauvignon
--(2, 4250.00, 'Pending'),     -- Lucy: 1x Cloudy Bay + 1x Whispering Angel
--(3, 15500.00, 'Processing'),  -- Gray: 1x Dom Pérignon Vintage Champagne
--(4, 2400.00, 'Completed'),   -- Wendy: 1x Cloudy Bay Sauvignon Blanc
--(5, 38000.00, 'Completed');  -- Juvia: 1x Penfolds Grange Shiraz
--GO

---- 5. Seed OrderItems
--INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice) VALUES 
---- Order 1 (Natsu)
--(1, 5, 2, 3200.00),

---- Order 2 (Lucy)
--(2, 2, 1, 2400.00),
--(2, 4, 1, 1850.00),

---- Order 3 (Gray)
--(3, 3, 1, 15500.00),

---- Order 4 (Wendy)
--(4, 2, 1, 2400.00),

---- Order 5 (Juvia)
--(5, 1, 1, 38000.00);
--GO


--USE WineryDb;

---- Customers and Admins require secure credential storage.
--ALTER TABLE Users ADD PasswordHash VARCHAR(255) NOT NULL DEFAULT '';

--ALTER TABLE Users ADD PasswordSalt VARCHAR(255) NOT NULL DEFAULT '';

--ALTER TABLE Users ADD CONSTRAINT DF_Users_RoleId DEFAULT 2 FOR RoleId;

---- AI FAQ Chatbot Tables
--CREATE TABLE FAQs (
--    FaqId INT IDENTITY(1,1) PRIMARY KEY,
--    Question NVARCHAR(255) NOT NULL,
--    Answer NVARCHAR(MAX) NOT NULL
--);

--CREATE TABLE ChatSessions (
--    SessionId INT IDENTITY(1,1) PRIMARY KEY,
--    UserId INT NOT NULL CONSTRAINT FK_ChatSessions_Users FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
--    StartedAt DATETIME DEFAULT GETDATE()
--);

--CREATE TABLE ChatMessages (
--    MessageId INT IDENTITY(1,1) PRIMARY KEY,
--    SessionId INT NOT NULL CONSTRAINT FK_ChatMessages_ChatSessions FOREIGN KEY REFERENCES ChatSessions(SessionId) ON DELETE CASCADE,
--    MessageText NVARCHAR(MAX) NOT NULL,
--    IsBot BIT NOT NULL, -- 0 for User, 1 for AI Bot
--    SentAt DATETIME DEFAULT GETDATE()
--);

----CREATE DATABASE WineryDb_Replica
--USE WineryDb_Replica;

--CREATE TABLE Dim_Wine (
--    WineKey INT IDENTITY(1,1) PRIMARY KEY,
--    ProductId INT,
--    WineName VARCHAR(100),
--    Category NVARCHAR(50)
--);

--CREATE TABLE Dim_Customer (
--    CustomerKey INT IDENTITY(1,1) PRIMARY KEY,
--    UserId INT,
--    FullName VARCHAR(100),
--    Address VARCHAR(255)
--);

--CREATE TABLE Dim_Time (
--    TimeKey INT IDENTITY(1,1) PRIMARY KEY,
--    Date DATETIME,
--    Month INT,
--    Quarter INT,
--    Year INT
--);

--CREATE TABLE Fact_Sales (
--    SalesId INT IDENTITY(1,1) PRIMARY KEY,
--    WineKey INT NOT NULL FOREIGN KEY REFERENCES Dim_Wine(WineKey),
--    CustomerKey INT NOT NULL FOREIGN KEY REFERENCES Dim_Customer(CustomerKey),
--    TimeKey INT NOT NULL FOREIGN KEY REFERENCES Dim_Time(TimeKey),
--    QuantitySold INT NOT NULL,
--    TotalRevenue DECIMAL(18,2) NOT NULL
--);
--GO


USE WineryDb_Replica;
-- 2. Clone schema and current data from Primary
SELECT * INTO WineryDb_Replica.dbo.Roles FROM WineryDb.dbo.Roles;
SELECT * INTO WineryDb_Replica.dbo.Users FROM WineryDb.dbo.Users;
SELECT * INTO WineryDb_Replica.dbo.Wines FROM WineryDb.dbo.Wines;
SELECT * INTO WineryDb_Replica.dbo.Orders FROM WineryDb.dbo.Orders;
SELECT * INTO WineryDb_Replica.dbo.OrderItems FROM WineryDb.dbo.OrderItems;

USE WineryDb_Replica;
GO

-- Verify table existence in Replica
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';

select * from faqs

INSERT INTO FAQs (Question, Answer) VALUES
('What is the price range of your wines?', 'Our wines range from ₱1,850.00 for our Whispering Angel Rosé up to ₱38,000.00 for our premium Penfolds Grange Shiraz.'),
('How long is the shelf life of unopened wine?', 'Unopened red wines generally last 2–3 years beyond the printed expiration date, white wines 1–2 years, and fine vintage champagnes/reds can age gracefully for 10–20+ years when stored in a cool, dark environment.'),
('How can I check if a wine is in stock?', 'You can browse our online catalog under the /api/Wines/catalog endpoint to view real-time stock quantities for every bottle.'),
('Do you offer delivery to my address?', 'Yes! We deliver across Metro Manila and surrounding provinces. Standard delivery takes 2 to 4 business days.'),
('How should I store wine after opening?', 'Once opened, recork the bottle tightly and store red wines in a cool place for 3–5 days. White, rosé, and sparkling wines should be refrigerated and consumed within 1–3 days.');