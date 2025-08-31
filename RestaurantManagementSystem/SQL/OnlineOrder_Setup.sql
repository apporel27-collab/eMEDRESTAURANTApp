-- UC-006: Manage Online Order (Ordering Hub) - Database Setup
-- Created: August 31, 2025

-- Order Sources table (web, mobile app, delivery aggregators)
CREATE TABLE OrderSources (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    ApiKey NVARCHAR(100) NULL,
    ApiSecret NVARCHAR(100) NULL,
    WebhookUrl NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Order Source Configuration
CREATE TABLE OrderSourceConfigurations (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderSourceId INT NOT NULL FOREIGN KEY REFERENCES OrderSources(Id),
    ConfigKey NVARCHAR(50) NOT NULL,
    ConfigValue NVARCHAR(MAX) NULL,
    IsEncrypted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Online Order Statuses
CREATE TABLE OnlineOrderStatuses (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    Color NVARCHAR(20) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

-- Online Orders table
CREATE TABLE OnlineOrders (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderSourceId INT NOT NULL FOREIGN KEY REFERENCES OrderSources(Id),
    ExternalOrderId NVARCHAR(50) NOT NULL,
    OrderNumber NVARCHAR(20) NOT NULL,
    CustomerId INT NULL FOREIGN KEY REFERENCES Customers(Id),
    OrderStatusId INT NOT NULL FOREIGN KEY REFERENCES OnlineOrderStatuses(Id),
    OrderTotal DECIMAL(10,2) NOT NULL,
    TaxAmount DECIMAL(10,2) NOT NULL,
    DeliveryFee DECIMAL(10,2) NOT NULL DEFAULT 0,
    Tip DECIMAL(10,2) NOT NULL DEFAULT 0,
    ServiceFee DECIMAL(10,2) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    CouponCode NVARCHAR(50) NULL,
    PaymentMethod NVARCHAR(50) NULL,
    PaymentStatus NVARCHAR(20) NULL,
    IsDelivery BIT NOT NULL DEFAULT 0,
    IsPreOrder BIT NOT NULL DEFAULT 0,
    RequestedDeliveryTime DATETIME NULL,
    ActualDeliveryTime DATETIME NULL,
    DeliveryAddress NVARCHAR(255) NULL,
    DeliveryNotes NVARCHAR(500) NULL,
    DeliveryDriverName NVARCHAR(100) NULL,
    DeliveryDriverPhone NVARCHAR(20) NULL,
    CustomerName NVARCHAR(100) NOT NULL,
    CustomerPhone NVARCHAR(20) NULL,
    CustomerEmail NVARCHAR(100) NULL,
    SpecialInstructions NVARCHAR(500) NULL,
    SourceData NVARCHAR(MAX) NULL, -- JSON of original order data
    SyncStatus INT NOT NULL DEFAULT 0, -- 0=New, 1=Synced, 2=Error
    ErrorDetails NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    SyncedToLocalOrderId INT NULL FOREIGN KEY REFERENCES Orders(Id)
);

-- Online Order Items
CREATE TABLE OnlineOrderItems (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OnlineOrderId INT NOT NULL FOREIGN KEY REFERENCES OnlineOrders(Id),
    ExternalProductId NVARCHAR(50) NULL,
    ProductName NVARCHAR(100) NOT NULL,
    MenuItemId INT NULL FOREIGN KEY REFERENCES MenuItems(Id),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    SpecialInstructions NVARCHAR(500) NULL,
    SourceData NVARCHAR(MAX) NULL, -- JSON of original item data
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Online Order Item Modifiers
CREATE TABLE OnlineOrderItemModifiers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OnlineOrderItemId INT NOT NULL FOREIGN KEY REFERENCES OnlineOrderItems(Id),
    ExternalModifierId NVARCHAR(50) NULL,
    ModifierName NVARCHAR(100) NOT NULL,
    MenuItemModifierId INT NULL FOREIGN KEY REFERENCES MenuItemModifiers(Id),
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Webhook Events table
CREATE TABLE WebhookEvents (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderSourceId INT NOT NULL FOREIGN KEY REFERENCES OrderSources(Id),
    EventType NVARCHAR(50) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    ProcessStatus INT NOT NULL DEFAULT 0, -- 0=New, 1=Processed, 2=Error
    ErrorDetails NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ProcessedAt DATETIME NULL
);

-- Menu Item Mapping for External Services
CREATE TABLE ExternalMenuItemMappings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    MenuItemId INT NOT NULL FOREIGN KEY REFERENCES MenuItems(Id),
    OrderSourceId INT NOT NULL FOREIGN KEY REFERENCES OrderSources(Id),
    ExternalItemId NVARCHAR(50) NOT NULL,
    ExternalItemName NVARCHAR(100) NULL,
    ExternalPrice DECIMAL(10,2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    LastSyncedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_ExternalMenuItemMapping UNIQUE(MenuItemId, OrderSourceId, ExternalItemId)
);

-- Modifier Mapping for External Services
CREATE TABLE ExternalModifierMappings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    MenuItemModifierId INT NOT NULL FOREIGN KEY REFERENCES MenuItemModifiers(Id),
    OrderSourceId INT NOT NULL FOREIGN KEY REFERENCES OrderSources(Id),
    ExternalModifierId NVARCHAR(50) NOT NULL,
    ExternalModifierName NVARCHAR(100) NULL,
    ExternalPrice DECIMAL(10,2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    LastSyncedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_ExternalModifierMapping UNIQUE(MenuItemModifierId, OrderSourceId, ExternalModifierId)
);

-- API Call Logs
CREATE TABLE ApiCallLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderSourceId INT NOT NULL FOREIGN KEY REFERENCES OrderSources(Id),
    EndpointUrl NVARCHAR(255) NOT NULL,
    RequestMethod NVARCHAR(10) NOT NULL,
    RequestHeaders NVARCHAR(MAX) NULL,
    RequestBody NVARCHAR(MAX) NULL,
    ResponseCode INT NULL,
    ResponseBody NVARCHAR(MAX) NULL,
    ExecutionTimeMs INT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Insert initial data for order statuses
INSERT INTO OnlineOrderStatuses (Name, Description, Color, IsActive)
VALUES
('New', 'Order has been received but not yet acknowledged', '#FF9900', 1),
('Acknowledged', 'Order has been acknowledged by the restaurant', '#3399FF', 1),
('In Preparation', 'Order is being prepared in the kitchen', '#9966CC', 1),
('Ready for Pickup', 'Order is ready for pickup by the customer or delivery driver', '#33CC33', 1),
('Out for Delivery', 'Order has been picked up and is on the way to the customer', '#FF6600', 1),
('Delivered', 'Order has been delivered to the customer', '#009900', 1),
('Completed', 'Order has been fulfilled and closed', '#006633', 1),
('Cancelled', 'Order has been cancelled', '#CC0000', 1),
('Refunded', 'Order has been refunded', '#990000', 1);

-- Insert common order sources
INSERT INTO OrderSources (Name, Description, IsActive)
VALUES
('Restaurant Website', 'Orders placed directly on our website', 1),
('Restaurant Mobile App', 'Orders placed through our mobile application', 1),
('UberEats', 'Orders from UberEats platform', 1),
('DoorDash', 'Orders from DoorDash platform', 1),
('GrubHub', 'Orders from GrubHub platform', 1),
('Postmates', 'Orders from Postmates platform', 1);

-- Stored Procedure to get all Online Orders with filtering
CREATE OR ALTER PROCEDURE GetOnlineOrders
    @StatusId INT = NULL,
    @OrderSourceId INT = NULL,
    @SyncStatus INT = NULL,
    @StartDate DATETIME = NULL,
    @EndDate DATETIME = NULL,
    @SearchTerm NVARCHAR(50) = NULL
AS
BEGIN
    SELECT
        oo.Id,
        oo.OrderNumber,
        oo.ExternalOrderId,
        os.Name AS OrderSourceName,
        oos.Name AS StatusName,
        oos.Color AS StatusColor,
        oo.CustomerName,
        oo.CustomerPhone,
        oo.OrderTotal,
        oo.IsDelivery,
        oo.CreatedAt AS OrderDate,
        oo.RequestedDeliveryTime,
        oo.SyncStatus,
        oo.SyncedToLocalOrderId
    FROM OnlineOrders oo
    INNER JOIN OrderSources os ON oo.OrderSourceId = os.Id
    INNER JOIN OnlineOrderStatuses oos ON oo.OrderStatusId = oos.Id
    WHERE
        (@StatusId IS NULL OR oo.OrderStatusId = @StatusId)
        AND (@OrderSourceId IS NULL OR oo.OrderSourceId = @OrderSourceId)
        AND (@SyncStatus IS NULL OR oo.SyncStatus = @SyncStatus)
        AND (@StartDate IS NULL OR oo.CreatedAt >= @StartDate)
        AND (@EndDate IS NULL OR oo.CreatedAt <= @EndDate)
        AND (@SearchTerm IS NULL 
            OR oo.OrderNumber LIKE '%' + @SearchTerm + '%' 
            OR oo.ExternalOrderId LIKE '%' + @SearchTerm + '%'
            OR oo.CustomerName LIKE '%' + @SearchTerm + '%'
            OR oo.CustomerPhone LIKE '%' + @SearchTerm + '%'
            OR oo.CustomerEmail LIKE '%' + @SearchTerm + '%')
    ORDER BY oo.CreatedAt DESC;
END;

-- Stored Procedure to get Online Order details
CREATE OR ALTER PROCEDURE GetOnlineOrderDetails
    @OrderId INT
AS
BEGIN
    -- Get Order Header
    SELECT
        oo.Id,
        oo.OrderNumber,
        oo.ExternalOrderId,
        os.Id AS OrderSourceId,
        os.Name AS OrderSourceName,
        oos.Id AS StatusId,
        oos.Name AS StatusName,
        oos.Color AS StatusColor,
        oo.CustomerName,
        oo.CustomerPhone,
        oo.CustomerEmail,
        oo.OrderTotal,
        oo.TaxAmount,
        oo.DeliveryFee,
        oo.Tip,
        oo.ServiceFee,
        oo.DiscountAmount,
        oo.CouponCode,
        oo.PaymentMethod,
        oo.PaymentStatus,
        oo.IsDelivery,
        oo.IsPreOrder,
        oo.RequestedDeliveryTime,
        oo.ActualDeliveryTime,
        oo.DeliveryAddress,
        oo.DeliveryNotes,
        oo.DeliveryDriverName,
        oo.DeliveryDriverPhone,
        oo.SpecialInstructions,
        oo.SyncStatus,
        oo.ErrorDetails,
        oo.CreatedAt AS OrderDate,
        oo.UpdatedAt,
        oo.SyncedToLocalOrderId
    FROM OnlineOrders oo
    INNER JOIN OrderSources os ON oo.OrderSourceId = os.Id
    INNER JOIN OnlineOrderStatuses oos ON oo.OrderStatusId = oos.Id
    WHERE oo.Id = @OrderId;
    
    -- Get Order Items
    SELECT
        ooi.Id,
        ooi.ProductName,
        ooi.ExternalProductId,
        ooi.MenuItemId,
        ooi.Quantity,
        ooi.UnitPrice,
        ooi.TotalPrice,
        ooi.SpecialInstructions,
        m.Name AS MenuItemName
    FROM OnlineOrderItems ooi
    LEFT JOIN MenuItems m ON ooi.MenuItemId = m.Id
    WHERE ooi.OnlineOrderId = @OrderId;
    
    -- Get Item Modifiers
    SELECT
        ooim.Id,
        ooim.OnlineOrderItemId,
        ooim.ModifierName,
        ooim.ExternalModifierId,
        ooim.MenuItemModifierId,
        ooim.Quantity,
        ooim.UnitPrice,
        ooim.TotalPrice,
        mm.Name AS MenuModifierName
    FROM OnlineOrderItemModifiers ooim
    INNER JOIN OnlineOrderItems ooi ON ooim.OnlineOrderItemId = ooi.Id
    LEFT JOIN MenuItemModifiers mm ON ooim.MenuItemModifierId = mm.Id
    WHERE ooi.OnlineOrderId = @OrderId;
END;

-- Stored Procedure to update Online Order status
CREATE OR ALTER PROCEDURE UpdateOnlineOrderStatus
    @OrderId INT,
    @StatusId INT,
    @Notes NVARCHAR(500) = NULL
AS
BEGIN
    BEGIN TRANSACTION;
    
    BEGIN TRY
        UPDATE OnlineOrders
        SET 
            OrderStatusId = @StatusId,
            UpdatedAt = GETDATE(),
            SpecialInstructions = CASE 
                WHEN @Notes IS NOT NULL THEN 
                    ISNULL(SpecialInstructions + CHAR(13) + CHAR(10) + '---' + CHAR(13) + CHAR(10), '') + 
                    FORMAT(GETDATE(), 'yyyy-MM-dd HH:mm') + ': ' + @Notes
                ELSE SpecialInstructions
            END
        WHERE Id = @OrderId;
        
        -- For delivery orders, update the actual delivery time when status is set to Delivered
        IF @StatusId = 6 -- Delivered
        BEGIN
            UPDATE OnlineOrders
            SET ActualDeliveryTime = GETDATE()
            WHERE Id = @OrderId AND IsDelivery = 1 AND ActualDeliveryTime IS NULL;
        END
        
        COMMIT TRANSACTION;
        
        -- Return updated order
        EXEC GetOnlineOrderDetails @OrderId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

-- Stored Procedure to sync Online Order to local order system
CREATE OR ALTER PROCEDURE SyncOnlineOrderToLocalOrder
    @OnlineOrderId INT
AS
BEGIN
    DECLARE @OrderId INT;
    DECLARE @CustomerId INT;
    DECLARE @OrderTotal DECIMAL(10,2);
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Get or create customer
        SELECT @CustomerId = CustomerId FROM OnlineOrders WHERE Id = @OnlineOrderId;
        
        IF @CustomerId IS NULL
        BEGIN
            DECLARE @CustomerName NVARCHAR(100);
            DECLARE @CustomerPhone NVARCHAR(20);
            DECLARE @CustomerEmail NVARCHAR(100);
            
            SELECT 
                @CustomerName = CustomerName,
                @CustomerPhone = CustomerPhone,
                @CustomerEmail = CustomerEmail
            FROM OnlineOrders
            WHERE Id = @OnlineOrderId;
            
            -- Check if customer already exists by phone or email
            SELECT @CustomerId = Id 
            FROM Customers 
            WHERE 
                (@CustomerPhone IS NOT NULL AND Phone = @CustomerPhone)
                OR (@CustomerEmail IS NOT NULL AND Email = @CustomerEmail);
                
            -- Create new customer if not found
            IF @CustomerId IS NULL
            BEGIN
                INSERT INTO Customers (Name, Phone, Email, CreatedAt, UpdatedAt)
                VALUES (@CustomerName, @CustomerPhone, @CustomerEmail, GETDATE(), GETDATE());
                
                SET @CustomerId = SCOPE_IDENTITY();
                
                -- Update online order with customer id
                UPDATE OnlineOrders SET CustomerId = @CustomerId WHERE Id = @OnlineOrderId;
            END
        END
        
        -- Create Order
        DECLARE @OrderSource NVARCHAR(50);
        DECLARE @DeliveryAddress NVARCHAR(255);
        DECLARE @SpecialInstructions NVARCHAR(500);
        DECLARE @IsDelivery BIT;
        
        SELECT 
            @OrderSource = os.Name,
            @DeliveryAddress = oo.DeliveryAddress,
            @SpecialInstructions = oo.SpecialInstructions,
            @OrderTotal = oo.OrderTotal,
            @IsDelivery = oo.IsDelivery
        FROM OnlineOrders oo
        INNER JOIN OrderSources os ON oo.OrderSourceId = os.Id
        WHERE oo.Id = @OnlineOrderId;
        
        -- Create new order in local system
        INSERT INTO Orders (
            CustomerId, 
            OrderType, 
            OrderStatus, 
            OrderTotal, 
            Notes, 
            DeliveryAddress,
            CreatedAt, 
            UpdatedAt
        )
        VALUES (
            @CustomerId,
            CASE WHEN @IsDelivery = 1 THEN 2 ELSE 1 END, -- 1=Takeout, 2=Delivery
            1, -- New
            @OrderTotal,
            'Order from ' + @OrderSource + CHAR(13) + CHAR(10) + ISNULL(@SpecialInstructions, ''),
            @DeliveryAddress,
            GETDATE(),
            GETDATE()
        );
        
        SET @OrderId = SCOPE_IDENTITY();
        
        -- Add order items
        INSERT INTO OrderItems (
            OrderId,
            MenuItemId,
            Quantity,
            UnitPrice,
            Notes
        )
        SELECT
            @OrderId,
            ooi.MenuItemId,
            ooi.Quantity,
            ooi.UnitPrice,
            ooi.SpecialInstructions
        FROM OnlineOrderItems ooi
        WHERE ooi.OnlineOrderId = @OnlineOrderId AND ooi.MenuItemId IS NOT NULL;
        
        -- Add item modifiers
        INSERT INTO OrderItemModifiers (
            OrderItemId,
            MenuItemModifierId,
            Quantity,
            UnitPrice
        )
        SELECT
            oi.Id,
            ooim.MenuItemModifierId,
            ooim.Quantity,
            ooim.UnitPrice
        FROM OnlineOrderItemModifiers ooim
        INNER JOIN OnlineOrderItems ooi ON ooim.OnlineOrderItemId = ooi.Id
        INNER JOIN OrderItems oi ON oi.OrderId = @OrderId AND oi.MenuItemId = ooi.MenuItemId
        WHERE ooi.OnlineOrderId = @OnlineOrderId 
            AND ooim.MenuItemModifierId IS NOT NULL;
        
        -- Update online order sync status
        UPDATE OnlineOrders
        SET 
            SyncStatus = 1, -- Synced
            SyncedToLocalOrderId = @OrderId,
            UpdatedAt = GETDATE()
        WHERE Id = @OnlineOrderId;
        
        COMMIT TRANSACTION;
        
        -- Return local order id
        SELECT @OrderId AS OrderId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        
        -- Update with error
        UPDATE OnlineOrders
        SET 
            SyncStatus = 2, -- Error
            ErrorDetails = ERROR_MESSAGE(),
            UpdatedAt = GETDATE()
        WHERE Id = @OnlineOrderId;
        
        THROW;
    END CATCH;
END;

-- Stored Procedure to get Order Sources
CREATE OR ALTER PROCEDURE GetOrderSources
AS
BEGIN
    SELECT Id, Name, Description, IsActive
    FROM OrderSources
    ORDER BY Name;
END;

-- Stored Procedure to get Order Statuses
CREATE OR ALTER PROCEDURE GetOnlineOrderStatuses
AS
BEGIN
    SELECT Id, Name, Description, Color, IsActive
    FROM OnlineOrderStatuses
    ORDER BY Id;
END;

-- Stored Procedure to manage External Menu Item Mappings
CREATE OR ALTER PROCEDURE ManageExternalMenuItemMapping
    @MenuItemId INT,
    @OrderSourceId INT,
    @ExternalItemId NVARCHAR(50),
    @ExternalItemName NVARCHAR(100) = NULL,
    @ExternalPrice DECIMAL(10,2) = NULL,
    @IsActive BIT = 1
AS
BEGIN
    BEGIN TRY
        -- Check if mapping already exists
        IF EXISTS (SELECT 1 FROM ExternalMenuItemMappings 
                   WHERE MenuItemId = @MenuItemId 
                   AND OrderSourceId = @OrderSourceId
                   AND ExternalItemId = @ExternalItemId)
        BEGIN
            -- Update existing mapping
            UPDATE ExternalMenuItemMappings
            SET
                ExternalItemName = ISNULL(@ExternalItemName, ExternalItemName),
                ExternalPrice = ISNULL(@ExternalPrice, ExternalPrice),
                IsActive = @IsActive,
                UpdatedAt = GETDATE()
            WHERE
                MenuItemId = @MenuItemId
                AND OrderSourceId = @OrderSourceId
                AND ExternalItemId = @ExternalItemId;
        END
        ELSE
        BEGIN
            -- Create new mapping
            INSERT INTO ExternalMenuItemMappings (
                MenuItemId,
                OrderSourceId,
                ExternalItemId,
                ExternalItemName,
                ExternalPrice,
                IsActive,
                CreatedAt,
                UpdatedAt
            )
            VALUES (
                @MenuItemId,
                @OrderSourceId,
                @ExternalItemId,
                @ExternalItemName,
                @ExternalPrice,
                @IsActive,
                GETDATE(),
                GETDATE()
            );
        END
        
        -- Return the updated/inserted mapping
        SELECT
            eim.Id,
            eim.MenuItemId,
            mi.Name AS MenuItemName,
            eim.OrderSourceId,
            os.Name AS OrderSourceName,
            eim.ExternalItemId,
            eim.ExternalItemName,
            eim.ExternalPrice,
            eim.IsActive,
            eim.LastSyncedAt,
            eim.CreatedAt,
            eim.UpdatedAt
        FROM ExternalMenuItemMappings eim
        INNER JOIN MenuItems mi ON eim.MenuItemId = mi.Id
        INNER JOIN OrderSources os ON eim.OrderSourceId = os.Id
        WHERE
            eim.MenuItemId = @MenuItemId
            AND eim.OrderSourceId = @OrderSourceId
            AND eim.ExternalItemId = @ExternalItemId;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
