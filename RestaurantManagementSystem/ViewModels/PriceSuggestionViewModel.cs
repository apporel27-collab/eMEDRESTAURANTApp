using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.ViewModels
{
    public class PriceSuggestionViewModel
    {
        public int MenuItemId { get; set; }
        
        public string MenuItemName { get; set; }
        
        [Display(Name = "Total Cost")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalCost { get; set; }
        
        [Display(Name = "Target GP %")]
        [Range(1, 90, ErrorMessage = "Target GP% must be between 1 and 90")]
        public decimal TargetGPPercentage { get; set; }
        
        [Display(Name = "Suggested Price")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal SuggestedPrice { get; set; }
        
        [Display(Name = "Current Price")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal CurrentPrice { get; set; }
        
        [Display(Name = "New Price")]
        [Required(ErrorMessage = "New price is required")]
        [Range(0.01, 9999.99, ErrorMessage = "Price must be between $0.01 and $9,999.99")]
        public decimal NewPrice { get; set; }
    }
}
