/*
    Staff Scheduling & Management Setup Script
    UC-006: Staff Scheduling & Management
*/

-- Create Departments Table
CREATE TABLE Departments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Create JobTitles Table
CREATE TABLE JobTitles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    DepartmentId INT NOT NULL,
    HourlyRate DECIMAL(10, 2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_JobTitle_Department FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
);

-- Add department and job title fields to Users table
ALTER TABLE Users ADD DepartmentId INT NULL;
ALTER TABLE Users ADD JobTitleId INT NULL;
ALTER TABLE Users ADD HireDate DATE NULL;
ALTER TABLE Users ADD EmployeeNumber NVARCHAR(20) NULL;
ALTER TABLE Users ADD HourlyRate DECIMAL(10, 2) NULL;
ALTER TABLE Users ADD SchedulePreferences NVARCHAR(MAX) NULL;
ALTER TABLE Users ADD AvailableDays NVARCHAR(50) NULL;
ALTER TABLE Users ADD MaxHoursPerWeek INT NULL;

-- Add foreign key constraints
ALTER TABLE Users ADD CONSTRAINT FK_Users_Department FOREIGN KEY (DepartmentId) REFERENCES Departments(Id);
ALTER TABLE Users ADD CONSTRAINT FK_Users_JobTitle FOREIGN KEY (JobTitleId) REFERENCES JobTitles(Id);

-- Create ShiftTypes Table
CREATE TABLE ShiftTypes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Color NVARCHAR(20) NULL, -- For calendar display
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Create Shifts Table
CREATE TABLE Shifts (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ShiftDate DATE NOT NULL,
    ShiftTypeId INT NOT NULL,
    DepartmentId INT NOT NULL,
    RequiredStaff INT NOT NULL DEFAULT 1,
    Notes NVARCHAR(500) NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Open, 1=Fully Staffed, 2=Understaffed, 3=Cancelled
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Shift_ShiftType FOREIGN KEY (ShiftTypeId) REFERENCES ShiftTypes(Id),
    CONSTRAINT FK_Shift_Department FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
);

-- Create ShiftAssignments Table
CREATE TABLE ShiftAssignments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ShiftId INT NOT NULL,
    UserId INT NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Assigned, 1=Confirmed, 2=Checked-In, 3=Completed, 4=Cancelled, 5=No-Show
    AssignedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CheckInTime DATETIME NULL,
    CheckOutTime DATETIME NULL,
    HoursWorked DECIMAL(5, 2) NULL,
    Notes NVARCHAR(500) NULL,
    CONSTRAINT FK_ShiftAssignment_Shift FOREIGN KEY (ShiftId) REFERENCES Shifts(Id),
    CONSTRAINT FK_ShiftAssignment_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT UK_ShiftAssignment_ShiftUser UNIQUE (ShiftId, UserId)
);

-- Create TimeOffRequests Table
CREATE TABLE TimeOffRequests (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Reason NVARCHAR(500) NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Approved, 2=Denied
    RequestedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ApprovedById INT NULL,
    ApprovedAt DATETIME NULL,
    Notes NVARCHAR(500) NULL,
    CONSTRAINT FK_TimeOffRequest_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_TimeOffRequest_ApprovedBy FOREIGN KEY (ApprovedById) REFERENCES Users(Id)
);

-- Create ShiftSwapRequests Table
CREATE TABLE ShiftSwapRequests (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RequestingUserId INT NOT NULL,
    ShiftAssignmentId INT NOT NULL,
    RequestedUserId INT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Approved, 2=Denied, 3=Cancelled
    RequestedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ApprovedById INT NULL,
    ApprovedAt DATETIME NULL,
    Notes NVARCHAR(500) NULL,
    CONSTRAINT FK_ShiftSwapRequest_RequestingUser FOREIGN KEY (RequestingUserId) REFERENCES Users(Id),
    CONSTRAINT FK_ShiftSwapRequest_ShiftAssignment FOREIGN KEY (ShiftAssignmentId) REFERENCES ShiftAssignments(Id),
    CONSTRAINT FK_ShiftSwapRequest_RequestedUser FOREIGN KEY (RequestedUserId) REFERENCES Users(Id),
    CONSTRAINT FK_ShiftSwapRequest_ApprovedBy FOREIGN KEY (ApprovedById) REFERENCES Users(Id)
);

