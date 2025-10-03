-- SQL Script for UC-004: Process Payments

-- Create tables if they don't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentMethods]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PaymentMethods](
        [Id] [int] IDENTITY(1,1) PRIMARY KEY,
        [Name] [nvarchar](50) NOT NULL,
        [DisplayName] [nvarchar](100) NOT NULL,
        [IsActive] [bit] NOT NULL DEFAULT(1),
        [RequiresCardInfo] [bit] NOT NULL DEFAULT(0),
        [RequiresCardPresent] [bit] NOT NULL DEFAULT(0),
        [RequiresApproval] [bit] NOT NULL DEFAULT(0),
        [CreatedAt] [datetime] NOT NULL DEFAULT(GETDATE()),
        [UpdatedAt] [datetime] NOT NULL DEFAULT(GETDATE())
    )
    
    -- Insert default payment methods
    INSERT INTO [dbo].[PaymentMethods] ([Name], [DisplayName], [RequiresCardInfo], [RequiresCardPresent], [RequiresApproval])
    VALUES
        ('CASH', 'Cash', 0, 0, 0),
        ('CREDIT_CARD', 'Credit Card', 1, 1, 1),
        ('DEBIT_CARD', 'Debit Card', 1, 1, 1),
        ('GIFT_CARD', 'Gift Card', 1, 1, 1),
        ('HOUSE_ACCOUNT', 'House Account', 0, 0, 1),
        ('COMP', 'Complimentary', 0, 0, 1)
END

-- Create Payments table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Payments](
        [Id] [int] IDENTITY(1,1) PRIMARY KEY,
        [OrderId] [int] NOT NULL,
        [PaymentMethodId] [int] NOT NULL,
        [Amount] [decimal](18,2) NOT NULL,
        [TipAmount] [decimal](18,2) NOT NULL DEFAULT(0),
        [Status] [int] NOT NULL DEFAULT(0), -- 0=Pending, 1=Approved, 2=Rejected, 3=Voided
        [ReferenceNumber] [nvarchar](100) NULL,
        [LastFourDigits] [nvarchar](4) NULL,
        [CardType] [nvarchar](50) NULL,
        [AuthorizationCode] [nvarchar](50) NULL,
        [Notes] [nvarchar](500) NULL,
        [ProcessedBy] [int] NULL,
        [ProcessedByName] [nvarchar](100) NULL,
        [CreatedAt] [datetime] NOT NULL DEFAULT(GETDATE()),
        [UpdatedAt] [datetime] NOT NULL DEFAULT(GETDATE()),
        
        CONSTRAINT [FK_Payments_Orders] FOREIGN KEY([OrderId]) REFERENCES [dbo].[Orders] ([Id]),
        CONSTRAINT [FK_Payments_PaymentMethods] FOREIGN KEY([PaymentMethodId]) REFERENCES [dbo].[PaymentMethods] ([Id])
    )
END

-- Create SplitBills table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SplitBills]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SplitBills](
        [Id] [int] IDENTITY(1,1) PRIMARY KEY,
        [OrderId] [int] NOT NULL,
        [Amount] [decimal](18,2) NOT NULL,
        [TaxAmount] [decimal](18,2) NOT NULL DEFAULT(0),
        [Status] [int] NOT NULL DEFAULT(0), -- 0=Open, 1=Paid, 2=Voided
        [Notes] [nvarchar](500) NULL,
        [CreatedBy] [int] NULL,
        [CreatedByName] [nvarchar](100) NULL,
        [CreatedAt] [datetime] NOT NULL DEFAULT(GETDATE()),
        [UpdatedAt] [datetime] NOT NULL DEFAULT(GETDATE()),
        
        CONSTRAINT [FK_SplitBills_Orders] FOREIGN KEY([OrderId]) REFERENCES [dbo].[Orders] ([Id])
    )
END

