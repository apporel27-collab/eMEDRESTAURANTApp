-- SQL Script for UC-005: Kitchen Management

-- Create tables if they don't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KitchenStations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[KitchenStations](
        [Id] [int] IDENTITY(1,1) PRIMARY KEY,
        [Name] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](200) NULL,
        [IsActive] [bit] NOT NULL DEFAULT(1),
        [CreatedAt] [datetime] NOT NULL DEFAULT(GETDATE()),
        [UpdatedAt] [datetime] NOT NULL DEFAULT(GETDATE())
    )
    
    -- Insert default kitchen stations
    INSERT INTO [dbo].[KitchenStations] ([Name], [Description])
    VALUES
        ('Hot Line', 'Main cooking station for hot food'),
        ('Cold Line', 'Preparation station for cold items and salads'),
        ('Grill', 'Grill and broiler station'),
        ('Fryer', 'Deep fryer station'),
        ('Dessert', 'Dessert preparation station')
END

-- Assign menu items to kitchen stations if this relationship doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MenuItem_KitchenStations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MenuItem_KitchenStations](
        [Id] [int] IDENTITY(1,1) PRIMARY KEY,
        [MenuItemId] [int] NOT NULL,
        [KitchenStationId] [int] NOT NULL,
        [IsPrimary] [bit] NOT NULL DEFAULT(1),
        
        CONSTRAINT [FK_MenuItem_KitchenStations_MenuItems] FOREIGN KEY([MenuItemId]) REFERENCES [dbo].[MenuItems] ([Id]),
        CONSTRAINT [FK_MenuItem_KitchenStations_KitchenStations] FOREIGN KEY([KitchenStationId]) REFERENCES [dbo].[KitchenStations] ([Id]),
        CONSTRAINT [UQ_MenuItem_KitchenStations] UNIQUE ([MenuItemId], [KitchenStationId])
    )
END

-- Add KitchenStationId to KitchenTickets if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[KitchenTickets]') AND name = 'KitchenStationId')
BEGIN
    ALTER TABLE [dbo].[KitchenTickets]
    ADD [KitchenStationId] [int] NULL
    
    ALTER TABLE [dbo].[KitchenTickets]
    ADD CONSTRAINT [FK_KitchenTickets_KitchenStations] 
    FOREIGN KEY([KitchenStationId]) REFERENCES [dbo].[KitchenStations] ([Id])
END

-- Create stored procedure to get kitchen dashboard data
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetKitchenDashboard]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_GetKitchenDashboard]
GO

