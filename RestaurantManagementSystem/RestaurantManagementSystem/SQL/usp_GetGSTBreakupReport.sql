IF OBJECT_ID('dbo.usp_GetGSTBreakupReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetGSTBreakupReport;
GO
CREATE PROCEDURE dbo.usp_GetGSTBreakupReport
    @StartDate DATE = NULL,
    @EndDate   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Normalize dates: if only one provided treat both as same day
    IF @StartDate IS NULL AND @EndDate IS NULL
    BEGIN
        SET @StartDate = CAST(GETDATE() AS DATE);
        SET @EndDate = @StartDate;
    END
    ELSE IF @StartDate IS NULL SET @StartDate = @EndDate;
    ELSE IF @EndDate IS NULL SET @EndDate = @StartDate;

    IF OBJECT_ID('tempdb..#PaymentCTE') IS NOT NULL DROP TABLE #PaymentCTE;

    SELECT 
        p.Id,
        p.CreatedAt,
        p.OrderId,
        o.OrderNumber,
        (p.Amount_ExclGST - ISNULL(p.DiscAmount,0)) AS TaxableValue,
        ISNULL(p.DiscAmount,0) AS DiscountAmount,
        ISNULL(p.CGST_Perc,0) AS CGSTPerc,
        ISNULL(p.CGSTAmount,0) AS CGSTAmount,
        ISNULL(p.SGST_Perc,0) AS SGSTPerc,
        ISNULL(p.SGSTAmount,0) AS SGSTAmount,
        ISNULL(p.CGSTAmount,0) + ISNULL(p.SGSTAmount,0) AS TotalGST,
        (p.Amount_ExclGST - ISNULL(p.DiscAmount,0)) + (ISNULL(p.CGSTAmount,0) + ISNULL(p.SGSTAmount,0)) AS InvoiceTotal
    INTO #PaymentCTE
    FROM Payments p
    INNER JOIN Orders o ON p.OrderId = o.Id
    WHERE CAST(p.CreatedAt AS DATE) BETWEEN @StartDate AND @EndDate
      AND p.Status = 1; -- Only completed / paid

    -- Summary
    SELECT 
        COUNT(*) AS InvoiceCount,
        SUM(TaxableValue) AS TotalTaxableValue,
        SUM(DiscountAmount) AS TotalDiscount,
        SUM(CGSTAmount) AS TotalCGST,
        SUM(SGSTAmount) AS TotalSGST,
        SUM(InvoiceTotal) AS NetAmount,
        CASE WHEN COUNT(*) > 0 THEN SUM(TaxableValue) / COUNT(*) ELSE 0 END AS AverageTaxablePerInvoice,
        CASE WHEN COUNT(*) > 0 THEN (SUM(CGSTAmount)+SUM(SGSTAmount)) / COUNT(*) ELSE 0 END AS AverageGSTPerInvoice
    FROM #PaymentCTE;

    -- Detail rows (grouped per order)
    SELECT 
        MIN(CreatedAt) AS PaymentDate,
        OrderNumber,
        SUM(TaxableValue) AS TaxableValue,
        SUM(DiscountAmount) AS DiscountAmount,
        MAX(CGSTPerc) AS CGSTPercentage,
        SUM(CGSTAmount) AS CGSTAmount,
        MAX(SGSTPerc) AS SGSTPercentage,
        SUM(SGSTAmount) AS SGSTAmount,
        SUM(TotalGST) AS TotalGST,
        SUM(InvoiceTotal) AS InvoiceTotal
    FROM #PaymentCTE
    GROUP BY OrderNumber
    ORDER BY MIN(CreatedAt) ASC;
END
GO
