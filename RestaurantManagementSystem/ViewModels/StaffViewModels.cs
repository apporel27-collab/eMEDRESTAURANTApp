using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.ViewModels
{
    // View Models for UC-006: Staff Scheduling & Management
    
    public class StaffDashboardViewModel
    {
        public List<Shift> UpcomingShifts { get; set; } = new List<Shift>();
        public List<TimeOffRequest> PendingTimeOffRequests { get; set; } = new List<TimeOffRequest>();
        public List<ShiftSwapRequest> PendingShiftSwapRequests { get; set; } = new List<ShiftSwapRequest>();
        public StaffScheduleStats Stats { get; set; } = new StaffScheduleStats();
    }
    
    public class StaffScheduleStats
    {
        public int TotalStaffCount { get; set; }
        public int ScheduledShiftsCount { get; set; }
        public int OpenShiftsCount { get; set; }
        public int UnderstaffedShiftsCount { get; set; }
        public int PendingTimeOffRequestsCount { get; set; }
        public int PendingShiftSwapRequestsCount { get; set; }
    }
    
    public class DepartmentViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Department name is required")]
        [StringLength(50, ErrorMessage = "Department name cannot exceed 50 characters")]
        [Display(Name = "Department Name")]
        public string Name { get; set; }
        
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string Description { get; set; }
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
    
    public class JobTitleViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Job title is required")]
        [StringLength(50, ErrorMessage = "Job title cannot exceed 50 characters")]
        [Display(Name = "Job Title")]
        public string Title { get; set; }
        
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string Description { get; set; }
        
        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        
        public string DepartmentName { get; set; }
        
        [Display(Name = "Hourly Rate")]
        [Range(0, 1000, ErrorMessage = "Hourly rate must be between 0 and 1000")]
        public decimal? HourlyRate { get; set; }
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        
        public List<Department> Departments { get; set; } = new List<Department>();
    }
    
    public class ShiftTypeViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Shift name is required")]
        [StringLength(50, ErrorMessage = "Shift name cannot exceed 50 characters")]
        [Display(Name = "Shift Name")]
        public string Name { get; set; }
        
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string Description { get; set; }
        
        [Required(ErrorMessage = "Start time is required")]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }
        
        [Required(ErrorMessage = "End time is required")]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }
        
        [Display(Name = "Color")]
        [StringLength(20, ErrorMessage = "Color code cannot exceed 20 characters")]
        public string Color { get; set; }
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
    
    public class ShiftViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Shift date is required")]
        [Display(Name = "Shift Date")]
        [DataType(DataType.Date)]
        public DateTime ShiftDate { get; set; }
        
        [Required(ErrorMessage = "Shift type is required")]
        [Display(Name = "Shift Type")]
        public int ShiftTypeId { get; set; }
        
        public string ShiftTypeName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        
        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        
        public string DepartmentName { get; set; }
        
        [Required(ErrorMessage = "Required staff count is required")]
        [Range(1, 100, ErrorMessage = "Required staff must be between 1 and 100")]
        [Display(Name = "Required Staff")]
        public int RequiredStaff { get; set; } = 1;
        
        [Display(Name = "Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
        
        [Display(Name = "Status")]
        public int Status { get; set; } = 0;
        
        public List<ShiftType> ShiftTypes { get; set; } = new List<ShiftType>();
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
        
        public int AssignedStaffCount => Assignments?.Count(a => a.Status < 4) ?? 0;
    }
    
    public class StaffScheduleViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        
        public List<DateTime> DateRange { get; set; } = new List<DateTime>();
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<Shift> Shifts { get; set; } = new List<Shift>();
    }
    
    public class ShiftDetailsViewModel
    {
        public Shift Shift { get; set; }
        public List<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
        public List<StaffMember> AvailableStaff { get; set; } = new List<StaffMember>();
    }
    
    public class AssignStaffViewModel
    {
        public int ShiftId { get; set; }
        public DateTime ShiftDate { get; set; }
        public string ShiftTypeName { get; set; }
        public string DepartmentName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        
        [Required(ErrorMessage = "Please select a staff member")]
        public int UserId { get; set; }
        
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
        
        public List<StaffMember> AvailableStaff { get; set; } = new List<StaffMember>();
    }
    
    public class StaffListViewModel
    {
        public List<StaffMember> Staff { get; set; } = new List<StaffMember>();
        public int? DepartmentId { get; set; }
        public List<Department> Departments { get; set; } = new List<Department>();
    }
    
    public class StaffMemberViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }
        
        [Display(Name = "Job Title")]
        public int? JobTitleId { get; set; }
        
        [Display(Name = "Hire Date")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }
        
        [Display(Name = "Employee Number")]
        [StringLength(20, ErrorMessage = "Employee number cannot exceed 20 characters")]
        public string EmployeeNumber { get; set; }
        
        [Display(Name = "Hourly Rate")]
        [Range(0, 1000, ErrorMessage = "Hourly rate must be between 0 and 1000")]
        public decimal? HourlyRate { get; set; }
        
        [Display(Name = "Schedule Preferences")]
        public string SchedulePreferences { get; set; }
        
        [Display(Name = "Available Days")]
        public List<string> AvailableDays { get; set; } = new List<string>();
        
        [Display(Name = "Maximum Hours Per Week")]
        [Range(0, 168, ErrorMessage = "Maximum hours must be between 0 and 168")]
        public int? MaxHoursPerWeek { get; set; }
        
        [Display(Name = "Role")]
        public string Role { get; set; }
        
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<JobTitle> JobTitles { get; set; } = new List<JobTitle>();
        public List<UserSkill> Skills { get; set; } = new List<UserSkill>();
        public List<StaffSkill> AvailableSkills { get; set; } = new List<StaffSkill>();
    }
    
    public class TimeOffRequestViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        [Display(Name = "Reason")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }
        
        public string FullName { get; set; }
        public string DepartmentName { get; set; }
    }
    
    public class TimeOffRequestsViewModel
    {
        public List<TimeOffRequest> Requests { get; set; } = new List<TimeOffRequest>();
        public int? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
    
    public class TimeOffRequestResponseViewModel
    {
        public int RequestId { get; set; }
        public int Status { get; set; } // 1=Approved, 2=Denied
        
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
        
        public string StaffName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
    
    public class ShiftSwapRequestViewModel
    {
        public int ShiftAssignmentId { get; set; }
        public int RequestingUserId { get; set; }
        
        public int? RequestedUserId { get; set; }
        
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
        
        public DateTime ShiftDate { get; set; }
        public string ShiftTypeName { get; set; }
        public string DepartmentName { get; set; }
        
        public List<StaffMember> AvailableStaff { get; set; } = new List<StaffMember>();
    }
    
    public class ShiftSwapRequestsViewModel
    {
        public List<ShiftSwapRequest> Requests { get; set; } = new List<ShiftSwapRequest>();
        public int? Status { get; set; }
    }
    
    public class ShiftSwapResponseViewModel
    {
        public int RequestId { get; set; }
        public int RespondingUserId { get; set; }
        public int Status { get; set; } // 1=Approved, 2=Denied
        
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
        
        public string RequesterName { get; set; }
        public DateTime ShiftDate { get; set; }
        public string ShiftTypeName { get; set; }
    }
    
    public class StaffSkillViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Skill name is required")]
        [StringLength(50, ErrorMessage = "Skill name cannot exceed 50 characters")]
        [Display(Name = "Skill Name")]
        public string Name { get; set; }
        
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string Description { get; set; }
        
        [Display(Name = "Required for All Staff")]
        public bool IsRequired { get; set; }
    }
    
    public class UserSkillViewModel
    {
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Skill is required")]
        public int SkillId { get; set; }
        
        [Required(ErrorMessage = "Proficiency level is required")]
        [Range(1, 3, ErrorMessage = "Proficiency level must be between 1 and 3")]
        public int ProficiencyLevel { get; set; } = 1;
        
        [Display(Name = "Certification Date")]
        [DataType(DataType.Date)]
        public DateTime? CertifiedAt { get; set; }
        
        [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters")]
        public string Notes { get; set; }
        
        public List<StaffSkill> AvailableSkills { get; set; } = new List<StaffSkill>();
    }
    
    public class BulkScheduleCreationViewModel
    {
        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        
        [Display(Name = "Monday")]
        public bool IncludeMonday { get; set; } = true;
        
        [Display(Name = "Tuesday")]
        public bool IncludeTuesday { get; set; } = true;
        
        [Display(Name = "Wednesday")]
        public bool IncludeWednesday { get; set; } = true;
        
        [Display(Name = "Thursday")]
        public bool IncludeThursday { get; set; } = true;
        
        [Display(Name = "Friday")]
        public bool IncludeFriday { get; set; } = true;
        
        [Display(Name = "Saturday")]
        public bool IncludeSaturday { get; set; } = true;
        
        [Display(Name = "Sunday")]
        public bool IncludeSunday { get; set; } = true;
        
        public List<ShiftScheduleItem> Shifts { get; set; } = new List<ShiftScheduleItem>();
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<ShiftType> ShiftTypes { get; set; } = new List<ShiftType>();
    }
    
    public class ShiftScheduleItem
    {
        public int ShiftTypeId { get; set; }
        public string ShiftTypeName { get; set; }
        public int RequiredStaff { get; set; } = 1;
    }
    
    public class StaffMemberScheduleViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<Shift> Shifts { get; set; } = new List<Shift>();
        public List<TimeOffRequest> TimeOffRequests { get; set; } = new List<TimeOffRequest>();
    }
}
