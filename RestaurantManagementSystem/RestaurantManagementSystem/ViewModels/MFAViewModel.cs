using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.ViewModels
{
    public class MFAViewModel
    {
        [Required]
        [Display(Name = "Verification Code")]
        [StringLength(6, ErrorMessage = "The {0} must be {1} characters long.", MinimumLength = 6)]
        public string VerificationCode { get; set; }

        public string UserId { get; set; }
        
        public string FactorType { get; set; }
        
        public string ReturnUrl { get; set; }
        
        [Display(Name = "Remember this device")]
        public bool RememberDevice { get; set; }
    }
}