CREATE PROCEDURE [dbo].[usp_GetKitchenDashboard]
    @StationId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get kitchen stations
    SELECT 
        Id, 
        Name, 
        Description,
        IsActive
    FROM 
        KitchenStations
    WHERE
        (@StationId IS NULL OR Id = @StationId)
        AND IsActive = 1
    ORDER BY 
        Name;
    
    -- Get active tickets for the station
    SELECT 
        kt.Id,
        kt.TicketNumber,
        kt.OrderId,
        o.OrderNumber,
        ISNULL(t.TableName, 'N/A') AS TableName,
        kt.KitchenStationId,
        ks.Name AS StationName,
        kt.Status,
        CASE 
            WHEN kt.Status = 0 THEN 'New'
            WHEN kt.Status = 1 THEN 'In Progress'
            WHEN kt.Status = 2 THEN 'Ready'
            WHEN kt.Status = 3 THEN 'Delivered'
            WHEN kt.Status = 4 THEN 'Cancelled'
            ELSE 'Unknown'
        END AS StatusDisplay,
        kt.CreatedAt,
        kt.CompletedAt,
        DATEDIFF(MINUTE, kt.CreatedAt, ISNULL(kt.CompletedAt, GETDATE())) AS MinutesSinceCreated
    FROM 
        KitchenTickets kt
    INNER JOIN 
        Orders o ON kt.OrderId = o.Id
    LEFT JOIN 
        KitchenStations ks ON kt.KitchenStationId = ks.Id
    LEFT JOIN
        TableTurnovers tt ON o.TableTurnoverId = tt.Id
    LEFT JOIN
        Tables t ON tt.TableId = t.Id
    WHERE
        kt.Status < 3  -- Not delivered or cancelled
        AND (@StationId IS NULL OR kt.KitchenStationId = @StationId OR kt.KitchenStationId IS NULL)
    ORDER BY
        kt.CreatedAt;
    
    -- Get active ticket items
    SELECT 
        kti.Id,
        kti.KitchenTicketId,
        kti.OrderItemId,
        kti.MenuItemName,
        kti.Quantity,
        kti.SpecialInstructions,
        kti.Status,
        CASE 
            WHEN kti.Status = 0 THEN 'New'
            WHEN kti.Status = 1 THEN 'In Progress'
            WHEN kti.Status = 2 THEN 'Ready'
            WHEN kti.Status = 3 THEN 'Delivered'
            WHEN kti.Status = 4 THEN 'Cancelled'
            ELSE 'Unknown'
        END AS StatusDisplay,
        kti.StartTime,
        kti.CompletionTime,
        kti.Notes,
        CASE 
            WHEN kti.StartTime IS NOT NULL AND kti.CompletionTime IS NULL THEN DATEDIFF(MINUTE, kti.StartTime, GETDATE())
            ELSE 0
        END AS MinutesCooking,
        mks.KitchenStationId,
        ks.Name AS StationName,
        mi.PrepTime
    FROM 
        KitchenTicketItems kti
    INNER JOIN 
        KitchenTickets kt ON kti.KitchenTicketId = kt.Id
    INNER JOIN
        OrderItems oi ON kti.OrderItemId = oi.Id
    INNER JOIN
        MenuItems mi ON oi.MenuItemId = mi.Id
    LEFT JOIN
        MenuItem_KitchenStations mks ON mi.Id = mks.MenuItemId AND mks.IsPrimary = 1
    LEFT JOIN
        KitchenStations ks ON mks.KitchenStationId = ks.Id
    WHERE
        kti.Status < 3  -- Not delivered or cancelled
        AND kt.Status < 3  -- Not delivered or cancelled
        AND (@StationId IS NULL OR kt.KitchenStationId = @StationId OR kt.KitchenStationId IS NULL
             OR mks.KitchenStationId = @StationId)
    ORDER BY
        kt.CreatedAt,
        kti.Id;
        
    -- Get summary stats
    SELECT
        SUM(CASE WHEN kt.Status = 0 THEN 1 ELSE 0 END) AS NewTicketsCount,
        SUM(CASE WHEN kt.Status = 1 THEN 1 ELSE 0 END) AS InProgressTicketsCount,
        SUM(CASE WHEN kt.Status = 2 THEN 1 ELSE 0 END) AS ReadyTicketsCount,
        SUM(CASE WHEN kti.Status < 2 THEN 1 ELSE 0 END) AS PendingItemsCount,
        SUM(CASE WHEN kti.Status = 2 THEN 1 ELSE 0 END) AS ReadyItemsCount,
        AVG(CASE WHEN kti.StartTime IS NOT NULL AND kti.CompletionTime IS NOT NULL 
                THEN DATEDIFF(SECOND, kti.StartTime, kti.CompletionTime) 
                ELSE NULL 
            END) / 60.0 AS AvgPrepTimeMinutes
    FROM 
        KitchenTickets kt
    LEFT JOIN 
        KitchenTicketItems kti ON kt.Id = kti.KitchenTicketId
    WHERE
        kt.Status < 3  -- Not delivered or cancelled
        AND (@StationId IS NULL OR kt.KitchenStationId = @StationId OR kt.KitchenStationId IS NULL);
END
GO

-- Create stored procedure to update kitchen ticket status
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_UpdateKitchenTicketStatus]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_UpdateKitchenTicketStatus]
GO

