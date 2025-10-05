-- Adds TakeAwayGSTPercentage column to dbo.RestaurantSettings if it does not already exist

IF NOT EXISTS (
      SELECT * FROM syscolumns 
      WHERE ID=Object_ID('dbo.RestaurantSettings') 
      AND Object_ID = Object_ID(N'dbo.RestaurantSettings')
   )
BEGIN
    ALTER TABLE dbo.RestaurantSettings

-- Optional: backfill any nulls if somehow created without default (safety)
UPDATE dbo.RestaurantSettings 
SET TakeAwayGSTPercentage = 5.00 
WHERE TakeAwayGSTPercentage IS NULL;
GO
