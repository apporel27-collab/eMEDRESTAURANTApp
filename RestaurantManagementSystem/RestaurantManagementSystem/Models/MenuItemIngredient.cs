using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class MenuItemIngredient
    {
        public int Id { get; set; }
        
        [Required]
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; }
        
        [Required]
        public int IngredientId { get; set; }
        public Ingredients Ingredient { get; set; }
        
        [Required]
        [Range(0.01, 9999.99, ErrorMessage = "Quantity must be between 0.01 and 9,999.99")]
        public decimal Quantity { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Unit { get; set; }
        
        [Display(Name = "Is Optional")]
        public bool IsOptional { get; set; }
        
        [Display(Name = "Instructions")]
        [StringLength(200)]
        public string Instructions { get; set; }
    }
}
