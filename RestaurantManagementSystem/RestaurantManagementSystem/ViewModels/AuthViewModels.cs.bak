using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; }
        
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
        
        public string ReturnUrl { get; set; }
    }
    
    public class MFAViewModel
    {
        [Required(ErrorMessage = "Verification code is required")]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; }
        
        public string FactorType { get; set; }
        
        public string UserId { get; set; }
        
        public string ReturnUrl { get; set; }
    }
    
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        [StringLength(50, ErrorMessage = "Username must be between {2} and {1} characters", MinimumLength = 3)]
        public string Username { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "First name must be between {2} and {1} characters", MinimumLength = 1)]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "Last name must be between {2} and {1} characters", MinimumLength = 1)]
        public string LastName { get; set; }
        
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be between {2} and {1} characters", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
            ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character")]
        public string Password { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
        
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
    
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
    
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be between {2} and {1} characters", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
            ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character")]
        public string Password { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
        
        public string Token { get; set; }
    }
    
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }
        
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, ErrorMessage = "Password must be between {2} and {1} characters", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
            ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character")]
        public string NewPassword { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
    
    public class UserListViewModel
    {
        public List<UserViewModel> Users { get; set; }
        public PaginationViewModel Pagination { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
        public string SortDirection { get; set; }
    }
    
    public class UserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public List<string> Roles { get; set; }
        public List<string> Outlets { get; set; }
    }
    
    public class UserEditViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        [StringLength(50, ErrorMessage = "Username must be between {2} and {1} characters", MinimumLength = 3)]
        public string Username { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "First name must be between {2} and {1} characters", MinimumLength = 1)]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "Last name must be between {2} and {1} characters", MinimumLength = 1)]
        public string LastName { get; set; }
        
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; }
        
        [Display(Name = "Locked Out")]
        public bool IsLockedOut { get; set; }
        
        [Display(Name = "Requires MFA")]
        public bool RequiresMFA { get; set; }
        
        [Display(Name = "Must Change Password")]
        public bool MustChangePassword { get; set; }
        
        [Display(Name = "Selected Roles")]
        public List<int> SelectedRoles { get; set; } = new List<int>();
        
        [Display(Name = "Available Roles")]
        public List<RoleViewModel> AvailableRoles { get; set; } = new List<RoleViewModel>();
        
        [Display(Name = "Selected Outlets")]
        public List<int> SelectedOutlets { get; set; } = new List<int>();
        
        [Display(Name = "Available Outlets")]
        public List<OutletViewModel> AvailableOutlets { get; set; } = new List<OutletViewModel>();
    }
    
    public class RoleViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSystemRole { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }
    
    public class OutletViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public bool IsActive { get; set; }
    }
    
    public class PaginationViewModel
    {
        public int TotalItems { get; set; }
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / ItemsPerPage);
    }
    
    public class OverrideRequestViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Request type is required")]
        [Display(Name = "Request Type")]
        public string RequestType { get; set; }
        
        [Display(Name = "Amount")]
        [DataType(DataType.Currency)]
        public decimal? Amount { get; set; }
        
        [Display(Name = "Order ID")]
        public int? OrderId { get; set; }
        
        [Required(ErrorMessage = "Justification is required")]
        [Display(Name = "Justification")]
        [StringLength(500, ErrorMessage = "Justification must be between {2} and {1} characters", MinimumLength = 10)]
        public string Justification { get; set; }
        
        public string RequesterName { get; set; }
        
        public string Status { get; set; }
        
        public DateTime RequestDate { get; set; }
    }
    
    public class OverrideResponseViewModel
    {
        public int RequestId { get; set; }
        
        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public string Status { get; set; }
        
        [Required(ErrorMessage = "Justification is required")]
        [Display(Name = "Justification")]
        [StringLength(500, ErrorMessage = "Justification must be between {2} and {1} characters", MinimumLength = 10)]
        public string ApproverJustification { get; set; }
    }
    
    public class UserWithPermissionsViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
        public List<string> Outlets { get; set; } = new List<string>();
    }
}
