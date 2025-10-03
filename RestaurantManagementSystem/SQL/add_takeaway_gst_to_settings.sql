-- Adds TakeAwayGSTPercentage column to RestaurantSettings if it does not already exist
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = N'TakeAwayGSTPercentage' 
      AND Object_ID = Object_ID(N'RestaurantSettings')
)
BEGIN
    ALTER TABLE RestaurantSettings 
    ADD TakeAwayGSTPercentage DECIMAL(5,2) NOT NULL CONSTRAINT DF_RestaurantSettings_TakeAwayGST DEFAULT(5.00);
END
GO

-- Optional: backfill any nulls if somehow created without default (safety)
UPDATE RestaurantSettings 
SET TakeAwayGSTPercentage = 5.00 
WHERE TakeAwayGSTPercentage IS NULL;
GO
