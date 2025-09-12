using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.ViewModels
{

    public class EditUserViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot be longer than 50 characters")]
        public string Username { get; set; }
        
        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50, ErrorMessage = "First Name cannot be longer than 50 characters")]
        public string FirstName { get; set; }
        
        [StringLength(50, ErrorMessage = "Last Name cannot be longer than 50 characters")]
        public string LastName { get; set; }
        
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
        public string Email { get; set; }
        
        [Phone(ErrorMessage = "Invalid Phone Number")]
        [StringLength(20, ErrorMessage = "Phone cannot be longer than 20 characters")]
        public string Phone { get; set; }
        
        [Required(ErrorMessage = "Role is required")]
        public int Role { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
