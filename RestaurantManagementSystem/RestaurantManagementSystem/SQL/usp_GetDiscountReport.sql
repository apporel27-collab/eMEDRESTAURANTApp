-- =============================================
-- Author:      Auto-generated
-- Create date: 2025-10-04
-- Description: Returns discount usage summary and detailed discounted orders
-- Parameters: @StartDate DATE, @EndDate DATE
-- =============================================
IF OBJECT_ID('usp_GetDiscountReport', 'P') IS NOT NULL
    DROP PROCEDURE usp_GetDiscountReport
GO

CREATE PROCEDURE usp_GetDiscountReport
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @StartDate = ISNULL(@StartDate, CAST(GETDATE() AS DATE));
    SET @EndDate = ISNULL(@EndDate, CAST(GETDATE() AS DATE));

    DECLARE @StartDateTime DATETIME = CAST(@StartDate AS DATETIME);
    DECLARE @EndDateTime DATETIME = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME)); -- exclusive

    /* Summary Metrics */
    ;WITH Discounted AS (
        SELECT o.Id, o.OrderNumber, o.CreatedAt, o.DiscountAmount, o.Subtotal, o.TaxAmount, o.TipAmount, o.TotalAmount, o.Status, o.UserId
        FROM Orders o WITH (NOLOCK)
        WHERE o.CreatedAt >= @StartDateTime AND o.CreatedAt < @EndDateTime
          AND o.DiscountAmount > 0
    )
    SELECT 
        TotalDiscountedOrders = COUNT(*),
        TotalDiscountAmount = ISNULL(SUM(DiscountAmount),0),
        AvgDiscountPerOrder = CASE WHEN COUNT(*)>0 THEN AVG(DiscountAmount) ELSE 0 END,
        MaxDiscount = ISNULL(MAX(DiscountAmount),0),
        MinDiscount = ISNULL(MIN(DiscountAmount),0),
        TotalGrossBeforeDiscount = ISNULL(SUM(Subtotal + TaxAmount + TipAmount),0),
        NetAfterDiscount = ISNULL(SUM(TotalAmount),0)
    FROM Discounted;

    /* Detailed Rows */
    SELECT 
        o.Id AS OrderId,
        o.OrderNumber,
        o.CreatedAt,
        o.DiscountAmount,
        o.Subtotal,
        o.TaxAmount,
        o.TipAmount,
        o.TotalAmount,
        (o.Subtotal + o.TaxAmount + o.TipAmount) AS GrossAmount,
        (o.Subtotal + o.TaxAmount + o.TipAmount) - o.TotalAmount AS DiscountApplied,
        u.Username,
        u.FirstName,
        u.LastName,
        o.Status
    FROM Orders o WITH (NOLOCK)
    LEFT JOIN Users u WITH (NOLOCK) ON u.Id = o.UserId
    WHERE o.CreatedAt >= @StartDateTime AND o.CreatedAt < @EndDateTime
      AND o.DiscountAmount > 0
    ORDER BY o.CreatedAt DESC;
END
GO
