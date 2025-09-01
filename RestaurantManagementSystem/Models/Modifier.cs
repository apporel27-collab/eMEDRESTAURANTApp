using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class Modifier
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(255)]
        public string Description { get; set; }
        
        [Display(Name = "Modifier Type")]
        [Required(ErrorMessage = "Modifier Type is required")]
        public string ModifierType { get; set; } // "Addition", "Substitution", "Removal"
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}
