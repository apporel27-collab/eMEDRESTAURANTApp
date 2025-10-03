-- Add CancelledAt column to Orders table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CancelledAt')
BEGIN
    ALTER TABLE Orders
    ADD CancelledAt DATETIME NULL;
    
    PRINT 'CancelledAt column added to Orders table successfully';
END
ELSE
BEGIN
    PRINT 'CancelledAt column already exists in Orders table';
END

-- Update the status values in Orders table if needed
-- Make sure Status = 4 is recognized as 'Cancelled'
-- This is just documentation - your application code already handles this
/*
Status values in Orders table:
0 = Open
1 = In Progress
2 = Ready
3 = Completed
4 = Cancelled
*/