-- Create SplitBillItems table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SplitBillItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SplitBillItems](
        [Id] [int] IDENTITY(1,1) PRIMARY KEY,
        [SplitBillId] [int] NOT NULL,
        [OrderItemId] [int] NOT NULL,
        [Quantity] [int] NOT NULL DEFAULT(1),
        [Amount] [decimal](18,2) NOT NULL,
        
        CONSTRAINT [FK_SplitBillItems_SplitBills] FOREIGN KEY([SplitBillId]) REFERENCES [dbo].[SplitBills] ([Id]),
        CONSTRAINT [FK_SplitBillItems_OrderItems] FOREIGN KEY([OrderItemId]) REFERENCES [dbo].[OrderItems] ([Id])
    )
END

-- Create a stored procedure to get order payment information
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetOrderPaymentInfo]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_GetOrderPaymentInfo]
GO

CREATE PROCEDURE [dbo].[usp_GetOrderPaymentInfo]
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get order details
    SELECT 
        o.Id,
        o.OrderNumber,
        o.Subtotal,
        o.TaxAmount,
        o.TipAmount,
        o.DiscountAmount,
        o.TotalAmount,
        ISNULL(SUM(p.Amount + p.TipAmount), 0) AS PaidAmount,
        (o.TotalAmount - ISNULL(SUM(p.Amount + p.TipAmount), 0)) AS RemainingAmount,
        ISNULL(t.TableName, 'N/A') AS TableName,
        o.Status
    FROM 
        Orders o
    LEFT JOIN 
        Payments p ON o.Id = p.OrderId AND p.Status = 1 -- Approved payments only
    LEFT JOIN 
        TableTurnovers tt ON o.TableTurnoverId = tt.Id
    LEFT JOIN
        Tables t ON tt.TableId = t.Id
    WHERE 
        o.Id = @OrderId
    GROUP BY
        o.Id,
        o.OrderNumber,
        o.Subtotal,
        o.TaxAmount,
        o.TipAmount,
        o.DiscountAmount,
        o.TotalAmount,
        t.TableName,
        o.Status;
    
    -- Get order items
    SELECT
        oi.Id,
        oi.MenuItemId,
        mi.Name,
        oi.Quantity,
        oi.UnitPrice,
        oi.Subtotal
    FROM
        OrderItems oi
    INNER JOIN
        MenuItems mi ON oi.MenuItemId = mi.Id
    WHERE
        oi.OrderId = @OrderId
        AND oi.Status != 5; -- Not cancelled
    
    -- Get existing payments
    SELECT
        p.Id,
        p.PaymentMethodId,
        pm.Name AS PaymentMethod,
        pm.DisplayName AS PaymentMethodDisplay,
        p.Amount,
        p.TipAmount,
        p.Status,
        p.ReferenceNumber,
        p.LastFourDigits,
        p.CardType,
        p.AuthorizationCode,
        p.Notes,
        p.ProcessedByName,
        p.CreatedAt
    FROM
        Payments p
    INNER JOIN
        PaymentMethods pm ON p.PaymentMethodId = pm.Id
    WHERE
        p.OrderId = @OrderId
    ORDER BY
        p.CreatedAt DESC;
    
    -- Get available payment methods
    SELECT
        Id,
        Name,
        DisplayName,
        RequiresCardInfo,
        RequiresCardPresent,
        RequiresApproval
    FROM
        PaymentMethods
    WHERE
        IsActive = 1
    ORDER BY
        DisplayName;
END
GO

-- Create stored procedure to process a payment
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_ProcessPayment]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_ProcessPayment]
GO

