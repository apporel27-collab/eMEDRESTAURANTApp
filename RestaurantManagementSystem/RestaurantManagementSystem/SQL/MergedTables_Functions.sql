-- Scalar function for reusable merged table name resolution
IF OBJECT_ID('dbo.fn_GetOrderMergedTables', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_GetOrderMergedTables;
GO

CREATE FUNCTION dbo.fn_GetOrderMergedTables (@OrderId INT)
RETURNS NVARCHAR(400)
AS
BEGIN
    DECLARE @mergedNames NVARCHAR(400);
    
    SELECT @mergedNames = STRING_AGG(t.TableName, ' + ') WITHIN GROUP (ORDER BY t.TableName)
    FROM OrderTables ot
    INNER JOIN Tables t ON ot.TableId = t.Id
    WHERE ot.OrderId = @OrderId;
    
    RETURN @mergedNames;
END;
GO

-- Update view to include merged tables fallback
IF OBJECT_ID('vw_OrderMergedTables') IS NOT NULL
    DROP VIEW vw_OrderMergedTables;
GO

CREATE VIEW vw_OrderMergedTables AS
SELECT 
    o.Id AS OrderId,
    CASE 
        WHEN merged.MergedTableNames IS NOT NULL THEN merged.MergedTableNames
        WHEN o.OrderType = 0 AND t.TableName IS NOT NULL THEN t.TableName
        WHEN o.OrderType = 0 AND t.TableNumber IS NOT NULL THEN CAST(t.TableNumber AS VARCHAR(10))
        ELSE NULL
    END AS TableDisplayName
FROM Orders o
LEFT JOIN TableTurnovers tt ON o.TableTurnoverId = tt.Id
LEFT JOIN Tables t ON tt.TableId = t.Id
LEFT JOIN (
    SELECT 
        ot.OrderId,
        STRING_AGG(t2.TableName, ' + ') WITHIN GROUP (ORDER BY t2.TableName) AS MergedTableNames
    FROM OrderTables ot
    INNER JOIN Tables t2 ON ot.TableId = t2.Id
    GROUP BY ot.OrderId
) merged ON o.Id = merged.OrderId;
GO

-- Optional: Backfill existing single-table orders into OrderTables for consistency
-- Run this only once after confirming the OrderTables structure is in place
/*
INSERT INTO OrderTables (OrderId, TableId)
SELECT DISTINCT o.Id, t.Id
FROM Orders o
INNER JOIN TableTurnovers tt ON o.TableTurnoverId = tt.Id
INNER JOIN Tables t ON tt.TableId = t.Id
LEFT JOIN OrderTables ot ON ot.OrderId = o.Id AND ot.TableId = t.Id
WHERE o.OrderType = 0  -- Dine-in orders only
  AND ot.Id IS NULL;   -- Not already in OrderTables
*/