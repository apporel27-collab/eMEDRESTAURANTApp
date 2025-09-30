-- SQL script to create RestaurantSettings table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'RestaurantSettings')
BEGIN
    DROP TABLE RestaurantSettings;
    PRINT 'Dropped existing RestaurantSettings table to recreate with updated schema.';
END

CREATE TABLE RestaurantSettings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RestaurantName NVARCHAR(100) NOT NULL,
    StreetAddress NVARCHAR(200) NOT NULL,
    City NVARCHAR(50) NOT NULL,
    State NVARCHAR(50) NOT NULL,
    Pincode NVARCHAR(10) NOT NULL,
    Country NVARCHAR(50) NOT NULL,
    GSTCode NVARCHAR(15) NOT NULL,
    PhoneNumber NVARCHAR(15) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Website NVARCHAR(100) NULL,
    LogoPath NVARCHAR(200) NULL,
    CurrencySymbol NVARCHAR(50) DEFAULT '₹',
    DefaultGSTPercentage DECIMAL(5,2) DEFAULT 5.00,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);

-- Insert default settings
INSERT INTO RestaurantSettings (
    RestaurantName, StreetAddress, City, State, Pincode, Country, 
    GSTCode, PhoneNumber, Email, Website, CurrencySymbol, DefaultGSTPercentage
)
VALUES (
    'My Restaurant', '123 Main St', 'Mumbai', 'Maharashtra', '400001', 'India',
    '27AAPFU0939F1ZV', '9876543210', 'contact@myrestaurant.com', 'www.myrestaurant.com', '₹', 5.00
);

PRINT 'RestaurantSettings table created and default data inserted.';