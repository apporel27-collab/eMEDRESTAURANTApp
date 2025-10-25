CREATE PROCEDURE [dbo].[usp_CreateOrUpdateUserWithRoles]
    @Id INT = 0,
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255),
    @Salt NVARCHAR(100),
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50) = NULL,
    @Email NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @IsActive BIT = 1,
    @RoleIds NVARCHAR(MAX) = NULL -- comma-separated role IDs
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NewUserId INT;

    IF @Id = 0
    BEGIN
    INSERT INTO [dbo].[Users] (Username, PasswordHash, Salt, FirstName, LastName, Email, Phone, IsActive)
        VALUES (@Username, @PasswordHash, @Salt, @FirstName, @LastName, @Email, @Phone, @IsActive);
        SET @NewUserId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
    UPDATE [dbo].[Users]
        SET Username = @Username,
            PasswordHash = @PasswordHash,
            Salt = @Salt,
            FirstName = @FirstName,
            LastName = @LastName,
            Email = @Email,
            Phone = @Phone,
            IsActive = @IsActive
        WHERE Id = @Id;
        SET @NewUserId = @Id;
    END

    -- Remove existing role mappings
    DELETE FROM [dbo].[UserRoles] WHERE UserId = @NewUserId;

    -- Add new role mappings
    IF @RoleIds IS NOT NULL AND LEN(@RoleIds) > 0
    BEGIN
        DECLARE @RoleIdTable TABLE (RoleId INT);
        DECLARE @Pos INT = 0, @NextPos INT, @RoleId NVARCHAR(10);
        SET @RoleIds = @RoleIds + ',';
        WHILE CHARINDEX(',', @RoleIds, @Pos + 1) > 0
        BEGIN
            SET @NextPos = CHARINDEX(',', @RoleIds, @Pos + 1);
            SET @RoleId = SUBSTRING(@RoleIds, @Pos + 1, @NextPos - @Pos - 1);
            INSERT INTO @RoleIdTable (RoleId) VALUES (CAST(@RoleId AS INT));
            SET @Pos = @NextPos;
        END
    INSERT INTO [dbo].[UserRoles] (UserId, RoleId)
        SELECT @NewUserId, RoleId FROM @RoleIdTable;
    END

    SELECT @NewUserId AS UserId;
END