CREATE PROCEDURE [dbo].[usp_UpdateKitchenTicketStatus]
    @TicketId INT,
    @Status INT,
    @UpdatedBy INT = NULL,
    @UpdatedByName NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @CurrentStatus INT;
        DECLARE @OrderId INT;
        
        -- Get current status and order ID
        SELECT @CurrentStatus = Status, @OrderId = OrderId
        FROM KitchenTickets
        WHERE Id = @TicketId;
        
        IF @CurrentStatus IS NULL
        BEGIN
            RAISERROR('Kitchen ticket not found.', 16, 1);
            RETURN;
        END
        
        -- Update ticket status
        UPDATE KitchenTickets
        SET Status = @Status,
            CompletedAt = CASE WHEN @Status >= 2 THEN GETDATE() ELSE CompletedAt END,
            UpdatedAt = GETDATE()
        WHERE Id = @TicketId;
        
        -- If marking as ready or delivered, update all items
        IF @Status >= 2
        BEGIN
            UPDATE KitchenTicketItems
            SET Status = @Status,
                CompletionTime = CASE WHEN Status < 2 THEN GETDATE() ELSE CompletionTime END,
                Notes = CASE 
                          WHEN Notes IS NULL THEN 'Completed with ticket'
                          ELSE Notes + ' | Completed with ticket'
                       END
            WHERE KitchenTicketId = @TicketId AND Status < @Status;
        END
        
        -- Update order items' status
        UPDATE OrderItems
        SET Status = CASE 
                        WHEN @Status = 2 THEN 2 -- Ready
                        WHEN @Status = 3 THEN 3 -- Delivered
                        ELSE Status
                     END,
            UpdatedAt = GETDATE()
        WHERE Id IN (
            SELECT OrderItemId
            FROM KitchenTicketItems
            WHERE KitchenTicketId = @TicketId
        ) AND Status < CASE 
                        WHEN @Status = 2 THEN 2 
                        WHEN @Status = 3 THEN 3
                        ELSE Status
                     END;
        
        -- Check if all tickets for the order are completed/delivered/cancelled
        DECLARE @AllTicketsComplete BIT = 0;
        
        IF NOT EXISTS (
            SELECT 1
            FROM KitchenTickets
            WHERE OrderId = @OrderId AND Status < 2 -- New or In Progress
        )
        BEGIN
            SET @AllTicketsComplete = 1;
            
            -- Update order status if all tickets are complete
            UPDATE Orders
            SET Status = CASE
                            WHEN Status < 2 THEN 2 -- Ready
                            ELSE Status
                         END,
                UpdatedAt = GETDATE()
            WHERE Id = @OrderId AND Status < 2;
        END
        
        COMMIT TRANSACTION;
        
        -- Return updated ticket and status
        SELECT 
            @TicketId AS TicketId, 
            @Status AS Status, 
            CASE 
                WHEN @Status = 0 THEN 'New'
                WHEN @Status = 1 THEN 'In Progress'
                WHEN @Status = 2 THEN 'Ready'
                WHEN @Status = 3 THEN 'Delivered'
                WHEN @Status = 4 THEN 'Cancelled'
                ELSE 'Unknown'
            END AS StatusDisplay,
            @AllTicketsComplete AS AllTicketsComplete,
            'Ticket status updated successfully.' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Return error
        SELECT 0 AS TicketId, -1 AS Status, '' AS StatusDisplay, 0 AS AllTicketsComplete, ERROR_MESSAGE() AS Message;
    END CATCH;
END
GO

-- Create stored procedure to update kitchen ticket item status
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_UpdateKitchenTicketItemStatus]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_UpdateKitchenTicketItemStatus]
GO

