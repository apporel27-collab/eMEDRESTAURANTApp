USE [dev_Restaurant]
GO

-- Drop procedure if it exists
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetOrderPaymentInfo')
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
        p.OrderId = @OrderId;
    
    -- Get available payment methods without using DisplayOrder column
    SELECT
        pm.Id,
        pm.Name,
        pm.DisplayName,
        pm.RequiresCardInfo,
        ISNULL(pm.RequiresCardPresent, 0) as RequiresCardPresent,
        ISNULL(pm.RequiresApproval, 0) as RequiresApproval
    FROM
        PaymentMethods pm
    WHERE
        pm.IsActive = 1
    ORDER BY
        pm.DisplayName;
END
GO