CREATE PROCEDURE [dbo].[usp_ProcessPayment]
    @OrderId INT,
    @PaymentMethodId INT,
    @Amount DECIMAL(18,2),
    @TipAmount DECIMAL(18,2),
    @ReferenceNumber NVARCHAR(100) = NULL,
    @LastFourDigits NVARCHAR(4) = NULL,
    @CardType NVARCHAR(50) = NULL,
    @AuthorizationCode NVARCHAR(50) = NULL,
    @Notes NVARCHAR(500) = NULL,
    @ProcessedBy INT = NULL,
    @ProcessedByName NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @OrderStatus INT;
    DECLARE @OrderTotal DECIMAL(18,2);
    DECLARE @CurrentlyPaid DECIMAL(18,2);
    DECLARE @PaymentStatus INT = 1; -- Default to approved
    DECLARE @PaymentId INT;
    DECLARE @ErrorMessage NVARCHAR(200);
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate the order
        SELECT @OrderStatus = Status, @OrderTotal = TotalAmount
        FROM Orders
        WHERE Id = @OrderId;
        
        IF @OrderStatus IS NULL
        BEGIN
            SET @ErrorMessage = 'Order not found.';
            RAISERROR(@ErrorMessage, 16, 1);
            RETURN;
        END
        
        IF @OrderStatus = 4 -- Cancelled
        BEGIN
            SET @ErrorMessage = 'Cannot process payment for a cancelled order.';
            RAISERROR(@ErrorMessage, 16, 1);
            RETURN;
        END
        
        IF @OrderStatus = 3 -- Completed
        BEGIN
            SET @ErrorMessage = 'Order is already completed. Additional payments require manager approval.';
            SET @PaymentStatus = 0; -- Set to pending
        END
        
        -- Calculate currently paid amount
        SELECT @CurrentlyPaid = ISNULL(SUM(Amount + TipAmount), 0)
        FROM Payments
        WHERE OrderId = @OrderId AND Status = 1; -- Approved payments only
        
        -- Validate payment amount
        IF (@CurrentlyPaid + @Amount + @TipAmount) > (@OrderTotal * 1.1) -- Allow up to 10% overpayment
        BEGIN
            SET @ErrorMessage = 'Payment amount exceeds order total by more than 10%.';
            THROW 51000, @ErrorMessage, 1;
        END
        
        -- Create payment record
        INSERT INTO Payments (
            OrderId,
            PaymentMethodId,
            Amount,
            TipAmount,
            Status,
            ReferenceNumber,
            LastFourDigits,
            CardType,
            AuthorizationCode,
            Notes,
            ProcessedBy,
            ProcessedByName
        )
        VALUES (
            @OrderId,
            @PaymentMethodId,
            @Amount,
            @TipAmount,
            @PaymentStatus,
            @ReferenceNumber,
            @LastFourDigits,
            @CardType,
            @AuthorizationCode,
            @Notes,
            @ProcessedBy,
            @ProcessedByName
        );
        
        SET @PaymentId = SCOPE_IDENTITY();
        
        -- Update order if fully paid
        IF (@CurrentlyPaid + @Amount + @TipAmount) >= @OrderTotal AND @OrderStatus < 3
        BEGIN
            -- Update order status to completed
            UPDATE Orders
            SET Status = 3, -- Completed
                CompletedAt = GETDATE(),
                UpdatedAt = GETDATE(),
                TipAmount = TipAmount + @TipAmount -- Add this payment's tip to order total tip
            WHERE Id = @OrderId;
        END
        ELSE IF @TipAmount > 0
        BEGIN
            -- Update order with additional tip amount
            UPDATE Orders
            SET TipAmount = TipAmount + @TipAmount,
                UpdatedAt = GETDATE()
            WHERE Id = @OrderId;
        END
        
        COMMIT TRANSACTION;
        
        -- Return payment ID and status
        SELECT @PaymentId AS PaymentId, @PaymentStatus AS Status, 'Payment processed successfully.' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Return error
        SELECT 0 AS PaymentId, -1 AS Status, ERROR_MESSAGE() AS Message;
    END CATCH;
END
GO

-- Create stored procedure to void a payment
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_VoidPayment]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_VoidPayment]
GO

