using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;
using Microsoft.Data.SqlClient;

namespace RestaurantManagementSystem.Services
{
    public class SettingsService
    {
        private readonly RestaurantDbContext _dbContext;
        private readonly string _connectionString;

        public SettingsService(RestaurantDbContext dbContext, string connectionString)
        {
            _dbContext = dbContext;
            _connectionString = connectionString;
        }

        public async Task<RestaurantSettings> GetSettingsAsync()
        {
            try
            {
                // Get settings from the database
                var settings = await _dbContext.RestaurantSettings.FirstOrDefaultAsync();
                
                // If no settings exist, create default settings
                if (settings == null)
                {
                    settings = new RestaurantSettings
                    {
                        RestaurantName = "My Restaurant",
                        StreetAddress = "123 Main Street",
                        City = "Mumbai",
                        State = "Maharashtra",
                        Pincode = "400001",
                        Country = "India",
                        GSTCode = "27AAPFU0939F1ZV",
                        PhoneNumber = "+919876543210",
                        Email = "info@myrestaurant.com",
                        Website = "https://www.myrestaurant.com",
                        CurrencySymbol = "₹",
                        DefaultGSTPercentage = 5.00m,
                        TakeAwayGSTPercentage = 5.00m,
                        IsDefaultGSTRequired = true,
                        BillFormat = "A4"
                    };
                    
                    _dbContext.RestaurantSettings.Add(settings);
                    await _dbContext.SaveChangesAsync();
                }
                
                return settings;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting restaurant settings: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateSettingsAsync(RestaurantSettings settings)
        {
            try
            {
                // Get current settings
                var currentSettings = await _dbContext.RestaurantSettings.FirstOrDefaultAsync();
                
                if (currentSettings != null)
                {
                    // Update properties
                    currentSettings.RestaurantName = settings.RestaurantName;
                    currentSettings.StreetAddress = settings.StreetAddress;
                    currentSettings.City = settings.City;
                    currentSettings.State = settings.State;
                    currentSettings.Pincode = settings.Pincode;
                    currentSettings.Country = settings.Country;
                    currentSettings.GSTCode = settings.GSTCode;
                    currentSettings.PhoneNumber = settings.PhoneNumber;
                    currentSettings.Email = settings.Email;
                    currentSettings.Website = settings.Website;
                    currentSettings.LogoPath = settings.LogoPath;
                    currentSettings.CurrencySymbol = settings.CurrencySymbol;
                    currentSettings.DefaultGSTPercentage = settings.DefaultGSTPercentage;
                    currentSettings.TakeAwayGSTPercentage = settings.TakeAwayGSTPercentage;
                    currentSettings.IsDefaultGSTRequired = settings.IsDefaultGSTRequired;
                    currentSettings.IsTakeAwayGSTRequired = settings.IsTakeAwayGSTRequired;
                    currentSettings.BillFormat = settings.BillFormat;
                    currentSettings.UpdatedAt = DateTime.Now;
                    
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    // If no settings exist (unlikely), add new settings
                    settings.CreatedAt = DateTime.Now;
                    settings.UpdatedAt = DateTime.Now;
                    _dbContext.RestaurantSettings.Add(settings);
                    await _dbContext.SaveChangesAsync();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating restaurant settings: {ex.Message}", ex);
            }
        }

        // Direct SQL method for environments without Entity Framework migrations
        public async Task<bool> EnsureSettingsTableExistsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Check if table exists
                var checkTableCommand = new SqlCommand(@"
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'RestaurantSettings' AND schema_id = SCHEMA_ID('dbo'))
                    SELECT 1
                    ELSE
                    SELECT 0", connection);
                
                var tableExists = (int)await checkTableCommand.ExecuteScalarAsync() == 1;
                
                if (!tableExists)
                {
                    // Create the table using SQL
                    var createTableCommand = new SqlCommand(@"
                    CREATE TABLE [dbo].[RestaurantSettings] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [RestaurantName] NVARCHAR(100) NOT NULL,
                        [StreetAddress] NVARCHAR(200) NOT NULL,
                        [City] NVARCHAR(50) NOT NULL,
                        [State] NVARCHAR(50) NOT NULL,
                        [Pincode] NVARCHAR(10) NOT NULL,
                        [Country] NVARCHAR(50) NOT NULL,
                        [GSTCode] NVARCHAR(15) NOT NULL,
                        [PhoneNumber] NVARCHAR(15) NULL,
                        [Email] NVARCHAR(100) NULL,
                        [Website] NVARCHAR(100) NULL,
                        [LogoPath] NVARCHAR(200) NULL,
                        [CurrencySymbol] NVARCHAR(50) NOT NULL DEFAULT N'₹',
                        [DefaultGSTPercentage] DECIMAL(5,2) NOT NULL DEFAULT 5.00,
                        [TakeAwayGSTPercentage] DECIMAL(5,2) NOT NULL DEFAULT 5.00,
                        [IsDefaultGSTRequired] BIT NOT NULL DEFAULT 1,
                        [IsTakeAwayGSTRequired] BIT NOT NULL DEFAULT 1,
                        [BillFormat] NVARCHAR(10) NOT NULL DEFAULT N'A4',
                        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
                    );
                    
                    -- Insert default restaurant settings
                    INSERT INTO [dbo].[RestaurantSettings] (
                        [RestaurantName], 
                        [StreetAddress], 
                        [City], 
                        [State], 
                        [Pincode], 
                        [Country], 
                        [GSTCode],
                        [PhoneNumber],
                        [Email],
                        [Website],
                        [CurrencySymbol],
                        [DefaultGSTPercentage],
                        [TakeAwayGSTPercentage],
                        [IsDefaultGSTRequired],
                        [IsTakeAwayGSTRequired],
                        [BillFormat]
                    )
                    VALUES (
                        'My Restaurant',
                        'Sample Street Address',
                        'Mumbai',
                        'Maharashtra',
                        '400001',
                        'India',
                        '27AAPFU0939F1ZV',
                        '+919876543210',
                        'info@myrestaurant.com',
                        'https://www.myrestaurant.com',
                        '₹',
                        5.00,
                        5.00,
                        1,
                        1,
                        'A4'
                    );", connection);
                    
                    await createTableCommand.ExecuteNonQueryAsync();
                    return true;
                }
                
                // Also ensure new parameter columns exist
                await EnsureParameterColumnsExistAsync();
                
                return false;
            }
        }
        
        private async Task EnsureParameterColumnsExistAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Check if IsDefaultGSTRequired column exists
                var checkColumn1 = new SqlCommand(@"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'RestaurantSettings' 
                    AND COLUMN_NAME = 'IsDefaultGSTRequired'
                    AND TABLE_SCHEMA = 'dbo'", connection);
                    
                var column1Exists = (int)await checkColumn1.ExecuteScalarAsync() > 0;
                
                if (!column1Exists)
                {
                    var addColumn1 = new SqlCommand(@"
                        ALTER TABLE [dbo].[RestaurantSettings] 
                        ADD [IsDefaultGSTRequired] BIT NOT NULL DEFAULT 1", connection);
                    await addColumn1.ExecuteNonQueryAsync();
                }
                
                // Check if IsTakeAwayGSTRequired column exists
                var checkColumn2 = new SqlCommand(@"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'RestaurantSettings' 
                    AND COLUMN_NAME = 'IsTakeAwayGSTRequired'
                    AND TABLE_SCHEMA = 'dbo'", connection);
                    
                var column2Exists = (int)await checkColumn2.ExecuteScalarAsync() > 0;
                
                if (!column2Exists)
                {
                    var addColumn2 = new SqlCommand(@"
                        ALTER TABLE [dbo].[RestaurantSettings] 
                        ADD [IsTakeAwayGSTRequired] BIT NOT NULL DEFAULT 1", connection);
                    await addColumn2.ExecuteNonQueryAsync();
                }
                
                // Check if BillFormat column exists
                var checkColumn3 = new SqlCommand(@"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'RestaurantSettings' 
                    AND COLUMN_NAME = 'BillFormat'
                    AND TABLE_SCHEMA = 'dbo'", connection);
                    
                var column3Exists = (int)await checkColumn3.ExecuteScalarAsync() > 0;
                
                if (!column3Exists)
                {
                    var addColumn3 = new SqlCommand(@"
                        ALTER TABLE [dbo].[RestaurantSettings] 
                        ADD [BillFormat] NVARCHAR(10) NOT NULL DEFAULT N'A4'", connection);
                    await addColumn3.ExecuteNonQueryAsync();
                }
            }
        }
    }
}