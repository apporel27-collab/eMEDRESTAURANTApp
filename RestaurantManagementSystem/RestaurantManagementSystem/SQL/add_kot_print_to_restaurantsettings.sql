-- Adds IsKOTBillPrintRequired column to dbo.RestaurantSettings if it does not exist
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[RestaurantSettings]') AND name = 'IsKOTBillPrintRequired'
)
BEGIN
    ALTER TABLE [dbo].[RestaurantSettings]
    ADD [IsKOTBillPrintRequired] BIT NOT NULL DEFAULT(0)
END

-- Normalize existing rows to ensure no NULLs
IF EXISTS (SELECT 1 FROM [dbo].[RestaurantSettings])
BEGIN
    UPDATE dbo.RestaurantSettings
    SET IsKOTBillPrintRequired = ISNULL(IsKOTBillPrintRequired, 0)
    WHERE Id IS NOT NULL
END
