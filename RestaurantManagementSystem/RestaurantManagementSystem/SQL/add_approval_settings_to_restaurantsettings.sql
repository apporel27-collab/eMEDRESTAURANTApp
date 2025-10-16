-- Adds two columns to dbo.RestaurantSettings if they do not exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RestaurantSettings]') AND name = 'IsDiscountApprovalRequired')
BEGIN
    ALTER TABLE [dbo].[RestaurantSettings] ADD [IsDiscountApprovalRequired] BIT NOT NULL DEFAULT(0)
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RestaurantSettings]') AND name = 'IsCardPaymentApprovalRequired')
BEGIN
    ALTER TABLE [dbo].[RestaurantSettings] ADD [IsCardPaymentApprovalRequired] BIT NOT NULL DEFAULT(0)
END

-- Insert default row if table empty
IF NOT EXISTS (SELECT 1 FROM [dbo].[RestaurantSettings])
BEGIN
    INSERT INTO [dbo].[RestaurantSettings] (RestaurantName, StreetAddress, City, State, Pincode, Country, GSTCode, PhoneNumber, Email, Website, LogoPath, CurrencySymbol, DefaultGSTPercentage, TakeAwayGSTPercentage, IsDefaultGSTRequired, IsTakeAwayGSTRequired, IsDiscountApprovalRequired, IsCardPaymentApprovalRequired, BillFormat, CreatedAt, UpdatedAt)
    VALUES ('My Restaurant', '', '', '', '000000', '', '', '', '', '', '', '₹', 5.00, 5.00, 1, 1, 0, 0, 'A4', GETDATE(), GETDATE())
END
