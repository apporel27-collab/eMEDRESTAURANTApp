-- Auth_Setup.sql
-- Creates the authentication and authorization database structure

-- Users Table
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Salt NVARCHAR(MAX) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    IsLockedOut BIT NOT NULL DEFAULT 0,
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    LastLoginDate DATETIME NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
    MustChangePassword BIT NOT NULL DEFAULT 0,
    PasswordLastChanged DATETIME NULL,
    RequiresMFA BIT NOT NULL DEFAULT 0
);

-- Roles Table
CREATE TABLE Roles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200) NULL,
    IsSystemRole BIT NOT NULL DEFAULT 0,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE()
);

-- Permissions Table
CREATE TABLE Permissions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(200) NULL,
    Category NVARCHAR(50) NOT NULL,
    IsSystemPermission BIT NOT NULL DEFAULT 0
);

-- Role Permissions (Many-to-Many relationship)
CREATE TABLE RolePermissions (
    RoleId INT NOT NULL,
    PermissionId INT NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);

-- User Roles (Many-to-Many relationship)
CREATE TABLE UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    AssignedDate DATETIME NOT NULL DEFAULT GETDATE(),
    AssignedBy INT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_AssignedBy FOREIGN KEY (AssignedBy) REFERENCES Users(Id)
);

-- Outlets Table (For outlet scopes)
CREATE TABLE Outlets (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Location NVARCHAR(200) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE()
);