CREATE PROCEDURE [dbo].[usp_VoidPayment]
    @PaymentId INT,
    @Reason NVARCHAR(500),
    @ProcessedBy INT = NULL,
    @ProcessedByName NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @OrderId INT;
    DECLARE @PaymentAmount DECIMAL(18,2);
    DECLARE @TipAmount DECIMAL(18,2);
    DECLARE @CurrentStatus INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Get payment information
        SELECT 
            @OrderId = OrderId,
            @PaymentAmount = Amount,
            @TipAmount = TipAmount,
            @CurrentStatus = Status
        FROM 
            Payments
        WHERE 
            Id = @PaymentId;
        
        IF @OrderId IS NULL
        BEGIN
            RAISERROR('Payment not found.', 16, 1);
            RETURN;
        END
        
        IF @CurrentStatus = 3 -- Already voided
        BEGIN
            RAISERROR('This payment has already been voided.', 16, 1);
            RETURN;
        END
        
        -- Update payment status to voided
        UPDATE Payments
        SET Status = 3, -- Voided
            Notes = ISNULL(Notes + ' | ', '') + 'VOIDED: ' + @Reason,
            UpdatedAt = GETDATE()
        WHERE Id = @PaymentId;
        
        -- Update order to reduce tip amount
        IF @TipAmount > 0
        BEGIN
            UPDATE Orders
            SET TipAmount = TipAmount - @TipAmount,
                UpdatedAt = GETDATE()
            WHERE Id = @OrderId;
        END
        
        -- Reopen order if needed
        UPDATE Orders
        SET Status = 1, -- In Progress
            CompletedAt = NULL,
            UpdatedAt = GETDATE()
        WHERE Id = @OrderId
          AND Status = 3 -- Completed
          AND (SELECT SUM(Amount + TipAmount) FROM Payments WHERE OrderId = @OrderId AND Status = 1) < TotalAmount;
        
        COMMIT TRANSACTION;
        
        -- Return success
        SELECT 1 AS Result, 'Payment voided successfully.' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Return error
        SELECT 0 AS Result, ERROR_MESSAGE() AS Message;
    END CATCH;
END
GO

-- Create stored procedure to create a split bill
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_CreateSplitBill]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_CreateSplitBill]
GO

