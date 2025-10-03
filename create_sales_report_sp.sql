-- Sales Report Stored Procedure
-- This procedure generates comprehensive sales reports with date filtering and optional user filtering

CREATE OR ALTER PROCEDURE usp_GetSalesReport
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Set default dates if not provided (last 30 days)
    IF @StartDate IS NULL
        SET @StartDate = DATEADD(DAY, -30, GETDATE())
    
    IF @EndDate IS NULL
        SET @EndDate = GETDATE()
    
    -- Ensure end date includes the entire day
    SET @EndDate = DATEADD(DAY, 1, @EndDate)
    
    -- Summary Statistics
    SELECT 
        COUNT(DISTINCT o.Id) AS TotalOrders,
        ISNULL(SUM(o.TotalAmount), 0) AS TotalSales,
        ISNULL(AVG(o.TotalAmount), 0) AS AverageOrderValue,
        ISNULL(SUM(o.Subtotal), 0) AS TotalSubtotal,
        ISNULL(SUM(o.TaxAmount), 0) AS TotalTax,
        ISNULL(SUM(o.TipAmount), 0) AS TotalTips,
        ISNULL(SUM(o.DiscountAmount), 0) AS TotalDiscounts,
        COUNT(DISTINCT CASE WHEN o.Status = 2 THEN o.Id END) AS CompletedOrders,
        COUNT(DISTINCT CASE WHEN o.Status = 3 THEN o.Id END) AS CancelledOrders
    FROM Orders o
    LEFT JOIN Users u ON o.UserId = u.Id
    WHERE o.CreatedAt >= @StartDate 
        AND o.CreatedAt < @EndDate
        AND (@UserId IS NULL OR o.UserId = @UserId);
    
    -- Daily Sales Breakdown
    SELECT 
        CAST(o.CreatedAt AS DATE) AS SalesDate,
        COUNT(DISTINCT o.Id) AS OrderCount,
        ISNULL(SUM(o.TotalAmount), 0) AS DailySales,
        ISNULL(AVG(o.TotalAmount), 0) AS AvgOrderValue
    FROM Orders o
    LEFT JOIN Users u ON o.UserId = u.Id
    WHERE o.CreatedAt >= @StartDate 
        AND o.CreatedAt < @EndDate
        AND (@UserId IS NULL OR o.UserId = @UserId)
    GROUP BY CAST(o.CreatedAt AS DATE)
    ORDER BY SalesDate DESC;
    
    -- Top Menu Items
    SELECT TOP 10
        mi.Name AS ItemName,
        mi.Id AS MenuItemId,
        SUM(oi.Quantity) AS TotalQuantitySold,
        ISNULL(SUM(oi.Subtotal), 0) AS TotalRevenue,
        ISNULL(AVG(oi.UnitPrice), 0) AS AveragePrice,
        COUNT(DISTINCT oi.OrderId) AS OrderCount
    FROM OrderItems oi
    INNER JOIN Orders o ON oi.OrderId = o.Id
    INNER JOIN MenuItems mi ON oi.MenuItemId = mi.Id
    LEFT JOIN Users u ON o.UserId = u.Id
    WHERE o.CreatedAt >= @StartDate 
        AND o.CreatedAt < @EndDate
        AND o.Status IN (1, 2) -- Active and Completed orders only
        AND (@UserId IS NULL OR o.UserId = @UserId)
    GROUP BY mi.Id, mi.Name
    ORDER BY TotalRevenue DESC;
    
    -- Sales by User (Server Performance)
    SELECT 
        ISNULL(u.FirstName + ' ' + u.LastName, 'Walk-in Customer') AS ServerName,
        ISNULL(u.Username, 'N/A') AS Username,
        u.Id AS UserId,
        COUNT(DISTINCT o.Id) AS OrderCount,
        ISNULL(SUM(o.TotalAmount), 0) AS TotalSales,
        ISNULL(AVG(o.TotalAmount), 0) AS AvgOrderValue,
        ISNULL(SUM(o.TipAmount), 0) AS TotalTips
    FROM Orders o
    LEFT JOIN Users u ON o.UserId = u.Id
    WHERE o.CreatedAt >= @StartDate 
        AND o.CreatedAt < @EndDate
        AND (@UserId IS NULL OR o.UserId = @UserId)
    GROUP BY u.Id, u.FirstName, u.LastName, u.Username
    ORDER BY TotalSales DESC;
    
    -- Order Status Distribution
    SELECT 
        CASE o.Status 
            WHEN 0 THEN 'Pending'
            WHEN 1 THEN 'Active'
            WHEN 2 THEN 'Completed'
            WHEN 3 THEN 'Cancelled'
            ELSE 'Unknown'
        END AS OrderStatus,
        COUNT(*) AS OrderCount,
        ISNULL(SUM(o.TotalAmount), 0) AS TotalAmount,
        ROUND((COUNT(*) * 100.0 / 
            (SELECT COUNT(*) FROM Orders WHERE CreatedAt >= @StartDate AND CreatedAt < @EndDate AND (@UserId IS NULL OR UserId = @UserId))), 2) AS Percentage
    FROM Orders o
    WHERE o.CreatedAt >= @StartDate 
        AND o.CreatedAt < @EndDate
        AND (@UserId IS NULL OR o.UserId = @UserId)
    GROUP BY o.Status
    ORDER BY OrderCount DESC;
    
    -- Hourly Sales Pattern
    SELECT 
        DATEPART(HOUR, o.CreatedAt) AS HourOfDay,
        COUNT(DISTINCT o.Id) AS OrderCount,
        ISNULL(SUM(o.TotalAmount), 0) AS HourlySales,
        ISNULL(AVG(o.TotalAmount), 0) AS AvgOrderValue
    FROM Orders o
    WHERE o.CreatedAt >= @StartDate 
        AND o.CreatedAt < @EndDate
        AND (@UserId IS NULL OR o.UserId = @UserId)
    GROUP BY DATEPART(HOUR, o.CreatedAt)
    ORDER BY HourOfDay;
    
END