-- User Outlet Scopes
CREATE TABLE UserOutletScopes (
    UserId INT NOT NULL,
    OutletId INT NOT NULL,
    AssignedDate DATETIME NOT NULL DEFAULT GETDATE(),
    AssignedBy INT NULL,
    CONSTRAINT PK_UserOutletScopes PRIMARY KEY (UserId, OutletId),
    CONSTRAINT FK_UserOutletScopes_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserOutletScopes_Outlets FOREIGN KEY (OutletId) REFERENCES Outlets(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserOutletScopes_AssignedBy FOREIGN KEY (AssignedBy) REFERENCES Users(Id)
);

-- MFA Factors
CREATE TABLE MFAFactors (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    FactorType NVARCHAR(20) NOT NULL, -- 'Email', 'Phone', 'App'
    FactorValue NVARCHAR(100) NOT NULL, -- Email, Phone number, or App ID
    IsVerified BIT NOT NULL DEFAULT 0,
    IsDefault BIT NOT NULL DEFAULT 0,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastUsedDate DATETIME NULL,
    CONSTRAINT FK_MFAFactors_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Audit Log
CREATE TABLE AuditLog (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    UserId INT NULL,
    Action NVARCHAR(100) NOT NULL,
    Details NVARCHAR(MAX) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    EntityName NVARCHAR(100) NULL,
    EntityId NVARCHAR(50) NULL,
    Timestamp DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_AuditLog_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Sessions
CREATE TABLE UserSessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId INT NOT NULL,
    Token NVARCHAR(MAX) NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    ExpiryDate DATETIME NOT NULL,
    LastActivityDate DATETIME NOT NULL,
    IpAddress NVARCHAR(50) NULL,
    DeviceId NVARCHAR(100) NULL,
    UserAgent NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Devices
CREATE TABLE Devices (
    Id NVARCHAR(100) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    DeviceType NVARCHAR(50) NOT NULL, -- 'POS', 'KDS', 'Manager', 'Mobile'
    OutletId INT NULL,
    StationName NVARCHAR(50) NULL,
    RegisteredDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastActiveDate DATETIME NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active', -- 'Active', 'Inactive', 'Maintenance', 'Blocked'
    Notes NVARCHAR(500) NULL,
    CONSTRAINT FK_Devices_Outlets FOREIGN KEY (OutletId) REFERENCES Outlets(Id)
);

-- Device Terminal Bindings
CREATE TABLE TerminalBindings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DeviceId NVARCHAR(100) NOT NULL,
    TerminalName NVARCHAR(50) NOT NULL,
    AllowedRoles NVARCHAR(MAX) NULL, -- Comma-separated list of role IDs or JSON array
    SessionTimeout INT NOT NULL DEFAULT 30, -- Minutes
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TerminalBindings_Devices FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
);

-- Access Reviews
CREATE TABLE AccessReviews (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- 'Pending', 'InProgress', 'Completed'
    ReviewType NVARCHAR(50) NOT NULL, -- 'Regular', 'AdHoc'
    Scope NVARCHAR(50) NOT NULL, -- 'All', 'Outlet', 'Role'
    ScopeId INT NULL, -- Outlet ID or Role ID if applicable
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_AccessReviews_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);

-- Access Review Tasks
CREATE TABLE AccessReviewTasks (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ReviewId INT NOT NULL,
    UserId INT NOT NULL,
    ReviewerId INT NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Approved', 'Removed', 'Modified'
    CompletedDate DATETIME NULL,
    Comments NVARCHAR(500) NULL,
    CONSTRAINT FK_AccessReviewTasks_Reviews FOREIGN KEY (ReviewId) REFERENCES AccessReviews(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AccessReviewTasks_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_AccessReviewTasks_Reviewers FOREIGN KEY (ReviewerId) REFERENCES Users(Id)
);

-- Override Requests
CREATE TABLE OverrideRequests (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RequestType NVARCHAR(50) NOT NULL, -- 'Void', 'Refund', 'Discount', 'PriceOverride', 'DayReopen'
    RequesterId INT NOT NULL,
    ApproverId INT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Approved', 'Rejected'
    Amount DECIMAL(18, 2) NULL,
    OrderId INT NULL,
    Justification NVARCHAR(500) NOT NULL,
    ApproverJustification NVARCHAR(500) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    RespondedDate DATETIME NULL,
    CONSTRAINT FK_OverrideRequests_Requesters FOREIGN KEY (RequesterId) REFERENCES Users(Id),
    CONSTRAINT FK_OverrideRequests_Approvers FOREIGN KEY (ApproverId) REFERENCES Users(Id)
);

-- Insert default roles
INSERT INTO Roles (Name, Description, IsSystemRole) VALUES
('Administrator', 'Full system access', 1),
('Manager', 'Restaurant management', 1),
('Staff', 'Regular staff member', 1),
('Kitchen', 'Kitchen staff', 1),
('Cashier', 'Cashier/POS operator', 1);

-- Insert default permissions
INSERT INTO Permissions (Name, Description, Category, IsSystemPermission) VALUES
-- User Management Permissions
('users.view', 'View users', 'UserManagement', 1),
('users.create', 'Create users', 'UserManagement', 1),
('users.edit', 'Edit users', 'UserManagement', 1),
('users.delete', 'Delete users', 'UserManagement', 1),
('roles.assign', 'Assign roles', 'UserManagement', 1),
('outlets.assign', 'Assign outlet scopes', 'UserManagement', 1),

-- Menu Management Permissions
('menu.view', 'View menu items', 'MenuManagement', 1),
('menu.create', 'Create menu items', 'MenuManagement', 1),
('menu.edit', 'Edit menu items', 'MenuManagement', 1),
('menu.delete', 'Delete menu items', 'MenuManagement', 1),

-- Order Management Permissions
('orders.view', 'View orders', 'OrderManagement', 1),
('orders.create', 'Create orders', 'OrderManagement', 1),
('orders.edit', 'Edit orders', 'OrderManagement', 1),
('orders.cancel', 'Cancel orders', 'OrderManagement', 1),

-- Payment Permissions
('payments.process', 'Process payments', 'PaymentManagement', 1),
('payments.refund', 'Process refunds', 'PaymentManagement', 1),
('payments.void', 'Void payments', 'PaymentManagement', 1),

-- Kitchen Permissions
('kitchen.view', 'View kitchen orders', 'KitchenManagement', 1),
('kitchen.update', 'Update order status', 'KitchenManagement', 1),

-- Table Service Permissions
('tables.view', 'View tables', 'TableManagement', 1),
('tables.assign', 'Assign tables', 'TableManagement', 1),
('tables.modify', 'Modify table status', 'TableManagement', 1),

-- Reporting Permissions
('reports.view', 'View reports', 'Reporting', 1),
('reports.create', 'Create reports', 'Reporting', 1),
('reports.export', 'Export reports', 'Reporting', 1),

-- Settings Permissions
('settings.view', 'View settings', 'SystemSettings', 1),
('settings.modify', 'Modify settings', 'SystemSettings', 1);

-- Assign permissions to roles
-- Administrator (all permissions)
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT 1, Id FROM Permissions;

-- Manager permissions
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT 2, Id FROM Permissions 
WHERE Name IN (
    'users.view', 'roles.assign', 'outlets.assign',
    'menu.view', 'menu.create', 'menu.edit',
    'orders.view', 'orders.create', 'orders.edit', 'orders.cancel',
    'payments.process', 'payments.refund', 'payments.void',
    'kitchen.view', 
    'tables.view', 'tables.assign', 'tables.modify',
    'reports.view', 'reports.create', 'reports.export'
);

-- Staff permissions
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT 3, Id FROM Permissions 
WHERE Name IN (
    'menu.view',
    'orders.view', 'orders.create', 'orders.edit',
    'payments.process',
    'tables.view', 'tables.modify'
);

-- Kitchen permissions
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT 4, Id FROM Permissions 
WHERE Name IN (
    'kitchen.view', 'kitchen.update',
    'menu.view'
);

-- Cashier permissions
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT 5, Id FROM Permissions 
WHERE Name IN (
    'menu.view',
    'orders.view', 'orders.create',
    'payments.process',
    'tables.view'
);

-- Create a default admin user
-- Password is 'Admin@123' (this is a hashed password with salt - in production use proper password hashing)
INSERT INTO Users (Username, Email, PasswordHash, Salt, FirstName, LastName, RequiresMFA)
VALUES ('admin', 'admin@restaurant.com', 
        '1000:5b42403363486974614c33736d4f7373:ff7e21ebb8123c9d9e4aa13e93ce5a20868cb0dc530ef1e7bb1c3da51a608e172e52de7d39e26ce9ca40596fbb30826b14c01089ddad46bd97240703ac56e45e', -- Hashed password for 'Admin@123'
        '5b42403363486974614c33736d4f7373', -- Salt
        'System', 'Administrator', 1);

-- Assign admin role to admin user
INSERT INTO UserRoles (UserId, RoleId)
VALUES (1, 1);

-- Create a default outlet
INSERT INTO Outlets (Name, Location)
VALUES ('Main Restaurant', '123 Main Street');

-- Assign admin to the default outlet
INSERT INTO UserOutletScopes (UserId, OutletId)
VALUES (1, 1);

-- Stored procedure to authenticate a user
CREATE PROCEDURE sp_AuthenticateUser
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @UserId INT;
    DECLARE @IsLockedOut BIT;
    DECLARE @FailedAttempts INT;
    
    -- Get user info
    SELECT @UserId = Id, @IsLockedOut = IsLockedOut, @FailedAttempts = FailedLoginAttempts
    FROM Users
    WHERE Username = @Username AND IsActive = 1;
    
    -- Check if user exists and is not locked out
    IF @UserId IS NULL
    BEGIN
        SELECT 0 AS Success, 'Invalid username or password' AS Message, NULL AS UserId;
        RETURN;
    END
    
    IF @IsLockedOut = 1
    BEGIN
        SELECT 0 AS Success, 'Account is locked. Please contact administrator.' AS Message, NULL AS UserId;
        RETURN;
    END
    
    -- Check password
    DECLARE @StoredHash NVARCHAR(MAX);
    SELECT @StoredHash = PasswordHash FROM Users WHERE Id = @UserId;
    
    IF @StoredHash = @PasswordHash
    BEGIN
        -- Successful login
        UPDATE Users
        SET LastLoginDate = GETDATE(),
            FailedLoginAttempts = 0
        WHERE Id = @UserId;
        
        -- Return user details
        SELECT 1 AS Success, 'Login successful' AS Message, Id AS UserId, Username, Email, FirstName, LastName, RequiresMFA
        FROM Users
        WHERE Id = @UserId;
    END
    ELSE
    BEGIN
        -- Failed login attempt
        SET @FailedAttempts = @FailedAttempts + 1;
        
        -- Lock account after 5 failed attempts
        IF @FailedAttempts >= 5
        BEGIN
            UPDATE Users
            SET FailedLoginAttempts = @FailedAttempts,
                IsLockedOut = 1
            WHERE Id = @UserId;
            
            SELECT 0 AS Success, 'Account locked after multiple failed attempts' AS Message, NULL AS UserId;
        END
        ELSE
        BEGIN
            UPDATE Users
            SET FailedLoginAttempts = @FailedAttempts
            WHERE Id = @UserId;
            
            SELECT 0 AS Success, 'Invalid username or password' AS Message, NULL AS UserId;
        END
    END
END;

-- Stored procedure to get user roles and permissions
CREATE PROCEDURE sp_GetUserRolesAndPermissions
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get user roles
    SELECT r.Id, r.Name, r.Description
    FROM Roles r
    INNER JOIN UserRoles ur ON r.Id = ur.RoleId
    WHERE ur.UserId = @UserId;
    
    -- Get user permissions
    SELECT DISTINCT p.Id, p.Name, p.Description, p.Category
    FROM Permissions p
    INNER JOIN RolePermissions rp ON p.Id = rp.PermissionId
    INNER JOIN UserRoles ur ON rp.RoleId = ur.RoleId
    WHERE ur.UserId = @UserId;
    
    -- Get user outlet scopes
    SELECT o.Id, o.Name, o.Location
    FROM Outlets o
    INNER JOIN UserOutletScopes uos ON o.Id = uos.OutletId
    WHERE uos.UserId = @UserId AND o.IsActive = 1;
END;

-- Stored procedure to create a new audit log entry
CREATE PROCEDURE sp_CreateAuditLog
    @UserId INT = NULL,
    @Action NVARCHAR(100),
    @Details NVARCHAR(MAX) = NULL,
    @IpAddress NVARCHAR(50) = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @EntityName NVARCHAR(100) = NULL,
    @EntityId NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO AuditLog (UserId, Action, Details, IpAddress, UserAgent, EntityName, EntityId)
    VALUES (@UserId, @Action, @Details, @IpAddress, @UserAgent, @EntityName, @EntityId);
    
    SELECT SCOPE_IDENTITY() AS Id;
END;

-- Stored procedure to create or update a session
CREATE PROCEDURE sp_CreateOrUpdateSession
    @UserId INT,
    @Token NVARCHAR(MAX),
    @ExpiryMinutes INT = 60,
    @IpAddress NVARCHAR(50) = NULL,
    @DeviceId NVARCHAR(100) = NULL,
    @UserAgent NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SessionId UNIQUEIDENTIFIER = NEWID();
    DECLARE @ExpiryDate DATETIME = DATEADD(MINUTE, @ExpiryMinutes, GETDATE());
    
    -- Deactivate existing sessions for this user/device if necessary
    UPDATE UserSessions
    SET IsActive = 0
    WHERE UserId = @UserId 
      AND (DeviceId = @DeviceId OR DeviceId IS NULL)
      AND IsActive = 1;
    
    -- Create new session
    INSERT INTO UserSessions (Id, UserId, Token, ExpiryDate, LastActivityDate, IpAddress, DeviceId, UserAgent)
    VALUES (@SessionId, @UserId, @Token, @ExpiryDate, GETDATE(), @IpAddress, @DeviceId, @UserAgent);
    
    -- Return session info
    SELECT @SessionId AS SessionId, @ExpiryDate AS ExpiryDate;
END;

-- Stored procedure to validate session
CREATE PROCEDURE sp_ValidateSession
    @Token NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @UserId INT;
    DECLARE @IsActive BIT;
    DECLARE @ExpiryDate DATETIME;
    
    -- Get session info
    SELECT @UserId = UserId, @IsActive = IsActive, @ExpiryDate = ExpiryDate
    FROM UserSessions
    WHERE Token = @Token;
    
    -- Check if session exists and is valid
    IF @UserId IS NULL OR @IsActive = 0 OR @ExpiryDate < GETDATE()
    BEGIN
        SELECT 0 AS IsValid, NULL AS UserId;
        RETURN;
    END
    
    -- Update last activity
    UPDATE UserSessions
    SET LastActivityDate = GETDATE()
    WHERE Token = @Token;
    
    -- Return user info
    SELECT 1 AS IsValid, u.Id AS UserId, u.Username, u.Email, u.FirstName, u.LastName
    FROM Users u
    WHERE u.Id = @UserId AND u.IsActive = 1;
END;

-- Stored procedure to handle override requests
CREATE PROCEDURE sp_CreateOverrideRequest
    @RequestType NVARCHAR(50),
    @RequesterId INT,
    @Amount DECIMAL(18, 2) = NULL,
    @OrderId INT = NULL,
    @Justification NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO OverrideRequests (RequestType, RequesterId, Amount, OrderId, Justification)
    VALUES (@RequestType, @RequesterId, @Amount, @OrderId, @Justification);
    
    DECLARE @RequestId INT = SCOPE_IDENTITY();
    
    -- Return the created request
    SELECT Id, RequestType, Status, CreatedDate
    FROM OverrideRequests
    WHERE Id = @RequestId;
END;

-- Stored procedure to approve/deny override request
CREATE PROCEDURE sp_RespondToOverrideRequest
    @RequestId INT,
    @ApproverId INT,
    @Status NVARCHAR(20), -- 'Approved' or 'Rejected'
    @ApproverJustification NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE OverrideRequests
    SET ApproverId = @ApproverId,
        Status = @Status,
        ApproverJustification = @ApproverJustification,
        RespondedDate = GETDATE()
    WHERE Id = @RequestId;
    
    -- Return the updated request
    SELECT Id, RequestType, Status, CreatedDate, RespondedDate
    FROM OverrideRequests
    WHERE Id = @RequestId;
END;
