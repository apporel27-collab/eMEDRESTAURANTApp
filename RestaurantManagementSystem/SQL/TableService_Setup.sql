-- Create tables for UC-002: Seat Guest & Assign Server

-- Create ServerAssignments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ServerAssignments')
BEGIN
    CREATE TABLE [dbo].[ServerAssignments] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [TableId] INT NOT NULL,
        [ServerId] INT NOT NULL,
        [AssignedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [LastModifiedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [AssignedById] INT NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [FK_ServerAssignments_Tables] FOREIGN KEY ([TableId]) REFERENCES [Tables]([Id]),
        CONSTRAINT [FK_ServerAssignments_Users_Server] FOREIGN KEY ([ServerId]) REFERENCES [Users]([Id]),
        CONSTRAINT [FK_ServerAssignments_Users_AssignedBy] FOREIGN KEY ([AssignedById]) REFERENCES [Users]([Id])
    );
END
GO

-- Create TableTurnovers Table for tracking turn times
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TableTurnovers')
BEGIN
    CREATE TABLE [dbo].[TableTurnovers] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [TableId] INT NOT NULL,
        [ReservationId] INT NULL,
        [WaitlistId] INT NULL,
        [GuestName] NVARCHAR(100) NOT NULL,
        [PartySize] INT NOT NULL,
        [SeatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [StartedServiceAt] DATETIME NULL,
        [CompletedAt] DATETIME NULL,
        [DepartedAt] DATETIME NULL,
        [Status] INT NOT NULL DEFAULT 0, -- 0=Seated, 1=InService, 2=CheckRequested, 3=Paid, 4=Completed, 5=Departed
        [Notes] NVARCHAR(500) NULL,
        [TargetTurnTimeMinutes] INT NOT NULL DEFAULT 90,
        CONSTRAINT [FK_TableTurnovers_Tables] FOREIGN KEY ([TableId]) REFERENCES [Tables]([Id]),
        CONSTRAINT [FK_TableTurnovers_Reservations] FOREIGN KEY ([ReservationId]) REFERENCES [Reservations]([Id]),
        CONSTRAINT [FK_TableTurnovers_Waitlist] FOREIGN KEY ([WaitlistId]) REFERENCES [Waitlist]([Id])
    );
END
GO

-- Create stored procedure for server assignments
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_AssignServerToTable')
    DROP PROCEDURE usp_AssignServerToTable
GO

CREATE PROCEDURE [dbo].[usp_AssignServerToTable]
    @TableId INT,
    @ServerId INT,
    @AssignedById INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Message NVARCHAR(200);
    
    -- Check if table exists
    IF NOT EXISTS (SELECT 1 FROM [Tables] WHERE [Id] = @TableId)
    BEGIN
        SELECT 'Table does not exist.' AS [Message];
        RETURN;
    END
    
    -- Check if server exists and has Server role
    IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = @ServerId AND [Role] = 2) -- Role 2 = Server
    BEGIN
        SELECT 'Invalid server selected. User does not exist or is not a server.' AS [Message];
        RETURN;
    END
    
    -- Deactivate any existing active assignments for this table
    UPDATE [ServerAssignments]
    SET [IsActive] = 0,
        [LastModifiedAt] = GETDATE()
    WHERE [TableId] = @TableId AND [IsActive] = 1;
    
    -- Create new assignment
    INSERT INTO [ServerAssignments] (
        [TableId],
        [ServerId],
        [AssignedAt],
        [LastModifiedAt],
        [AssignedById],
        [IsActive]
    ) VALUES (
        @TableId,
        @ServerId,
        GETDATE(),
        GETDATE(),
        @AssignedById,
        1
    );
    
    SET @Message = 'Server assigned successfully.';
    SELECT @Message AS [Message];
END
GO

-- Create stored procedure for starting table turnover
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_StartTableTurnover')
    DROP PROCEDURE usp_StartTableTurnover
GO

