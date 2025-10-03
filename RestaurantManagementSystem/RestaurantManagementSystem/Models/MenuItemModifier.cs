using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class MenuItemModifierNew
    {
        public int Id { get; set; }
        
        [Required]
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; }
        
        [Required]
        public int ModifierId { get; set; }
        public Modifier Modifier { get; set; }
        
        [Required]
        [Range(0, 9999.99, ErrorMessage = "Price adjustment must be between ₹0 and ₹9,999.99")]
        [DataType(DataType.Currency)]
        [Display(Name = "Price Adjustment")]
        public decimal PriceAdjustment { get; set; }
        
        [Display(Name = "Is Default")]
        public bool IsDefault { get; set; }
        
        [Display(Name = "Maximum Allowed")]
        [Range(0, 10, ErrorMessage = "Maximum allowed must be between 0 and 10")]
        public int? MaxAllowed { get; set; }
    }
}