CREATE PROCEDURE [dbo].[usp_CreateSplitBill]
    @OrderId INT,
    @Items NVARCHAR(MAX), -- Format: 'OrderItemId,Quantity,Amount;OrderItemId,Quantity,Amount'
    @Notes NVARCHAR(500) = NULL,
    @CreatedBy INT = NULL,
    @CreatedByName NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SplitBillId INT;
    DECLARE @TotalAmount DECIMAL(18,2) = 0;
    DECLARE @TaxRate DECIMAL(5,4);
    DECLARE @TaxAmount DECIMAL(18,2) = 0;
    DECLARE @OrderStatus INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Get order status
        SELECT @OrderStatus = Status 
        FROM Orders 
        WHERE Id = @OrderId;
        
        IF @OrderStatus IS NULL
        BEGIN
            RAISERROR('Order not found.', 16, 1);
            RETURN;
        END
        
        IF @OrderStatus = 4 -- Cancelled
        BEGIN
            RAISERROR('Cannot create split bill for a cancelled order.', 16, 1);
            RETURN;
        END
        
        -- Get tax rate (from order tax divided by subtotal)
        SELECT 
            @TaxRate = CASE WHEN Subtotal > 0 THEN TaxAmount / Subtotal ELSE 0 END
        FROM 
            Orders
        WHERE 
            Id = @OrderId;
        
        -- Create split bill
        INSERT INTO SplitBills (
            OrderId,
            Amount,
            TaxAmount,
            Notes,
            CreatedBy,
            CreatedByName
        )
        VALUES (
            @OrderId,
            0, -- Will update after adding items
            0, -- Will update after adding items
            @Notes,
            @CreatedBy,
            @CreatedByName
        );
        
        SET @SplitBillId = SCOPE_IDENTITY();
        
        -- Parse items and add them to the split bill
        DECLARE @Index INT = 1;
        DECLARE @Pos INT = 0;
        DECLARE @NextPos INT = 0;
        DECLARE @ItemData NVARCHAR(100);
        
        WHILE CHARINDEX(';', @Items, @Index) > 0
        BEGIN
            SET @NextPos = CHARINDEX(';', @Items, @Index);
            SET @ItemData = SUBSTRING(@Items, @Index, @NextPos - @Index);
            
            DECLARE @OrderItemId INT;
            DECLARE @Quantity INT;
            DECLARE @Amount DECIMAL(18,2);
            
            -- Parse OrderItemId
            SET @Pos = CHARINDEX(',', @ItemData, 1);
            SET @OrderItemId = CAST(SUBSTRING(@ItemData, 1, @Pos - 1) AS INT);
            
            -- Parse Quantity
            SET @Index = @Pos + 1;
            SET @Pos = CHARINDEX(',', @ItemData, @Index);
            SET @Quantity = CAST(SUBSTRING(@ItemData, @Index, @Pos - @Index) AS INT);
            
            -- Parse Amount
            SET @Index = @Pos + 1;
            SET @Amount = CAST(SUBSTRING(@ItemData, @Index, LEN(@ItemData) - @Index + 1) AS DECIMAL(18,2));
            
            -- Add item to split bill
            INSERT INTO SplitBillItems (
                SplitBillId,
                OrderItemId,
                Quantity,
                Amount
            )
            VALUES (
                @SplitBillId,
                @OrderItemId,
                @Quantity,
                @Amount
            );
            
            SET @TotalAmount = @TotalAmount + @Amount;
            SET @Index = @NextPos + 1;
        END
        
        -- Handle the last item
        IF @Index <= LEN(@Items)
        BEGIN
            SET @ItemData = SUBSTRING(@Items, @Index, LEN(@Items) - @Index + 1);
            
            DECLARE @LastOrderItemId INT;
            DECLARE @LastQuantity INT;
            DECLARE @LastAmount DECIMAL(18,2);
            
            -- Parse OrderItemId
            SET @Pos = CHARINDEX(',', @ItemData, 1);
            SET @LastOrderItemId = CAST(SUBSTRING(@ItemData, 1, @Pos - 1) AS INT);
            
            -- Parse Quantity
            SET @Index = @Pos + 1;
            SET @Pos = CHARINDEX(',', @ItemData, @Index);
            SET @LastQuantity = CAST(SUBSTRING(@ItemData, @Index, @Pos - @Index) AS INT);
            
            -- Parse Amount
            SET @Index = @Pos + 1;
            SET @LastAmount = CAST(SUBSTRING(@ItemData, @Index, LEN(@ItemData) - @Index + 1) AS DECIMAL(18,2));
            
            -- Add item to split bill
            INSERT INTO SplitBillItems (
                SplitBillId,
                OrderItemId,
                LastQuantity,
                Amount
            )
            VALUES (
                @SplitBillId,
                @LastOrderItemId,
                @LastQuantity,
                @LastAmount
            );
            
            SET @TotalAmount = @TotalAmount + @LastAmount;
        END
        
        -- Calculate tax amount
        SET @TaxAmount = @TotalAmount * @TaxRate;
        
        -- Update split bill with total amount and tax
        UPDATE SplitBills
        SET Amount = @TotalAmount,
            TaxAmount = @TaxAmount
        WHERE Id = @SplitBillId;
        
        COMMIT TRANSACTION;
        
        -- Return split bill ID and amounts
        SELECT 
            @SplitBillId AS SplitBillId, 
            @TotalAmount AS Amount, 
            @TaxAmount AS TaxAmount, 
            (@TotalAmount + @TaxAmount) AS TotalAmount,
            'Split bill created successfully.' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Return error
        SELECT 0 AS SplitBillId, 0 AS Amount, 0 AS TaxAmount, 0 AS TotalAmount, ERROR_MESSAGE() AS Message;
    END CATCH;
END
GO
