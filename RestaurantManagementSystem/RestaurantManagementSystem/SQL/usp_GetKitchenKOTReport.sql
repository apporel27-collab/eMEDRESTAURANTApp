-- Stored procedure: usp_GetKitchenKOTReport
-- Parameters: @FromDate DATE = NULL, @ToDate DATE = NULL, @Station NVARCHAR(100) = NULL
-- Returns: KOT items for kitchen display/export

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.usp_GetKitchenKOTReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetKitchenKOTReport
GO

CREATE PROCEDURE dbo.usp_GetKitchenKOTReport
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @Station NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Start DATETIME = COALESCE(CAST(@FromDate AS DATETIME), DATEADD(day, -1, CAST(GETDATE() AS DATE)));
    DECLARE @End DATETIME = DATEADD(day, 1, COALESCE(CAST(@ToDate AS DATETIME), CAST(GETDATE() AS DATE)));

    SELECT
        o.Id AS OrderId,
        o.OrderNumber,
        ISNULL(t.TableName, CONCAT('Table ', o.TableTurnoverId)) AS TableName,
        i.Name AS ItemName,
        oi.Quantity,
        ISNULL(s.Name, '') AS Station,
        CASE WHEN oi.IsPrepared = 1 THEN 'Completed' ELSE 'Pending' END AS Status,
        oi.RequestedAt
    FROM OrderItems oi
    INNER JOIN Orders o ON oi.OrderId = o.Id
    LEFT JOIN MenuItems i ON oi.MenuItemId = i.Id
    LEFT JOIN Stations s ON i.StationId = s.Id
    LEFT JOIN Tables t ON o.TableTurnoverId = t.Id
    WHERE oi.RequestedAt >= @Start AND oi.RequestedAt < @End
    AND (@Station IS NULL OR @Station = '' OR s.Name = @Station)
    ORDER BY oi.RequestedAt DESC;
END
GO
