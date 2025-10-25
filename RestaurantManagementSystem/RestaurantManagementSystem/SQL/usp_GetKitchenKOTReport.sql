--EXEC [dbo].[usp_GetKitchenKOTReport] ''
create or alter PROCEDURE [dbo].[usp_GetKitchenKOTReport]
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @Station NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Start DATETIME = COALESCE(CAST(@FromDate AS DATETIME), DATEADD(day, -1, CAST(GETDATE() AS DATE)));
    DECLARE @End DATETIME = DATEADD(day, 1, COALESCE(CAST(@ToDate AS DATETIME), CAST(GETDATE() AS DATE)));

    SELECT Distinct
        o.Id AS OrderId,
        o.OrderNumber,
        ISNULL(t.TableName, CONCAT('Table ', o.TableTurnoverId)) AS TableName,
        i.Name AS ItemName,
        oi.Quantity,
        ISNULL(s.Name, '') AS Station,
        CASE WHEN kti.status = 3 THEN 'Completed' ELSE 'Pending' END AS Status,
        kti.StartTime
    FROM OrderItems oi
    INNER JOIN dbo.Orders o ON oi.OrderId = o.Id
    LEFT JOIN dbo.MenuItems i ON oi.MenuItemId = i.Id
    LEFT JOIN [dbo].[KitchenStations] s ON i.KitchenStationId = s.Id
    LEFT JOIN Tables t ON o.TableTurnoverId = t.Id
    Left Join [dbo].[KitchenTicketItems] kti on kti.OrderItemId=oi.id
    WHERE kti.StartTime >= @Start AND kti.CompletionTime < @End
    AND (@Station IS NULL OR @Station = '' OR s.Name = @Station)
    ORDER BY kti.StartTime DESC;
END
GO
