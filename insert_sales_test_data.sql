-- Insert Test Data for Sales Report
-- This script will add sample orders and order items for testing the sales report functionality

USE dev_Restaurant;
GO

-- First, let's check if we have menu items
IF NOT EXISTS (SELECT 1 FROM MenuItems)
BEGIN
    -- Insert some sample menu items
    INSERT INTO MenuItems (Name, Description, Price, CategoryId, IsAvailable, CreatedAt, UpdatedAt)
    VALUES 
    ('Chicken Biryani', 'Aromatic basmati rice with spiced chicken', 350.00, 1, 1, GETDATE(), GETDATE()),
    ('Butter Chicken', 'Creamy tomato-based chicken curry', 280.00, 1, 1, GETDATE(), GETDATE()),
    ('Paneer Tikka', 'Grilled cottage cheese with spices', 240.00, 1, 1, GETDATE(), GETDATE()),
    ('Dal Makhani', 'Rich and creamy black lentil curry', 180.00, 1, 1, GETDATE(), GETDATE()),
    ('Naan Bread', 'Soft and fluffy Indian bread', 60.00, 1, 1, GETDATE(), GETDATE()),
    ('Gulab Jamun', 'Sweet syrup-soaked milk dumplings', 120.00, 1, 1, GETDATE(), GETDATE());
END

-- Insert some test orders
DECLARE @UserId INT = (SELECT TOP 1 Id FROM Users WHERE Username = 'admin');

-- Order 1 - Today
INSERT INTO Orders (OrderNumber, OrderType, Status, UserId, CustomerName, CustomerPhone, Subtotal, TaxAmount, TipAmount, DiscountAmount, TotalAmount, CreatedAt, UpdatedAt)
VALUES ('ORD-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-001', 1, 2, @UserId, 'John Doe', '9876543210', 650.00, 65.00, 50.00, 0.00, 765.00, GETDATE(), GETDATE());

DECLARE @OrderId1 INT = SCOPE_IDENTITY();

-- Order items for Order 1
INSERT INTO OrderItems (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Status, CreatedAt, UpdatedAt)
SELECT @OrderId1, Id, 
    CASE 
        WHEN Name = 'Chicken Biryani' THEN 1
        WHEN Name = 'Butter Chicken' THEN 1
        ELSE 0
    END,
    Price,
    CASE 
        WHEN Name = 'Chicken Biryani' THEN Price * 1
        WHEN Name = 'Butter Chicken' THEN Price * 1
        ELSE 0
    END,
    2, GETDATE(), GETDATE()
FROM MenuItems 
WHERE Name IN ('Chicken Biryani', 'Butter Chicken');

-- Order 2 - Yesterday
INSERT INTO Orders (OrderNumber, OrderType, Status, UserId, CustomerName, CustomerPhone, Subtotal, TaxAmount, TipAmount, DiscountAmount, TotalAmount, CreatedAt, UpdatedAt)
VALUES ('ORD-' + FORMAT(DATEADD(DAY, -1, GETDATE()), 'yyyyMMdd') + '-001', 1, 2, @UserId, 'Jane Smith', '9876543211', 480.00, 48.00, 35.00, 20.00, 543.00, DATEADD(DAY, -1, GETDATE()), DATEADD(DAY, -1, GETDATE()));

DECLARE @OrderId2 INT = SCOPE_IDENTITY();

-- Order items for Order 2
INSERT INTO OrderItems (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Status, CreatedAt, UpdatedAt)
SELECT @OrderId2, Id, 
    CASE 
        WHEN Name = 'Paneer Tikka' THEN 2
        ELSE 0
    END,
    Price,
    CASE 
        WHEN Name = 'Paneer Tikka' THEN Price * 2
        ELSE 0
    END,
    2, DATEADD(DAY, -1, GETDATE()), DATEADD(DAY, -1, GETDATE())
FROM MenuItems 
WHERE Name = 'Paneer Tikka';

