using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RestaurantManagementSystem.Models
{
    public class RecipeViewModel
    {
        public int Id { get; set; }
        
        public int MenuItemId { get; set; }

        [Display(Name = "Menu Item Name")]
        public string MenuItemName { get; set; }
        
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100)]
        public string Title { get; set; }
        
        [Required(ErrorMessage = "Preparation instructions are required")]
        [Display(Name = "Preparation Instructions")]
        public string PreparationInstructions { get; set; }
        
        [Required(ErrorMessage = "Cooking instructions are required")]
        [Display(Name = "Cooking Instructions")]
        public string CookingInstructions { get; set; }
        
        [Display(Name = "Plating Instructions")]
        public string PlatingInstructions { get; set; }
        
        [Required(ErrorMessage = "Yield is required")]
        [Range(1, 100, ErrorMessage = "Yield must be between 1 and 100")]
        public int Yield { get; set; }
        
        [Required(ErrorMessage = "Yield percentage is required")]
        [Range(1, 100, ErrorMessage = "Yield percentage must be between 1 and 100")]
        [Display(Name = "Yield %")]
        public decimal YieldPercentage { get; set; }
        
        [Required(ErrorMessage = "Preparation time is required")]
        [Range(1, 600, ErrorMessage = "Preparation time must be between 1 and 600 minutes")]
        [Display(Name = "Preparation Time (minutes)")]
        public int PreparationTimeMinutes { get; set; }
        
        [Required(ErrorMessage = "Cooking time is required")]
        [Range(1, 600, ErrorMessage = "Cooking time must be between 1 and 600 minutes")]
        [Display(Name = "Cooking Time (minutes)")]
        public int CookingTimeMinutes { get; set; }
        
        [Display(Name = "Notes")]
        public string Notes { get; set; }
        
        public int Version { get; set; }
        
        [Display(Name = "Created By")]
        public int CreatedById { get; set; }

        [Display(Name = "Is Archived")]
        public bool IsArchived { get; set; }
        
        public List<RecipeStepViewModel> Steps { get; set; } = new List<RecipeStepViewModel>();
        
        public List<MenuItemIngredientViewModel> Ingredients { get; set; } = new List<MenuItemIngredientViewModel>();
        
        public List<MenuItemAllergenViewModel> Allergens { get; set; } = new List<MenuItemAllergenViewModel>();
    }
    
    public class RecipeStepViewModel
    {
        public int Id { get; set; }
        
        public int RecipeId { get; set; }
        
        [Required(ErrorMessage = "Step number is required")]
        [Display(Name = "Step Number")]
        public int StepNumber { get; set; }
        
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
        
        [Display(Name = "Time Required (minutes)")]
        public int? TimeRequiredMinutes { get; set; }
        
        [Display(Name = "Temperature")]
        public string Temperature { get; set; }
        
        [Display(Name = "Special Equipment")]
        public string SpecialEquipment { get; set; }
        
        [Display(Name = "Tips")]
        public string Tips { get; set; }
        
        [Display(Name = "Image")]
        public string ImagePath { get; set; }
        
        // For file upload
        [Display(Name = "Image File")]
        public IFormFile ImageFile { get; set; }
    }
    
    public class MenuItemIngredientViewModel
    {
        public int Id { get; set; }
        
        public int MenuItemId { get; set; }
        
        [Required(ErrorMessage = "Ingredient is required")]
        [Display(Name = "Ingredient")]
        public int IngredientId { get; set; }
        
        public string IngredientName { get; set; }
        
        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.001, 9999.999, ErrorMessage = "Quantity must be between 0.001 and 9999.999")]
        public decimal Quantity { get; set; }
        
        [Required(ErrorMessage = "Unit is required")]
        [StringLength(20)]
        public string Unit { get; set; }
        
        [Display(Name = "Is Optional")]
        public bool IsOptional { get; set; }
        
        [StringLength(200)]
        public string Instructions { get; set; }
        
        [Display(Name = "Cost")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Cost { get; set; }
    }
    
    public class MenuItemAllergenViewModel
    {
        public int Id { get; set; }
        
        public int MenuItemId { get; set; }
        
        [Required(ErrorMessage = "Allergen is required")]
        [Display(Name = "Allergen")]
        public int AllergenId { get; set; }
        
        public string AllergenName { get; set; }
        
        [Required(ErrorMessage = "Severity level is required")]
        [Range(1, 3, ErrorMessage = "Severity level must be between 1 and 3")]
        [Display(Name = "Severity Level")]
        public int SeverityLevel { get; set; }
    }
}
