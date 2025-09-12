using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class AuthUser
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [StringLength(50)]
        public string FirstName { get; set; }
        
        [StringLength(50)]
        public string LastName { get; set; }
        
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }
        
        public bool IsActive { get; set; }
        
        public bool IsLockedOut { get; set; }
        
        public int FailedLoginAttempts { get; set; }
        
        public DateTime? LastLoginDate { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime LastModifiedDate { get; set; }
        
        public bool MustChangePassword { get; set; }
        
        public DateTime? PasswordLastChanged { get; set; }
        
        public bool RequiresMFA { get; set; }
        
        public string PasswordHash { get; set; }
        
        public string Salt { get; set; }
        
        public string FullName => $"{FirstName} {LastName}";
        
        public List<AuthUserRole> Roles { get; set; } = new List<AuthUserRole>();
        
        public List<string> Permissions { get; set; } = new List<string>();
        
        public List<Outlet> Outlets { get; set; } = new List<Outlet>();
    }
    
    public class AuthUserRole
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        public bool IsSystemRole { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime LastModifiedDate { get; set; }
    }
    
    public class Permission
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; }
        
        public bool IsSystemPermission { get; set; }
    }
    
    public class Outlet
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string Location { get; set; }
        
        public bool IsActive { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime LastModifiedDate { get; set; }
    }
    
    public class UserSession
    {
        public Guid Id { get; set; }
        
        public int UserId { get; set; }
        
        public string Token { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime ExpiryDate { get; set; }
        
        public DateTime LastActivityDate { get; set; }
        
        public string IpAddress { get; set; }
        
        public string DeviceId { get; set; }
        
        public string UserAgent { get; set; }
        
        public bool IsActive { get; set; }
    }
    
    public class AuditLogEntry
    {
        public long Id { get; set; }
        
        public int? UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Action { get; set; }
        
        public string Details { get; set; }
        
        [StringLength(50)]
        public string IpAddress { get; set; }
        
        [StringLength(500)]
        public string UserAgent { get; set; }
        
        [StringLength(100)]
        public string EntityName { get; set; }
        
        [StringLength(50)]
        public string EntityId { get; set; }
        
        public DateTime Timestamp { get; set; }
    }
    
    public class MFAFactor
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string FactorType { get; set; } // "Email", "Phone", "App"
        
        [Required]
        [StringLength(100)]
        public string FactorValue { get; set; }
        
        public bool IsVerified { get; set; }
        
        public bool IsDefault { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime? LastUsedDate { get; set; }
    }
    
    public class OverrideRequest
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string RequestType { get; set; } // "Void", "Refund", "Discount", "PriceOverride", "DayReopen"
        
        public int RequesterId { get; set; }
        
        public int? ApproverId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } // "Pending", "Approved", "Rejected"
        
        public decimal? Amount { get; set; }
        
        public int? OrderId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Justification { get; set; }
        
        [StringLength(500)]
        public string ApproverJustification { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime? RespondedDate { get; set; }
        
        // Navigation properties
        public AuthUser Requester { get; set; }
        public AuthUser Approver { get; set; }
    }
    
    public class Device
    {
        [StringLength(100)]
        public string Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [StringLength(50)]
        public string DeviceType { get; set; } // "POS", "KDS", "Manager", "Mobile"
        
        public int? OutletId { get; set; }
        
        [StringLength(50)]
        public string StationName { get; set; }
        
        public DateTime RegisteredDate { get; set; }
        
        public DateTime? LastActiveDate { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } // "Active", "Inactive", "Maintenance", "Blocked"
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        // Navigation property
        public Outlet Outlet { get; set; }
    }
}