-- Create AvailabilityTemplates Table
CREATE TABLE AvailabilityTemplates (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    IsDefault BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Create AvailabilityTemplateDetails Table
CREATE TABLE AvailabilityTemplateDetails (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TemplateId INT NOT NULL,
    DayOfWeek INT NOT NULL, -- 0=Sunday, 1=Monday, ..., 6=Saturday
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    CONSTRAINT FK_AvailabilityTemplateDetail_Template FOREIGN KEY (TemplateId) REFERENCES AvailabilityTemplates(Id),
    CONSTRAINT UK_AvailabilityTemplateDetail UNIQUE (TemplateId, DayOfWeek, StartTime, EndTime)
);

-- Create StaffSkills Table
CREATE TABLE StaffSkills (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    IsRequired BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Create UserSkills junction table
CREATE TABLE UserSkills (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    SkillId INT NOT NULL,
    ProficiencyLevel INT NOT NULL DEFAULT 1, -- 1=Basic, 2=Intermediate, 3=Advanced
    CertifiedAt DATETIME NULL,
    Notes NVARCHAR(200) NULL,
    CONSTRAINT FK_UserSkill_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserSkill_Skill FOREIGN KEY (SkillId) REFERENCES StaffSkills(Id),
    CONSTRAINT UK_UserSkill UNIQUE (UserId, SkillId)
);

GO

-- Stored Procedure to get all departments
CREATE PROCEDURE GetAllDepartments
AS
BEGIN
    SELECT 
        Id, 
        Name, 
        Description, 
        IsActive, 
        CreatedAt, 
        UpdatedAt
    FROM 
        Departments
    ORDER BY 
        Name;
END;
GO

-- Stored Procedure to get department by ID
CREATE PROCEDURE GetDepartmentById
    @DepartmentId INT
AS
BEGIN
    SELECT 
        Id, 
        Name, 
        Description, 
        IsActive, 
        CreatedAt, 
        UpdatedAt
    FROM 
        Departments
    WHERE 
        Id = @DepartmentId;
END;
GO

-- Stored Procedure to create a new department
CREATE PROCEDURE CreateDepartment
    @Name NVARCHAR(50),
    @Description NVARCHAR(200) = NULL,
    @IsActive BIT = 1,
    @DepartmentId INT OUTPUT
AS
BEGIN
    -- Check if department name already exists
    IF EXISTS (SELECT 1 FROM Departments WHERE Name = @Name)
    BEGIN
        SET @DepartmentId = 0;
        RETURN;
    END
    
    INSERT INTO Departments (
        Name,
        Description,
        IsActive,
        CreatedAt,
        UpdatedAt
    )
    VALUES (
        @Name,
        @Description,
        @IsActive,
        GETDATE(),
        GETDATE()
    );
    
    SET @DepartmentId = SCOPE_IDENTITY();
END;
GO

-- Stored Procedure to update an existing department
CREATE PROCEDURE UpdateDepartment
    @DepartmentId INT,
    @Name NVARCHAR(50),
    @Description NVARCHAR(200) = NULL,
    @IsActive BIT = 1
AS
BEGIN
    -- Check if department exists
    IF NOT EXISTS (SELECT 1 FROM Departments WHERE Id = @DepartmentId)
    BEGIN
        RETURN;
    END
    
    -- Check if new name conflicts with existing department (but not itself)
    IF EXISTS (SELECT 1 FROM Departments WHERE Name = @Name AND Id <> @DepartmentId)
    BEGIN
        RETURN;
    END
    
    UPDATE Departments
    SET 
        Name = @Name,
        Description = @Description,
        IsActive = @IsActive,
        UpdatedAt = GETDATE()
    WHERE 
        Id = @DepartmentId;
END;
GO

-- Stored Procedure to delete a department
CREATE PROCEDURE DeleteDepartment
    @DepartmentId INT
AS
BEGIN
    -- Check if department is being used by job titles
    IF EXISTS (SELECT 1 FROM JobTitles WHERE DepartmentId = @DepartmentId)
    BEGIN
        RETURN;
    END
    
    -- Check if department is being used by users
    IF EXISTS (SELECT 1 FROM Users WHERE DepartmentId = @DepartmentId)
    BEGIN
        RETURN;
    END
    
    -- Delete the department
    DELETE FROM Departments
    WHERE Id = @DepartmentId;
END;
GO

-- Stored Procedure to get all job titles
CREATE PROCEDURE GetAllJobTitles
AS
BEGIN
    SELECT 
        jt.Id, 
        jt.Title, 
        jt.Description, 
        jt.DepartmentId,
        d.Name AS DepartmentName,
        jt.HourlyRate,
        jt.IsActive, 
        jt.CreatedAt, 
        jt.UpdatedAt
    FROM 
        JobTitles jt
    INNER JOIN
        Departments d ON jt.DepartmentId = d.Id
    ORDER BY 
        d.Name, jt.Title;
END;
GO

-- Stored Procedure to get job titles by department
CREATE PROCEDURE GetJobTitlesByDepartment
    @DepartmentId INT
AS
BEGIN
    SELECT 
        jt.Id, 
        jt.Title, 
        jt.Description, 
        jt.DepartmentId,
        d.Name AS DepartmentName,
        jt.HourlyRate,
        jt.IsActive, 
        jt.CreatedAt, 
        jt.UpdatedAt
    FROM 
        JobTitles jt
    INNER JOIN
        Departments d ON jt.DepartmentId = d.Id
    WHERE
        jt.DepartmentId = @DepartmentId
    ORDER BY 
        jt.Title;
END;
GO

-- Stored Procedure to get job title by ID
CREATE PROCEDURE GetJobTitleById
    @JobTitleId INT
AS
BEGIN
    SELECT 
        jt.Id, 
        jt.Title, 
        jt.Description, 
        jt.DepartmentId,
        d.Name AS DepartmentName,
        jt.HourlyRate,
        jt.IsActive, 
        jt.CreatedAt, 
        jt.UpdatedAt
    FROM 
        JobTitles jt
    INNER JOIN
        Departments d ON jt.DepartmentId = d.Id
    WHERE 
        jt.Id = @JobTitleId;
END;
GO

-- Stored Procedure to create a new job title
CREATE PROCEDURE CreateJobTitle
    @Title NVARCHAR(50),
    @Description NVARCHAR(200) = NULL,
    @DepartmentId INT,
    @HourlyRate DECIMAL(10, 2) = NULL,
    @IsActive BIT = 1,
    @JobTitleId INT OUTPUT
AS
BEGIN
    -- Check if job title already exists in the same department
    IF EXISTS (SELECT 1 FROM JobTitles WHERE Title = @Title AND DepartmentId = @DepartmentId)
    BEGIN
        SET @JobTitleId = 0;
        RETURN;
    END
    
    INSERT INTO JobTitles (
        Title,
        Description,
        DepartmentId,
        HourlyRate,
        IsActive,
        CreatedAt,
        UpdatedAt
    )
    VALUES (
        @Title,
        @Description,
        @DepartmentId,
        @HourlyRate,
        @IsActive,
        GETDATE(),
        GETDATE()
    );
    
    SET @JobTitleId = SCOPE_IDENTITY();
END;
GO

-- Stored Procedure to update an existing job title
CREATE PROCEDURE UpdateJobTitle
    @JobTitleId INT,
    @Title NVARCHAR(50),
    @Description NVARCHAR(200) = NULL,
    @DepartmentId INT,
    @HourlyRate DECIMAL(10, 2) = NULL,
    @IsActive BIT = 1
AS
BEGIN
    -- Check if job title exists
    IF NOT EXISTS (SELECT 1 FROM JobTitles WHERE Id = @JobTitleId)
    BEGIN
        RETURN;
    END
    
    -- Check if new title conflicts with existing job title in the same department (but not itself)
    IF EXISTS (SELECT 1 FROM JobTitles WHERE Title = @Title AND DepartmentId = @DepartmentId AND Id <> @JobTitleId)
    BEGIN
        RETURN;
    END
    
    UPDATE JobTitles
    SET 
        Title = @Title,
        Description = @Description,
        DepartmentId = @DepartmentId,
        HourlyRate = @HourlyRate,
        IsActive = @IsActive,
        UpdatedAt = GETDATE()
    WHERE 
        Id = @JobTitleId;
END;
GO

-- Stored Procedure to delete a job title
CREATE PROCEDURE DeleteJobTitle
    @JobTitleId INT
AS
BEGIN
    -- Check if job title is being used by users
    IF EXISTS (SELECT 1 FROM Users WHERE JobTitleId = @JobTitleId)
    BEGIN
        RETURN;
    END
    
    -- Delete the job title
    DELETE FROM JobTitles
    WHERE Id = @JobTitleId;
END;
GO

-- Stored Procedure to get all shift types
CREATE PROCEDURE GetAllShiftTypes
AS
BEGIN
    SELECT 
        Id, 
        Name, 
        Description, 
        StartTime,
        EndTime,
        Color,
        IsActive, 
        CreatedAt, 
        UpdatedAt
    FROM 
        ShiftTypes
    ORDER BY 
        StartTime;
END;
GO

-- Stored Procedure to get shift type by ID
CREATE PROCEDURE GetShiftTypeById
    @ShiftTypeId INT
AS
BEGIN
    SELECT 
        Id, 
        Name, 
        Description, 
        StartTime,
        EndTime,
        Color,
        IsActive, 
        CreatedAt, 
        UpdatedAt
    FROM 
        ShiftTypes
    WHERE 
        Id = @ShiftTypeId;
END;
GO

-- Stored Procedure to create a new shift type
CREATE PROCEDURE CreateShiftType
    @Name NVARCHAR(50),
    @Description NVARCHAR(200) = NULL,
    @StartTime TIME,
    @EndTime TIME,
    @Color NVARCHAR(20) = NULL,
    @IsActive BIT = 1,
    @ShiftTypeId INT OUTPUT
AS
BEGIN
    -- Check if shift type name already exists
    IF EXISTS (SELECT 1 FROM ShiftTypes WHERE Name = @Name)
    BEGIN
        SET @ShiftTypeId = 0;
        RETURN;
    END
    
    INSERT INTO ShiftTypes (
        Name,
        Description,
        StartTime,
        EndTime,
        Color,
        IsActive,
        CreatedAt,
        UpdatedAt
    )
    VALUES (
        @Name,
        @Description,
        @StartTime,
        @EndTime,
        @Color,
        @IsActive,
        GETDATE(),
        GETDATE()
    );
    
    SET @ShiftTypeId = SCOPE_IDENTITY();
END;
GO

-- Stored Procedure to update an existing shift type
CREATE PROCEDURE UpdateShiftType
    @ShiftTypeId INT,
    @Name NVARCHAR(50),
    @Description NVARCHAR(200) = NULL,
    @StartTime TIME,
    @EndTime TIME,
    @Color NVARCHAR(20) = NULL,
    @IsActive BIT = 1
AS
BEGIN
    -- Check if shift type exists
    IF NOT EXISTS (SELECT 1 FROM ShiftTypes WHERE Id = @ShiftTypeId)
    BEGIN
        RETURN;
    END
    
    -- Check if new name conflicts with existing shift type (but not itself)
    IF EXISTS (SELECT 1 FROM ShiftTypes WHERE Name = @Name AND Id <> @ShiftTypeId)
    BEGIN
        RETURN;
    END
    
    UPDATE ShiftTypes
    SET 
        Name = @Name,
        Description = @Description,
        StartTime = @StartTime,
        EndTime = @EndTime,
        Color = @Color,
        IsActive = @IsActive,
        UpdatedAt = GETDATE()
    WHERE 
        Id = @ShiftTypeId;
END;
GO

-- Stored Procedure to delete a shift type
CREATE PROCEDURE DeleteShiftType
    @ShiftTypeId INT
AS
BEGIN
    -- Check if shift type is being used by shifts
    IF EXISTS (SELECT 1 FROM Shifts WHERE ShiftTypeId = @ShiftTypeId)
    BEGIN
        RETURN;
    END
    
    -- Delete the shift type
    DELETE FROM ShiftTypes
    WHERE Id = @ShiftTypeId;
END;
GO

-- Stored Procedure to create a shift
CREATE PROCEDURE CreateShift
    @ShiftDate DATE,
    @ShiftTypeId INT,
    @DepartmentId INT,
    @RequiredStaff INT,
    @Notes NVARCHAR(500) = NULL,
    @Status INT = 0,
    @ShiftId INT OUTPUT
AS
BEGIN
    -- Insert the shift
    INSERT INTO Shifts (
        ShiftDate,
        ShiftTypeId,
        DepartmentId,
        RequiredStaff,
        Notes,
        Status,
        CreatedAt,
        UpdatedAt
    )
    VALUES (
        @ShiftDate,
        @ShiftTypeId,
        @DepartmentId,
        @RequiredStaff,
        @Notes,
        @Status,
        GETDATE(),
        GETDATE()
    );
    
    SET @ShiftId = SCOPE_IDENTITY();
END;
GO

-- Stored Procedure to update a shift
CREATE PROCEDURE UpdateShift
    @ShiftId INT,
    @ShiftDate DATE,
    @ShiftTypeId INT,
    @DepartmentId INT,
    @RequiredStaff INT,
    @Notes NVARCHAR(500) = NULL,
    @Status INT = 0
AS
BEGIN
    -- Update the shift
    UPDATE Shifts
    SET 
        ShiftDate = @ShiftDate,
        ShiftTypeId = @ShiftTypeId,
        DepartmentId = @DepartmentId,
        RequiredStaff = @RequiredStaff,
        Notes = @Notes,
        Status = @Status,
        UpdatedAt = GETDATE()
    WHERE 
        Id = @ShiftId;
END;
GO

-- Stored Procedure to get shifts by date range and department
CREATE PROCEDURE GetShiftsByDateRangeAndDepartment
    @StartDate DATE,
    @EndDate DATE,
    @DepartmentId INT = NULL
AS
BEGIN
    SELECT 
        s.Id,
        s.ShiftDate,
        s.ShiftTypeId,
        st.Name AS ShiftTypeName,
        st.StartTime,
        st.EndTime,
        st.Color,
        s.DepartmentId,
        d.Name AS DepartmentName,
        s.RequiredStaff,
        s.Notes,
        s.Status,
        s.CreatedAt,
        s.UpdatedAt,
        (SELECT COUNT(*) FROM ShiftAssignments sa WHERE sa.ShiftId = s.Id AND sa.Status < 4) AS AssignedStaffCount
    FROM 
        Shifts s
    INNER JOIN
        ShiftTypes st ON s.ShiftTypeId = st.Id
    INNER JOIN
        Departments d ON s.DepartmentId = d.Id
    WHERE 
        s.ShiftDate BETWEEN @StartDate AND @EndDate
        AND (@DepartmentId IS NULL OR s.DepartmentId = @DepartmentId)
    ORDER BY 
        s.ShiftDate, st.StartTime;
END;
GO

-- Stored Procedure to get shift by ID with details
CREATE PROCEDURE GetShiftById
    @ShiftId INT
AS
BEGIN
    -- Get shift details
    SELECT 
        s.Id,
        s.ShiftDate,
        s.ShiftTypeId,
        st.Name AS ShiftTypeName,
        st.StartTime,
        st.EndTime,
        st.Color,
        s.DepartmentId,
        d.Name AS DepartmentName,
        s.RequiredStaff,
        s.Notes,
        s.Status,
        s.CreatedAt,
        s.UpdatedAt
    FROM 
        Shifts s
    INNER JOIN
        ShiftTypes st ON s.ShiftTypeId = st.Id
    INNER JOIN
        Departments d ON s.DepartmentId = d.Id
    WHERE 
        s.Id = @ShiftId;
    
    -- Get assigned staff
    SELECT 
        sa.Id AS AssignmentId,
        sa.ShiftId,
        sa.UserId,
        u.FirstName,
        u.LastName,
        u.Email,
        jt.Title AS JobTitle,
        sa.Status,
        sa.AssignedAt,
        sa.CheckInTime,
        sa.CheckOutTime,
        sa.HoursWorked,
        sa.Notes
    FROM 
        ShiftAssignments sa
    INNER JOIN
        Users u ON sa.UserId = u.Id
    LEFT JOIN
        JobTitles jt ON u.JobTitleId = jt.Id
    WHERE 
        sa.ShiftId = @ShiftId
    ORDER BY 
        u.LastName, u.FirstName;
END;
GO

-- Stored Procedure to assign staff to shift
CREATE PROCEDURE AssignStaffToShift
    @ShiftId INT,
    @UserId INT,
    @Notes NVARCHAR(500) = NULL,
    @AssignmentId INT OUTPUT
AS
BEGIN
    -- Check if assignment already exists
    IF EXISTS (SELECT 1 FROM ShiftAssignments WHERE ShiftId = @ShiftId AND UserId = @UserId)
    BEGIN
        SET @AssignmentId = 0;
        RETURN;
    END
    
    -- Check if user is available for this shift
    DECLARE @ShiftDate DATE;
    DECLARE @StartTime TIME;
    DECLARE @EndTime TIME;
    
    SELECT 
        @ShiftDate = s.ShiftDate,
        @StartTime = st.StartTime,
        @EndTime = st.EndTime
    FROM 
        Shifts s
    INNER JOIN
        ShiftTypes st ON s.ShiftTypeId = st.Id
    WHERE 
        s.Id = @ShiftId;
    
    -- Check for time off requests
    IF EXISTS (
        SELECT 1 
        FROM TimeOffRequests 
        WHERE UserId = @UserId 
        AND Status = 1 -- Approved
        AND @ShiftDate BETWEEN StartDate AND EndDate
    )
    BEGIN
        SET @AssignmentId = -1; -- User has approved time off
        RETURN;
    END
    
    -- Check for overlapping shifts
    IF EXISTS (
        SELECT 1
        FROM ShiftAssignments sa
        INNER JOIN Shifts s ON sa.ShiftId = s.Id
        INNER JOIN ShiftTypes st ON s.ShiftTypeId = st.Id
        WHERE sa.UserId = @UserId
        AND s.ShiftDate = @ShiftDate
        AND sa.Status < 4 -- Not cancelled
        AND (
            (@StartTime BETWEEN st.StartTime AND st.EndTime)
            OR (@EndTime BETWEEN st.StartTime AND st.EndTime)
            OR (st.StartTime BETWEEN @StartTime AND @EndTime)
        )
    )
    BEGIN
        SET @AssignmentId = -2; -- User has overlapping shift
        RETURN;
    END
    
    -- Insert the assignment
    INSERT INTO ShiftAssignments (
        ShiftId,
        UserId,
        Status,
        AssignedAt,
        Notes
    )
    VALUES (
        @ShiftId,
        @UserId,
        0, -- Assigned
        GETDATE(),
        @Notes
    );
    
    SET @AssignmentId = SCOPE_IDENTITY();
    
    -- Update shift status based on required staff
    DECLARE @RequiredStaff INT;
    DECLARE @AssignedStaff INT;
    
    SELECT @RequiredStaff = RequiredStaff
    FROM Shifts
    WHERE Id = @ShiftId;
    
    SELECT @AssignedStaff = COUNT(*)
    FROM ShiftAssignments
    WHERE ShiftId = @ShiftId AND Status < 4; -- Not cancelled
    
    UPDATE Shifts
    SET 
        Status = CASE 
                    WHEN @AssignedStaff >= @RequiredStaff THEN 1 -- Fully Staffed
                    ELSE 2 -- Understaffed
                 END,
        UpdatedAt = GETDATE()
    WHERE 
        Id = @ShiftId;
END;
GO

-- Stored Procedure to remove staff from shift
CREATE PROCEDURE RemoveStaffFromShift
    @AssignmentId INT
AS
BEGIN
    -- Get shift id for later update
    DECLARE @ShiftId INT;
    
    SELECT @ShiftId = ShiftId
    FROM ShiftAssignments
    WHERE Id = @AssignmentId;
    
    -- Update assignment status to cancelled
    UPDATE ShiftAssignments
    SET 
        Status = 4, -- Cancelled
        Notes = ISNULL(Notes, '') + ' [Removed from shift]'
    WHERE 
        Id = @AssignmentId;
    
    -- Update shift status based on required staff
    DECLARE @RequiredStaff INT;
    DECLARE @AssignedStaff INT;
    
    SELECT @RequiredStaff = RequiredStaff
    FROM Shifts
    WHERE Id = @ShiftId;
    
    SELECT @AssignedStaff = COUNT(*)
    FROM ShiftAssignments
    WHERE ShiftId = @ShiftId AND Status < 4; -- Not cancelled
    
    UPDATE Shifts
    SET 
        Status = CASE 
                    WHEN @AssignedStaff = 0 THEN 0 -- Open
                    WHEN @AssignedStaff >= @RequiredStaff THEN 1 -- Fully Staffed
                    ELSE 2 -- Understaffed
                 END,
        UpdatedAt = GETDATE()
    WHERE 
        Id = @ShiftId;
END;
GO

-- Stored Procedure to update shift assignment status
CREATE PROCEDURE UpdateShiftAssignmentStatus
    @AssignmentId INT,
    @Status INT,
    @Notes NVARCHAR(500) = NULL
AS
BEGIN
    -- Update assignment status
    UPDATE ShiftAssignments
    SET 
        Status = @Status,
        Notes = CASE 
                    WHEN @Notes IS NOT NULL THEN @Notes
                    ELSE Notes
                END,
        CheckInTime = CASE 
                        WHEN @Status = 2 AND CheckInTime IS NULL THEN GETDATE() -- Checked-In
                        ELSE CheckInTime
                      END,
        CheckOutTime = CASE 
                         WHEN @Status = 3 AND CheckOutTime IS NULL THEN GETDATE() -- Completed
                         ELSE CheckOutTime
                       END,
        HoursWorked = CASE 
                        WHEN @Status = 3 AND CheckInTime IS NOT NULL AND CheckOutTime IS NULL 
                        THEN DATEDIFF(MINUTE, CheckInTime, GETDATE()) / 60.0
                        ELSE HoursWorked
                      END
    WHERE 
        Id = @AssignmentId;
END;
GO

-- Stored Procedure to create a time off request
CREATE PROCEDURE CreateTimeOffRequest
    @UserId INT,
    @StartDate DATE,
    @EndDate DATE,
    @Reason NVARCHAR(500) = NULL,
    @RequestId INT OUTPUT
AS
BEGIN
    -- Insert the request
    INSERT INTO TimeOffRequests (
        UserId,
        StartDate,
        EndDate,
        Reason,
        Status,
        RequestedAt
    )
    VALUES (
        @UserId,
        @StartDate,
        @EndDate,
        @Reason,
        0, -- Pending
        GETDATE()
    );
    
    SET @RequestId = SCOPE_IDENTITY();
END;
GO

-- Stored Procedure to get time off requests by status and date range
CREATE PROCEDURE GetTimeOffRequests
    @Status INT = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SELECT 
        tor.Id,
        tor.UserId,
        u.FirstName,
        u.LastName,
        u.Email,
        d.Name AS DepartmentName,
        jt.Title AS JobTitle,
        tor.StartDate,
        tor.EndDate,
        tor.Reason,
        tor.Status,
        tor.RequestedAt,
        tor.ApprovedById,
        approver.FirstName + ' ' + approver.LastName AS ApprovedByName,
        tor.ApprovedAt,
        tor.Notes,
        DATEDIFF(DAY, tor.StartDate, tor.EndDate) + 1 AS DaysRequested
    FROM 
        TimeOffRequests tor
    INNER JOIN
        Users u ON tor.UserId = u.Id
    LEFT JOIN
        Departments d ON u.DepartmentId = d.Id
    LEFT JOIN
        JobTitles jt ON u.JobTitleId = jt.Id
    LEFT JOIN
        Users approver ON tor.ApprovedById = approver.Id
    WHERE 
        (@Status IS NULL OR tor.Status = @Status)
        AND (@StartDate IS NULL OR tor.EndDate >= @StartDate)
        AND (@EndDate IS NULL OR tor.StartDate <= @EndDate)
    ORDER BY 
        tor.Status, tor.StartDate;
END;
GO

-- Stored Procedure to get time off requests by user
CREATE PROCEDURE GetTimeOffRequestsByUser
    @UserId INT
AS
BEGIN
    SELECT 
        tor.Id,
        tor.UserId,
        tor.StartDate,
        tor.EndDate,
        tor.Reason,
        tor.Status,
        tor.RequestedAt,
        tor.ApprovedById,
        approver.FirstName + ' ' + approver.LastName AS ApprovedByName,
        tor.ApprovedAt,
        tor.Notes,
        DATEDIFF(DAY, tor.StartDate, tor.EndDate) + 1 AS DaysRequested
    FROM 
        TimeOffRequests tor
    LEFT JOIN
        Users approver ON tor.ApprovedById = approver.Id
    WHERE 
        tor.UserId = @UserId
    ORDER BY 
        tor.Status, tor.StartDate;
END;
GO

-- Stored Procedure to approve/deny time off request
CREATE PROCEDURE UpdateTimeOffRequestStatus
    @RequestId INT,
    @Status INT,
    @ApprovedById INT,
    @Notes NVARCHAR(500) = NULL
AS
BEGIN
    -- Update request status
    UPDATE TimeOffRequests
    SET 
        Status = @Status,
        ApprovedById = @ApprovedById,
        ApprovedAt = GETDATE(),
        Notes = @Notes
    WHERE 
        Id = @RequestId;
END;
GO

-- Stored Procedure to create shift swap request
CREATE PROCEDURE CreateShiftSwapRequest
    @RequestingUserId INT,
    @ShiftAssignmentId INT,
    @RequestedUserId INT = NULL,
    @Notes NVARCHAR(500) = NULL,
    @RequestId INT OUTPUT
AS
BEGIN
    -- Check if requesting user is assigned to this shift
    IF NOT EXISTS (
        SELECT 1
        FROM ShiftAssignments
        WHERE Id = @ShiftAssignmentId AND UserId = @RequestingUserId AND Status < 2 -- Not checked-in yet
    )
    BEGIN
        SET @RequestId = 0;
        RETURN;
    END
    
    -- Check if a swap request already exists for this assignment
    IF EXISTS (
        SELECT 1
        FROM ShiftSwapRequests
        WHERE ShiftAssignmentId = @ShiftAssignmentId AND Status = 0 -- Pending
    )
    BEGIN
        SET @RequestId = -1;
        RETURN;
    END
    
    -- Insert the request
    INSERT INTO ShiftSwapRequests (
        RequestingUserId,
        ShiftAssignmentId,
        RequestedUserId,
        Status,
        RequestedAt,
        Notes
    )
    VALUES (
        @RequestingUserId,
        @ShiftAssignmentId,
        @RequestedUserId,
        0, -- Pending
        GETDATE(),
        @Notes
    );
    
    SET @RequestId = SCOPE_IDENTITY();
END;
GO

-- Stored Procedure to get shift swap requests
CREATE PROCEDURE GetShiftSwapRequests
    @Status INT = NULL
AS
BEGIN
    SELECT 
        ssr.Id,
        ssr.RequestingUserId,
        requester.FirstName + ' ' + requester.LastName AS RequesterName,
        ssr.ShiftAssignmentId,
        s.ShiftDate,
        st.Name AS ShiftTypeName,
        st.StartTime,
        st.EndTime,
        d.Name AS DepartmentName,
        ssr.RequestedUserId,
        CASE 
            WHEN ssr.RequestedUserId IS NULL THEN 'Open to All'
            ELSE requested.FirstName + ' ' + requested.LastName
        END AS RequestedUserName,
        ssr.Status,
        ssr.RequestedAt,
        ssr.ApprovedById,
        approver.FirstName + ' ' + approver.LastName AS ApprovedByName,
        ssr.ApprovedAt,
        ssr.Notes
    FROM 
        ShiftSwapRequests ssr
    INNER JOIN
        ShiftAssignments sa ON ssr.ShiftAssignmentId = sa.Id
    INNER JOIN
        Shifts s ON sa.ShiftId = s.Id
    INNER JOIN
        ShiftTypes st ON s.ShiftTypeId = st.Id
    INNER JOIN
        Departments d ON s.DepartmentId = d.Id
    INNER JOIN
        Users requester ON ssr.RequestingUserId = requester.Id
    LEFT JOIN
        Users requested ON ssr.RequestedUserId = requested.Id
    LEFT JOIN
        Users approver ON ssr.ApprovedById = approver.Id
    WHERE 
        (@Status IS NULL OR ssr.Status = @Status)
    ORDER BY 
        s.ShiftDate, st.StartTime;
END;
GO

-- Stored Procedure to get shift swap requests for a user
CREATE PROCEDURE GetShiftSwapRequestsForUser
    @UserId INT
AS
BEGIN
    -- Get requests made by the user
    SELECT 
        ssr.Id,
        'Requested' AS RequestType,
        ssr.RequestingUserId,
        requester.FirstName + ' ' + requester.LastName AS RequesterName,
        ssr.ShiftAssignmentId,
        s.ShiftDate,
        st.Name AS ShiftTypeName,
        st.StartTime,
        st.EndTime,
        d.Name AS DepartmentName,
        ssr.RequestedUserId,
        CASE 
            WHEN ssr.RequestedUserId IS NULL THEN 'Open to All'
            ELSE requested.FirstName + ' ' + requested.LastName
        END AS RequestedUserName,
        ssr.Status,
        ssr.RequestedAt,
        ssr.ApprovedById,
        approver.FirstName + ' ' + approver.LastName AS ApprovedByName,
        ssr.ApprovedAt,
        ssr.Notes
    FROM 
        ShiftSwapRequests ssr
    INNER JOIN
        ShiftAssignments sa ON ssr.ShiftAssignmentId = sa.Id
    INNER JOIN
        Shifts s ON sa.ShiftId = s.Id
    INNER JOIN
        ShiftTypes st ON s.ShiftTypeId = st.Id
    INNER JOIN
        Departments d ON s.DepartmentId = d.Id
    INNER JOIN
        Users requester ON ssr.RequestingUserId = requester.Id
    LEFT JOIN
        Users requested ON ssr.RequestedUserId = requested.Id
    LEFT JOIN
        Users approver ON ssr.ApprovedById = approver.Id
    WHERE 
        ssr.RequestingUserId = @UserId
    
    UNION
    
    -- Get requests directed to the user or open to all in their department
    SELECT 
        ssr.Id,
        'Received' AS RequestType,
        ssr.RequestingUserId,
        requester.FirstName + ' ' + requester.LastName AS RequesterName,
        ssr.ShiftAssignmentId,
        s.ShiftDate,
        st.Name AS ShiftTypeName,
        st.StartTime,
        st.EndTime,
        d.Name AS DepartmentName,
        ssr.RequestedUserId,
        CASE 
            WHEN ssr.RequestedUserId IS NULL THEN 'Open to All'
            ELSE requested.FirstName + ' ' + requested.LastName
        END AS RequestedUserName,
        ssr.Status,
        ssr.RequestedAt,
        ssr.ApprovedById,
        approver.FirstName + ' ' + approver.LastName AS ApprovedByName,
        ssr.ApprovedAt,
        ssr.Notes
    FROM 
        ShiftSwapRequests ssr
    INNER JOIN
        ShiftAssignments sa ON ssr.ShiftAssignmentId = sa.Id
    INNER JOIN
        Shifts s ON sa.ShiftId = s.Id
    INNER JOIN
        ShiftTypes st ON s.ShiftTypeId = st.Id
    INNER JOIN
        Departments d ON s.DepartmentId = d.Id
    INNER JOIN
        Users requester ON ssr.RequestingUserId = requester.Id
    LEFT JOIN
        Users requested ON ssr.RequestedUserId = requested.Id
    LEFT JOIN
        Users approver ON ssr.ApprovedById = approver.Id
    INNER JOIN
        Users currentUser ON currentUser.Id = @UserId
    WHERE 
        (ssr.RequestedUserId = @UserId OR (ssr.RequestedUserId IS NULL AND currentUser.DepartmentId = s.DepartmentId))
        AND ssr.RequestingUserId <> @UserId
        AND ssr.Status = 0 -- Pending
    
    ORDER BY 
        ShiftDate, StartTime;
END;
GO

-- Stored Procedure to respond to shift swap request
CREATE PROCEDURE RespondToShiftSwapRequest
    @RequestId INT,
    @RespondingUserId INT,
    @Status INT,
    @ApprovedById INT = NULL,
    @Notes NVARCHAR(500) = NULL
AS
BEGIN
    DECLARE @ShiftAssignmentId INT;
    DECLARE @RequestingUserId INT;
    DECLARE @RequestedUserId INT;
    DECLARE @ShiftId INT;
    
    -- Get request details
    SELECT 
        @ShiftAssignmentId = ShiftAssignmentId,
        @RequestingUserId = RequestingUserId,
        @RequestedUserId = RequestedUserId
    FROM 
        ShiftSwapRequests
    WHERE 
        Id = @RequestId;
    
    -- Get shift id
    SELECT @ShiftId = ShiftId
    FROM ShiftAssignments
    WHERE Id = @ShiftAssignmentId;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Update request status
        UPDATE ShiftSwapRequests
        SET 
            Status = @Status,
            ApprovedById = @ApprovedById,
            ApprovedAt = GETDATE(),
            RequestedUserId = CASE WHEN @RequestedUserId IS NULL THEN @RespondingUserId ELSE @RequestedUserId END,
            Notes = CASE 
                        WHEN @Notes IS NOT NULL THEN @Notes
                        ELSE Notes
                    END
        WHERE 
            Id = @RequestId;
        
        -- If approved, swap the shifts
        IF @Status = 1 -- Approved
        BEGIN
            -- Create a new assignment for the responding user
            INSERT INTO ShiftAssignments (
                ShiftId,
                UserId,
                Status,
                AssignedAt,
                Notes
            )
            VALUES (
                @ShiftId,
                @RespondingUserId,
                0, -- Assigned
                GETDATE(),
                'Assigned via shift swap'
            );
            
            -- Update the original assignment to cancelled
            UPDATE ShiftAssignments
            SET 
                Status = 4, -- Cancelled
                Notes = ISNULL(Notes, '') + ' [Swapped with ' + CAST(@RespondingUserId AS NVARCHAR(10)) + ']'
            WHERE 
                Id = @ShiftAssignmentId;
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Stored Procedure to get user shifts by date range
CREATE PROCEDURE GetUserShiftsByDateRange
    @UserId INT,
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SELECT 
        s.Id AS ShiftId,
        sa.Id AS AssignmentId,
        s.ShiftDate,
        st.Name AS ShiftTypeName,
        st.StartTime,
        st.EndTime,
        st.Color,
        d.Name AS DepartmentName,
        sa.Status,
        sa.CheckInTime,
        sa.CheckOutTime,
        sa.HoursWorked,
        sa.Notes
    FROM 
        ShiftAssignments sa
    INNER JOIN
        Shifts s ON sa.ShiftId = s.Id
    INNER JOIN
        ShiftTypes st ON s.ShiftTypeId = st.Id
    INNER JOIN
        Departments d ON s.DepartmentId = d.Id
    WHERE 
        sa.UserId = @UserId
        AND s.ShiftDate BETWEEN @StartDate AND @EndDate
        AND sa.Status < 4 -- Not cancelled
    ORDER BY 
        s.ShiftDate, st.StartTime;
END;
GO

-- Initialize default departments
INSERT INTO Departments (Name, Description, IsActive)
VALUES 
('Kitchen', 'Kitchen and food preparation staff', 1),
('Service', 'Wait staff and servers', 1),
('Bar', 'Bartenders and bar staff', 1),
('Management', 'Managers and supervisors', 1),
('Hosting', 'Hostesses and reception', 1);
GO

-- Initialize default job titles
INSERT INTO JobTitles (Title, Description, DepartmentId, HourlyRate, IsActive)
VALUES 
('Executive Chef', 'Head of kitchen operations', 1, 35.00, 1),
('Sous Chef', 'Second in command in the kitchen', 1, 28.00, 1),
('Line Cook', 'Prepares food at a specific station', 1, 22.00, 1),
('Prep Cook', 'Prepares ingredients for cooking', 1, 18.00, 1),
('Server', 'Takes orders and serves food to guests', 2, 15.00, 1),
('Head Server', 'Supervises servers and assists with complex orders', 2, 18.00, 1),
('Bartender', 'Prepares and serves drinks', 3, 18.00, 1),
('Bar Manager', 'Oversees bar operations and inventory', 3, 22.00, 1),
('Restaurant Manager', 'Oversees daily operations of the restaurant', 4, 30.00, 1),
('Assistant Manager', 'Assists the restaurant manager', 4, 25.00, 1),
('Host/Hostess', 'Greets and seats guests', 5, 15.00, 1),
('Head Host', 'Manages the hosting team and reservations', 5, 18.00, 1);
GO

-- Initialize default shift types
INSERT INTO ShiftTypes (Name, Description, StartTime, EndTime, Color, IsActive)
VALUES 
('Morning', 'Morning shift', '06:00:00', '14:00:00', '#4CAF50', 1),
('Afternoon', 'Afternoon shift', '14:00:00', '22:00:00', '#2196F3', 1),
('Night', 'Night shift', '22:00:00', '06:00:00', '#9C27B0', 1),
('Breakfast', 'Breakfast service', '06:00:00', '11:00:00', '#FFC107', 1),
('Lunch', 'Lunch service', '11:00:00', '15:00:00', '#FF9800', 1),
('Dinner', 'Dinner service', '17:00:00', '23:00:00', '#F44336', 1),
('Brunch', 'Weekend brunch service', '10:00:00', '15:00:00', '#8BC34A', 1);
GO

-- Initialize default staff skills
INSERT INTO StaffSkills (Name, Description, IsRequired)
VALUES 
('Food Handling', 'Safe food handling certification', 1),
('Alcohol Service', 'Responsible alcohol service certification', 0),
('First Aid', 'Basic first aid training', 0),
('Fire Safety', 'Fire safety training', 1),
('Grill Station', 'Experience working the grill station', 0),
('Saute Station', 'Experience working the saute station', 0),
('Wine Knowledge', 'Knowledge of wines and pairings', 0),
('POS System', 'Training on the restaurant POS system', 1),
('Menu Knowledge', 'Thorough knowledge of menu items and ingredients', 1);
GO