CREATE PROCEDURE [dbo].[usp_StartTableTurnover]
    @TableId INT,
    @ReservationId INT = NULL,
    @WaitlistId INT = NULL,
    @GuestName NVARCHAR(100),
    @PartySize INT,
    @Notes NVARCHAR(500) = NULL,
    @TargetTurnTimeMinutes INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Message NVARCHAR(200);
    DECLARE @ErrorMsg NVARCHAR(200) = NULL;
    
    -- Check if table exists
    IF NOT EXISTS (SELECT 1 FROM [Tables] WHERE [Id] = @TableId)
    BEGIN
        SET @ErrorMsg = 'Table does not exist.';
    END
    
    -- Check if table is available
    IF EXISTS (SELECT 1 FROM [Tables] WHERE [Id] = @TableId AND [Status] != 0) -- 0 = Available
    BEGIN
        SET @ErrorMsg = 'Table is not available.';
    END
    
    -- Check if reservation exists if provided
    IF @ReservationId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Reservations] WHERE [Id] = @ReservationId)
    BEGIN
        SET @ErrorMsg = 'Reservation does not exist.';
    END
    
    -- Check if waitlist entry exists if provided
    IF @WaitlistId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Waitlist] WHERE [Id] = @WaitlistId)
    BEGIN
        SET @ErrorMsg = 'Waitlist entry does not exist.';
    END
    
    -- If there's an error, return it
    IF @ErrorMsg IS NOT NULL
    BEGIN
        SELECT @ErrorMsg AS [Message];
        RETURN;
    END
    
    -- Begin transaction
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Create new table turnover record
        INSERT INTO [TableTurnovers] (
            [TableId],
            [ReservationId],
            [WaitlistId],
            [GuestName],
            [PartySize],
            [SeatedAt],
            [Status],
            [Notes],
            [TargetTurnTimeMinutes]
        ) VALUES (
            @TableId,
            @ReservationId,
            @WaitlistId,
            @GuestName,
            @PartySize,
            GETDATE(),
            0, -- Seated
            @Notes,
            @TargetTurnTimeMinutes
        );
        
        -- Update table status to occupied
        UPDATE [Tables]
        SET [Status] = 2, -- Occupied
            [LastOccupiedAt] = GETDATE()
        WHERE [Id] = @TableId;
        
        -- Update reservation status if provided
        IF @ReservationId IS NOT NULL
        BEGIN
            UPDATE [Reservations]
            SET [Status] = 2, -- Seated
                [UpdatedAt] = GETDATE()
            WHERE [Id] = @ReservationId;
        END
        
        -- Update waitlist status if provided
        IF @WaitlistId IS NOT NULL
        BEGIN
            UPDATE [Waitlist]
            SET [Status] = 2, -- Seated
                [SeatedAt] = GETDATE()
            WHERE [Id] = @WaitlistId;
        END
        
        COMMIT TRANSACTION;
        SET @Message = 'Table turnover started successfully.';
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SET @Message = 'Error starting table turnover: ' + ERROR_MESSAGE();
    END CATCH
    
    SELECT @Message AS [Message];
END
GO

-- Create stored procedure for updating table turnover status
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpdateTableTurnoverStatus')
    DROP PROCEDURE usp_UpdateTableTurnoverStatus
GO

CREATE PROCEDURE [dbo].[usp_UpdateTableTurnoverStatus]
    @TurnoverId INT,
    @NewStatus INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Message NVARCHAR(200);
    DECLARE @TableId INT;
    DECLARE @CurrentStatus INT;
    
    -- Check if turnover record exists
    IF NOT EXISTS (SELECT 1 FROM [TableTurnovers] WHERE [Id] = @TurnoverId)
    BEGIN
        SELECT 'Turnover record does not exist.' AS [Message];
        RETURN;
    END
    
    -- Get current status and table ID
    SELECT @CurrentStatus = [Status], @TableId = [TableId]
    FROM [TableTurnovers]
    WHERE [Id] = @TurnoverId;
    
    -- Begin transaction
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Update turnover status and timestamps based on new status
        UPDATE [TableTurnovers]
        SET [Status] = @NewStatus,
            [StartedServiceAt] = CASE WHEN @NewStatus = 1 AND [StartedServiceAt] IS NULL THEN GETDATE() ELSE [StartedServiceAt] END,
            [CompletedAt] = CASE WHEN @NewStatus = 4 AND [CompletedAt] IS NULL THEN GETDATE() ELSE [CompletedAt] END,
            [DepartedAt] = CASE WHEN @NewStatus = 5 AND [DepartedAt] IS NULL THEN GETDATE() ELSE [DepartedAt] END
        WHERE [Id] = @TurnoverId;
        
        -- If status is Departed (5), update table status to Dirty (3)
        IF @NewStatus = 5
        BEGIN
            UPDATE [Tables]
            SET [Status] = 3 -- Dirty
            WHERE [Id] = @TableId;
        END
        
        COMMIT TRANSACTION;
        SET @Message = 'Table turnover status updated successfully.';
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SET @Message = 'Error updating turnover status: ' + ERROR_MESSAGE();
    END CATCH
    
    SELECT @Message AS [Message];
END
GO