-- Order 3 - Two days ago
INSERT INTO Orders (OrderNumber, OrderType, Status, UserId, CustomerName, CustomerPhone, Subtotal, TaxAmount, TipAmount, DiscountAmount, TotalAmount, CreatedAt, UpdatedAt)
VALUES ('ORD-' + FORMAT(DATEADD(DAY, -2, GETDATE()), 'yyyyMMdd') + '-001', 1, 2, @UserId, 'Mike Johnson', '9876543212', 720.00, 72.00, 80.00, 0.00, 872.00, DATEADD(DAY, -2, GETDATE()), DATEADD(DAY, -2, GETDATE()));

DECLARE @OrderId3 INT = SCOPE_IDENTITY();

-- Order items for Order 3
INSERT INTO OrderItems (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Status, CreatedAt, UpdatedAt)
SELECT @OrderId3, Id, 
    CASE 
        WHEN Name = 'Chicken Biryani' THEN 1
        WHEN Name = 'Dal Makhani' THEN 2
        ELSE 0
    END,
    Price,
    CASE 
        WHEN Name = 'Chicken Biryani' THEN Price * 1
        WHEN Name = 'Dal Makhani' THEN Price * 2
        ELSE 0
    END,
    2, DATEADD(DAY, -2, GETDATE()), DATEADD(DAY, -2, GETDATE())
FROM MenuItems 
WHERE Name IN ('Chicken Biryani', 'Dal Makhani');

-- Order 4 - Last week (Active order)
INSERT INTO Orders (OrderNumber, OrderType, Status, UserId, CustomerName, CustomerPhone, Subtotal, TaxAmount, TipAmount, DiscountAmount, TotalAmount, CreatedAt, UpdatedAt)
VALUES ('ORD-' + FORMAT(DATEADD(DAY, -7, GETDATE()), 'yyyyMMdd') + '-001', 1, 1, @UserId, 'Sarah Wilson', '9876543213', 420.00, 42.00, 25.00, 10.00, 477.00, DATEADD(DAY, -7, GETDATE()), DATEADD(DAY, -7, GETDATE()));

DECLARE @OrderId4 INT = SCOPE_IDENTITY();

-- Order items for Order 4
INSERT INTO OrderItems (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Status, CreatedAt, UpdatedAt)
SELECT @OrderId4, Id, 
    CASE 
        WHEN Name = 'Butter Chicken' THEN 1
        WHEN Name = 'Naan Bread' THEN 2
        ELSE 0
    END,
    Price,
    CASE 
        WHEN Name = 'Butter Chicken' THEN Price * 1
        WHEN Name = 'Naan Bread' THEN Price * 2
        ELSE 0
    END,
    1, DATEADD(DAY, -7, GETDATE()), DATEADD(DAY, -7, GETDATE())
FROM MenuItems 
WHERE Name IN ('Butter Chicken', 'Naan Bread');

-- Order 5 - Cancelled order
INSERT INTO Orders (OrderNumber, OrderType, Status, UserId, CustomerName, CustomerPhone, Subtotal, TaxAmount, TipAmount, DiscountAmount, TotalAmount, CreatedAt, UpdatedAt)
VALUES ('ORD-' + FORMAT(DATEADD(DAY, -3, GETDATE()), 'yyyyMMdd') + '-001', 1, 3, @UserId, 'David Brown', '9876543214', 240.00, 24.00, 0.00, 0.00, 264.00, DATEADD(DAY, -3, GETDATE()), DATEADD(DAY, -3, GETDATE()));

DECLARE @OrderId5 INT = SCOPE_IDENTITY();

-- Order items for cancelled order
INSERT INTO OrderItems (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Status, CreatedAt, UpdatedAt)
SELECT @OrderId5, Id, 1, Price, Price, 3, DATEADD(DAY, -3, GETDATE()), DATEADD(DAY, -3, GETDATE())
FROM MenuItems 
WHERE Name = 'Paneer Tikka';

SELECT 'Test data inserted successfully!' AS Message;
SELECT COUNT(*) AS TotalOrders FROM Orders;
SELECT COUNT(*) AS TotalOrderItems FROM OrderItems;