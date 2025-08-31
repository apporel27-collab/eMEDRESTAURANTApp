using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    // Models for UC-006: Staff Scheduling & Management
    
    public class Department
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public List<JobTitle> JobTitles { get; set; } = new List<JobTitle>();
    }
    
    public class JobTitle
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Title { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        public int DepartmentId { get; set; }
        
        [StringLength(50)]
        public string DepartmentName { get; set; }
        
        [Range(0, 1000)]
        public decimal? HourlyRate { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public Department Department { get; set; }
    }
    
    public class ShiftType
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        
        [StringLength(20)]
        public string Color { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public string TimeRangeDisplay => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        
        public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;
        
        public string DurationDisplay
        {
            get
            {
                var duration = EndTime - StartTime;
                if (duration.TotalMinutes < 0)
                {
                    // Handle overnight shifts
                    duration = duration.Add(TimeSpan.FromHours(24));
                }
                return $"{duration.Hours}h {duration.Minutes}m";
            }
        }
    }
    
    public class Shift
    {
        public int Id { get; set; }
        public DateTime ShiftDate { get; set; }
        public int ShiftTypeId { get; set; }
        
        [StringLength(50)]
        public string ShiftTypeName { get; set; }
        
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        
        [StringLength(20)]
        public string Color { get; set; }
        
        public int DepartmentId { get; set; }
        
        [StringLength(50)]
        public string DepartmentName { get; set; }
        
        public int RequiredStaff { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        public int Status { get; set; } // 0=Open, 1=Fully Staffed, 2=Understaffed, 3=Cancelled
        
        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    0 => "Open",
                    1 => "Fully Staffed",
                    2 => "Understaffed",
                    3 => "Cancelled",
                    _ => "Unknown"
                };
            }
        }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public int AssignedStaffCount { get; set; }
        
        // Navigation properties
        public ShiftType ShiftType { get; set; }
        public Department Department { get; set; }
        public List<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
        
        public string DateDisplay => ShiftDate.ToString("ddd, MMM d, yyyy");
        public string TimeDisplay => $"{StartTime:hh\\:mm tt} - {EndTime:hh\\:mm tt}";
    }
    
    public class ShiftAssignment
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }
        public int UserId { get; set; }
        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string JobTitle { get; set; }
        
        public int Status { get; set; } // 0=Assigned, 1=Confirmed, 2=Checked-In, 3=Completed, 4=Cancelled, 5=No-Show
        
        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    0 => "Assigned",
                    1 => "Confirmed",
                    2 => "Checked-In",
                    3 => "Completed",
                    4 => "Cancelled",
                    5 => "No-Show",
                    _ => "Unknown"
                };
            }
        }
        
        public DateTime AssignedAt { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public decimal? HoursWorked { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        // Navigation properties
        public Shift Shift { get; set; }
        public StaffMember User { get; set; }
    }
    
    public class TimeOffRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string DepartmentName { get; set; }
        public string JobTitle { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [StringLength(500)]
        public string Reason { get; set; }
        
        public int Status { get; set; } // 0=Pending, 1=Approved, 2=Denied
        
        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    0 => "Pending",
                    1 => "Approved",
                    2 => "Denied",
                    _ => "Unknown"
                };
            }
        }
        
        public DateTime RequestedAt { get; set; }
        
        public int? ApprovedById { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        public int DaysRequested { get; set; }
        
        // Navigation properties
        public StaffMember User { get; set; }
    }
    
    public class ShiftSwapRequest
    {
        public int Id { get; set; }
        public string RequestType { get; set; } // "Requested" or "Received"
        
        public int RequestingUserId { get; set; }
        public string RequesterName { get; set; }
        
        public int ShiftAssignmentId { get; set; }
        
        public DateTime ShiftDate { get; set; }
        public string ShiftTypeName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string DepartmentName { get; set; }
        
        public int? RequestedUserId { get; set; }
        public string RequestedUserName { get; set; }
        
        public int Status { get; set; } // 0=Pending, 1=Approved, 2=Denied, 3=Cancelled
        
        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    0 => "Pending",
                    1 => "Approved",
                    2 => "Denied",
                    3 => "Cancelled",
                    _ => "Unknown"
                };
            }
        }
        
        public DateTime RequestedAt { get; set; }
        
        public int? ApprovedById { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        public string DateDisplay => ShiftDate.ToString("ddd, MMM d, yyyy");
        public string TimeDisplay => $"{StartTime:hh\\:mm tt} - {EndTime:hh\\:mm tt}";
    }
    
    public class StaffSkill
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        public bool IsRequired { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    public class UserSkill
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SkillId { get; set; }
        
        [StringLength(50)]
        public string SkillName { get; set; }
        
        public int ProficiencyLevel { get; set; } // 1=Basic, 2=Intermediate, 3=Advanced
        
        public string ProficiencyDisplay
        {
            get
            {
                return ProficiencyLevel switch
                {
                    1 => "Basic",
                    2 => "Intermediate",
                    3 => "Advanced",
                    _ => "Unknown"
                };
            }
        }
        
        public DateTime? CertifiedAt { get; set; }
        
        [StringLength(200)]
        public string Notes { get; set; }
        
        // Navigation properties
        public StaffMember User { get; set; }
        public StaffSkill Skill { get; set; }
    }
    
    public class StaffMember : User
    {
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        
        public int? JobTitleId { get; set; }
        public string JobTitleName { get; set; }
        
        public DateTime? HireDate { get; set; }
        
        [StringLength(20)]
        public string EmployeeNumber { get; set; }
        
        public decimal? HourlyRate { get; set; }
        
        public string SchedulePreferences { get; set; }
        
        [StringLength(50)]
        public string AvailableDays { get; set; }
        
        public int? MaxHoursPerWeek { get; set; }
        
        // Navigation properties
        public Department Department { get; set; }
        public JobTitle JobTitle { get; set; }
        public List<UserSkill> Skills { get; set; } = new List<UserSkill>();
        public List<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
        public List<TimeOffRequest> TimeOffRequests { get; set; } = new List<TimeOffRequest>();
        
        public bool IsAvailable(DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            if (TimeOffRequests.Any(r => r.Status == 1 && date >= r.StartDate && date <= r.EndDate))
            {
                return false;
            }
            
            if (Assignments.Any(a => 
                a.Status < 4 && // Not cancelled
                a.Shift.ShiftDate == date.Date && 
                ((startTime >= a.Shift.StartTime && startTime < a.Shift.EndTime) || 
                (endTime > a.Shift.StartTime && endTime <= a.Shift.EndTime) ||
                (startTime <= a.Shift.StartTime && endTime >= a.Shift.EndTime))))
            {
                return false;
            }
            
            return true;
        }
    }
}
