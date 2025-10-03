using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class Allergen
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        [Display(Name = "Icon Path")]
        public string IconPath { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}