CREATE PROCEDURE [dbo].[usp_UpdateKitchenTicketItemStatus]
    @ItemId INT,
    @Status INT,
    @Notes NVARCHAR(200) = NULL,
    @UpdatedBy INT = NULL,
    @UpdatedByName NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @CurrentStatus INT;
        DECLARE @TicketId INT;
        DECLARE @OrderItemId INT;
        DECLARE @AllItemsComplete BIT = 0;
        
        -- Get current status and ticket ID
        SELECT 
            @CurrentStatus = Status, 
            @TicketId = KitchenTicketId,
            @OrderItemId = OrderItemId
        FROM KitchenTicketItems
        WHERE Id = @ItemId;
        
        IF @CurrentStatus IS NULL
        BEGIN
            RAISERROR('Kitchen ticket item not found.', 16, 1);
            RETURN;
        END
        
        -- Update item status
        UPDATE KitchenTicketItems
        SET Status = @Status,
            StartTime = CASE WHEN @Status >= 1 AND StartTime IS NULL THEN GETDATE() ELSE StartTime END,
            CompletionTime = CASE WHEN @Status >= 2 AND CompletionTime IS NULL THEN GETDATE() ELSE CompletionTime END,
            Notes = CASE 
                      WHEN @Notes IS NOT NULL THEN ISNULL(Notes + ' | ', '') + @Notes
                      ELSE Notes
                   END
        WHERE Id = @ItemId;
        
        -- Update order item status
        UPDATE OrderItems
        SET Status = CASE 
                      WHEN @Status = 2 THEN 2 -- Ready
                      WHEN @Status = 3 THEN 3 -- Delivered
                      ELSE Status
                   END,
            UpdatedAt = GETDATE()
        WHERE Id = @OrderItemId AND Status < @Status;
        
        -- Check if all items for the ticket are complete
        IF NOT EXISTS (
            SELECT 1
            FROM KitchenTicketItems
            WHERE KitchenTicketId = @TicketId AND Status < 2 -- New or In Progress
        )
        BEGIN
            SET @AllItemsComplete = 1;
            
            -- Update ticket status if all items are complete
            UPDATE KitchenTickets
            SET Status = 2, -- Ready
                CompletedAt = GETDATE(),
                UpdatedAt = GETDATE()
            WHERE Id = @TicketId AND Status < 2;
        END
        
        COMMIT TRANSACTION;
        
        -- Return updated item and status
        SELECT 
            @ItemId AS ItemId,
            @TicketId AS TicketId,
            @Status AS Status, 
            CASE 
                WHEN @Status = 0 THEN 'New'
                WHEN @Status = 1 THEN 'In Progress'
                WHEN @Status = 2 THEN 'Ready'
                WHEN @Status = 3 THEN 'Delivered'
                WHEN @Status = 4 THEN 'Cancelled'
                ELSE 'Unknown'
            END AS StatusDisplay,
            @AllItemsComplete AS AllItemsComplete,
            'Item status updated successfully.' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Return error
        SELECT 0 AS ItemId, 0 AS TicketId, -1 AS Status, '' AS StatusDisplay, 0 AS AllItemsComplete, ERROR_MESSAGE() AS Message;
    END CATCH;
END
GO

-- Create stored procedure to assign kitchen station to a ticket
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_AssignKitchenStation]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_AssignKitchenStation]
GO

CREATE PROCEDURE [dbo].[usp_AssignKitchenStation]
    @TicketId INT,
    @StationId INT,
    @UpdatedBy INT = NULL,
    @UpdatedByName NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Update ticket station
        UPDATE KitchenTickets
        SET KitchenStationId = @StationId,
            UpdatedAt = GETDATE()
        WHERE Id = @TicketId;
        
        -- Return success
        SELECT 
            @TicketId AS TicketId, 
            @StationId AS StationId,
            (SELECT Name FROM KitchenStations WHERE Id = @StationId) AS StationName,
            'Ticket assigned to station successfully.' AS Message;
    END TRY
    BEGIN CATCH
        -- Return error
        SELECT 0 AS TicketId, 0 AS StationId, '' AS StationName, ERROR_MESSAGE() AS Message;
    END CATCH;
END
GO
