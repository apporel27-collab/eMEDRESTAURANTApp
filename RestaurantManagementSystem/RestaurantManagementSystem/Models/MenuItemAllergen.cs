using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class MenuItemAllergen
    {
        public int Id { get; set; }
        
        [Required]
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; }
        
        [Required]
        public int AllergenId { get; set; }
        public Allergen Allergen { get; set; }
        
        [Display(Name = "Severity Level")]
        [Range(1, 3, ErrorMessage = "Severity level must be between 1 (mild) and 3 (severe)")]
        public int SeverityLevel { get; set; } = 1;
    }
}
