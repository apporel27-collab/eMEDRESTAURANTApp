using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class RecipeStep
    {
        public int Id { get; set; }
        
        [Required]
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }
        
        [Required]
        [Display(Name = "Step Number")]
        [Range(1, 50, ErrorMessage = "Step number must be between 1 and 50")]
        public int StepNumber { get; set; }
        
        [Required(ErrorMessage = "Step description is required")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }
        
        [Display(Name = "Time Required (Minutes)")]
        public int? TimeRequiredMinutes { get; set; }
        
        [Display(Name = "Temperature")]
        public string Temperature { get; set; }
        
        [Display(Name = "Special Equipment")]
        public string SpecialEquipment { get; set; }
        
        [Display(Name = "Tips")]
        [DataType(DataType.MultilineText)]
        public string Tips { get; set; }
        
        [Display(Name = "Image Path")]
        public string ImagePath { get; set; }
    }
}
